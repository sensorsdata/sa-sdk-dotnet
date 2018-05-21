using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Web.Script.Serialization;

namespace SensorsData.Analytics
{
    /// <summary>
    /// 所有Consumer需要实现此接口
    /// </summary>
    public interface IConsumer
    {
        void Send(Dictionary<string, Object> message);

        void Flush();

        void Close();
    }

    public class ConcurrentLoggingConsumer : IConsumer
    {
        private static readonly int BUFFER_LIMITATION = 1 * 1024 * 1024 * 1024; // 1G

        private readonly JavaScriptSerializer jsonSerializer = new JavaScriptSerializer();
        private readonly String filenamePrefix;
        private readonly StringBuilder messageBuffer;
        private readonly int bufferSize;
        private InnerLoggingFileWriter fileWriter;

        public ConcurrentLoggingConsumer(
            String filenamePrefix,
            int bufferSize)
        {
            this.filenamePrefix = filenamePrefix;
            this.messageBuffer = new StringBuilder(bufferSize);
            this.bufferSize = bufferSize;
        }

        public ConcurrentLoggingConsumer(String filenamePrefix) : this(filenamePrefix, 8192) { }

        public virtual void Send(Dictionary<string, Object> message)
        {
            lock (this)
            {
                if (messageBuffer.Length < BUFFER_LIMITATION)
                {
                    try
                    {
                        messageBuffer.Append(jsonSerializer.Serialize(message));
                        messageBuffer.Append("\r\n");
                    }
                    catch (Exception e)
                    {
                        throw new SystemException("fail to process json", e);
                    }
                }
                else
                {
                    throw new SystemException("logging buffer exceeded the allowed limitation.");
                }

                if (messageBuffer.Length >= bufferSize)
                {
                    this.Flush();
                }
            }
        }

        public void Flush()
        {
            lock (this)
            {
                if (messageBuffer.Length == 0)
                {
                    return;
                }

                String fileName = filenamePrefix + DateTime.Now.ToString("yyyyMMdd") + ".txt";

                if (fileWriter != null && !fileWriter.IsValid(fileName))
                {
                    InnerLoggingFileWriter.RemoveInstance(fileWriter);
                    fileWriter = null;
                }

                if (fileWriter == null)
                {
                    fileWriter = InnerLoggingFileWriter.GetInstance(fileName);
                }

                if (fileWriter.Write(messageBuffer.ToString()))
                {
                    messageBuffer.Length = 0;
                }
            }
        }

        public void Close()
        {
            Flush();
            if (fileWriter != null)
            {
                InnerLoggingFileWriter.RemoveInstance(fileWriter);
                fileWriter = null;
            }
        }

        private class InnerLoggingFileWriter
        {
            private static Dictionary<String, InnerLoggingFileWriter> instances = new Dictionary<string, InnerLoggingFileWriter>();
            private readonly String fileName;
            private readonly Mutex mutex;
            private readonly FileStream outputStream;
            private int refCount;

            private InnerLoggingFileWriter(String fileName)
            {
                this.outputStream = new FileStream(fileName, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
                this.fileName = fileName;
                this.refCount = 0;
                String mutexName = "Global\\SensorsAnalytics " + Path.GetFullPath(fileName).Replace('\\', '_');
                this.mutex = new Mutex(false, mutexName);
            }

            public static InnerLoggingFileWriter GetInstance(String fileName)
            {
                lock (instances)
                {
                    if (!instances.ContainsKey(fileName))
                    {
                        instances.Add(fileName, new InnerLoggingFileWriter(fileName));
                    }

                    InnerLoggingFileWriter writer = instances[fileName];
                    writer.refCount = writer.refCount + 1;
                    return writer;
                }
            }

            public static void RemoveInstance(InnerLoggingFileWriter writer)
            {
                lock (instances)
                {
                    writer.refCount = writer.refCount - 1;
                    if (writer.refCount == 0)
                    {
                        writer.Close();
                        instances.Remove(writer.fileName);
                    }
                }
            }

            public void Close()
            {
                outputStream.Close();
                mutex.Close();
            }

            public bool IsValid(String fileName)
            {
                return this.fileName.Equals(fileName);
            }

            public bool Write(String data)
            {
                lock (outputStream)
                {
                    mutex.WaitOne();
                    outputStream.Seek(0, SeekOrigin.End);
                    byte[] bytes = Encoding.UTF8.GetBytes(data);
                    outputStream.Write(bytes, 0, bytes.Length);
                    mutex.ReleaseMutex();
                }
                return true;
            }
        }
    }

    public class LoggingConsumer : ConcurrentLoggingConsumer
    {
        private string project = null;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="logPath">日志文件存放的路径</param>
        public LoggingConsumer(string logPath) : base(logPath + '/')
        {
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="logPath">日志文件存放的路径</param>
        /// <param name="project">在神策分析中存在的项目名</param>
        public LoggingConsumer(string logPath, string project)
            : this(logPath)
        {
            if (project == null || project.Length == 0)
            {
                throw new ArgumentException("The project is invalid.");
            }
            this.project = project;
        }

        public override void Send(Dictionary<string, Object> message)
        {
            if (this.project != null && !message.ContainsKey("project"))
            {
                message.Add("project", this.project);
            }
            base.Send(message);
        }
    }
}