using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using System.IO.Compression;
using Newtonsoft.Json;

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
                        messageBuffer.Append(JsonConvert.SerializeObject(message));
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
                    Flush();
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
                string mutexName = "Global\\SensorsAnalytics " + Path.GetFullPath(fileName).Replace('\\', '_').Replace('/', '_');
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

    /// <summary>
    /// 因为并发性能比较差，已不推荐使用。<br/>
    /// 此 Consumer 的作用是将事件写入文件中，当调用 <see cref="Flush"/> 的时候将文件中的数据发往指定的服务器上。
    /// </summary>
    public class ClientConsumer : IConsumer
    {
        private static readonly object lockObject = new object();
        private readonly string bufferFilename;
        private readonly int bufferSize;
        private readonly string serverUrl;
        private readonly Mutex mutex;
        private readonly FileStream fileStream;

        public ClientConsumer(
            string bufferFilename,
            int bufferSize,
            string serverUrl)
        {
            this.bufferFilename = bufferFilename;
            this.bufferSize = bufferSize;
            this.serverUrl = serverUrl;

            this.fileStream = new FileStream(bufferFilename, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
            string mutexName = "Global\\SensorsAnalytics " + Path.GetFullPath(bufferFilename).Replace('\\', '_').Replace('/', '_');
            this.mutex = new Mutex(false, mutexName);
        }

        public ClientConsumer(string bufferFilename, string serverUrl) : this(bufferFilename, 8192, serverUrl) { }

        public virtual void Send(Dictionary<string, Object> message)
        {
            lock (lockObject)
            {
                try
                {
                    string recordString = JsonConvert.SerializeObject(message) + "\n";
                    mutex.WaitOne();
                    fileStream.Seek(0, SeekOrigin.End);
                    byte[] bytes = Encoding.UTF8.GetBytes(recordString);
                    fileStream.Write(bytes, 0, bytes.Length);
                    mutex.ReleaseMutex();
                }
                catch (Exception e)
                {
                    throw new SystemException("fail to write file", e);
                }
            }
        }

        public void Flush()
        {
            lock (lockObject)
            {
                mutex.WaitOne();
                fileStream.Seek(0, SeekOrigin.Begin);
                int numBytesToRead = (int)fileStream.Length;
                int numBytesRead = 0;
                byte[] bytes = new byte[numBytesToRead];
                while (numBytesToRead > 0)
                {
                    int n = fileStream.Read(bytes, numBytesRead, numBytesToRead);
                    if (n == 0)
                    {
                        break;
                    }
                    numBytesRead += n;
                    numBytesToRead -= n;
                }
                string recordsString = System.Text.Encoding.UTF8.GetString(bytes, 0, bytes.Length);

                using (StringReader stringReader = new StringReader(recordsString))
                {
                    StringBuilder stringBuilder = new StringBuilder("[");
                    string line;
                    int count = 0;
                    while ((line = stringReader.ReadLine()) != null)
                    {
                        if (count != 0)
                        {
                            stringBuilder.Append(',');
                        }
                        stringBuilder.Append(line);
                        ++count;
                        if (count >= 3)
                        {
                            stringBuilder.Append("]");

                            Console.WriteLine(stringBuilder.ToString());
                            Console.WriteLine();
                            this.SendToServer(stringBuilder.ToString());

                            stringBuilder = new StringBuilder("[");
                            count = 0;
                        }
                    }

                    if (count > 0)
                    {
                        stringBuilder.Append("]");

                        Console.WriteLine(stringBuilder.ToString());
                        Console.WriteLine();
                        this.SendToServer(stringBuilder.ToString());
                    }
                }

                fileStream.SetLength(0);
                mutex.ReleaseMutex();
            }
        }

        private void SendToServer(string records)
        {
            string dataList = GzipAndBase64(records);
            string encodedDataList = System.Web.HttpUtility.UrlEncode(dataList);
            string requestBody = "gzip=1&data_list=" + encodedDataList;

            var request = (HttpWebRequest)WebRequest.Create(serverUrl);
            request.Method = "POST";
            request.Timeout = 30 * 1000;
            var data = Encoding.ASCII.GetBytes(requestBody);
            using (var stream = request.GetRequestStream())
            {
                stream.Write(data, 0, data.Length);
            }
            var response = (HttpWebResponse)request.GetResponse();
            var responseString = new StreamReader(response.GetResponseStream()).ReadToEnd();
        }

        private string GzipAndBase64(string inputStr)
        {
            byte[] inputBytes = Encoding.UTF8.GetBytes(inputStr);
            using (var outputStream = new MemoryStream())
            {
                using (var gzipStream = new GZipStream(outputStream, CompressionMode.Compress))
                    gzipStream.Write(inputBytes, 0, inputBytes.Length);
                var outputBytes = outputStream.ToArray();
                var base64Output = Convert.ToBase64String(outputBytes);
                return base64Output;
            }
        }

        public void Close()
        {
            Flush();
            fileStream.Close();
            mutex.Close();
        }
    }

    public class BatchConsumer : IConsumer
    {
        private readonly static int MAX_FLUSH_BULK_SIZE = 50;
        private readonly static int DEFAULT_TIME_OUT_SECOND = 30;
        private readonly List<Dictionary<string, Object>> messageList;
        private readonly string serverUrl;
        private readonly int bulkSize;
        private readonly int requestTimeoutMillisecond;
        private readonly bool throwException;

        public BatchConsumer(string serverUrl) : this(serverUrl, MAX_FLUSH_BULK_SIZE) { }

        public BatchConsumer(String serverUrl, int bulkSize) : this(serverUrl, bulkSize, DEFAULT_TIME_OUT_SECOND) { }

        public BatchConsumer(String serverUrl, int bulkSize, int requestTimeoutSecond) : this(serverUrl, bulkSize, requestTimeoutSecond, false) { }

        public BatchConsumer(string serverUrl, int bulkSize, int requestTimeoutSecond, bool throwException)
        {
            messageList = new List<Dictionary<string, object>>();
            this.serverUrl = serverUrl;
            this.bulkSize = Math.Min(MAX_FLUSH_BULK_SIZE, bulkSize);
            this.throwException = throwException;
            this.requestTimeoutMillisecond = requestTimeoutSecond * 1000;
        }

        public void Send(Dictionary<string, Object> message)
        {
            lock (messageList)
            {
                messageList.Add(message);
                if (messageList.Count >= bulkSize)
                {
                    Flush();
                }
            }
        }

        public void Flush()
        {
            lock (messageList)
            {
                while (messageList.Count != 0)
                {
                    int batchRecordCount = Math.Min(bulkSize, messageList.Count);
                    List<Dictionary<string, Object>> batchList = messageList.GetRange(0, batchRecordCount);
                    string sendingData;
                    try
                    {
                        sendingData = JsonConvert.SerializeObject(batchList);
                    }
                    catch (Exception exception)
                    {
                        messageList.RemoveRange(0, batchRecordCount);
                        if (throwException)
                        {
                            throw new SystemException("Failed to serialize data.", exception);
                        }
                        continue;
                    }

                    try
                    {
                        this.SendToServer(sendingData);
                        messageList.RemoveRange(0, batchRecordCount);
                    }
                    catch (Exception exception)
                    {
                        if (throwException)
                        {
                            throw new SystemException("Failed to dump message with BatchConsumer.", exception);
                        }
                        return;
                    }
                }
            }
        }

        private void SendToServer(string records)
        {
            string dataList = GzipAndBase64(records);
            string encodedDataList = System.Web.HttpUtility.UrlEncode(dataList);
            string requestBody = "gzip=1&data_list=" + encodedDataList;

            var request = (HttpWebRequest)WebRequest.Create(serverUrl);
            request.Method = "POST";
            request.ReadWriteTimeout = requestTimeoutMillisecond;
            request.Timeout = requestTimeoutMillisecond;
            request.UserAgent = "SensorsAnalytics DotNET SDK";
            var data = Encoding.ASCII.GetBytes(requestBody);
            using (var stream = request.GetRequestStream())
            {
                stream.Write(data, 0, data.Length);
            }
            var response = (HttpWebResponse)request.GetResponse();
            var responseString = new StreamReader(response.GetResponseStream()).ReadToEnd();
            if (response.StatusCode != HttpStatusCode.OK)
            {
                throw new SystemException("Sensors Analytics SDK send response is not 200, content: " + responseString);
            }
        }

        private string GzipAndBase64(string inputStr)
        {
            byte[] inputBytes = Encoding.UTF8.GetBytes(inputStr);
            using (var outputStream = new MemoryStream())
            {
                using (var gzipStream = new GZipStream(outputStream, CompressionMode.Compress))
                    gzipStream.Write(inputBytes, 0, inputBytes.Length);
                var outputBytes = outputStream.ToArray();
                var base64Output = Convert.ToBase64String(outputBytes);
                return base64Output;
            }
        }

        public void Close()
        {
            Flush();
        }
    }
}