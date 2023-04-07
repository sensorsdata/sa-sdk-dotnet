using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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
                    outputStream.Flush();
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
    [Obsolete("此类已弃用，请使用 NewClientConsumer 类")]
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

    /// <summary>
    /// 将数据存放在缓存中
    /// </summary>
    [Obsolete("此类已弃用，请使用 NewClientConsumer 类")]
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

    /// <summary>
    /// 新的客户端 Consumer，此 Consumer 联合了 ClientConsumer 和 BatchConsumer 功能，
    /// 如果你使用上述两个 Consumer，可以考虑使用 NewClientConsumer。
    /// NewClientConsumer 会优先将数据放在缓存中，如果数据量达到指定的数量或者客户主动调用 Flush 功能，
    /// 将会把缓存中数据上传到服务器。如果客户调用 SensorsAnalytics.shutDown() 方法，将会把数据保存在到文件中，
    /// 下一次启动的时候会将文件中的数据优先加载到缓存中。
    ///
    /// 注意：如果程序意外 Crash，并且没有调用 SensorsAnalytics.shutDown() 方法，那么缓存中数据将会丢失。
    /// </summary>
    public class NewClientConsumer : IConsumer
    {
        private readonly static bool IS_DEBUG = false;
        private readonly static int MAX_FLUSH_BULK_SIZE = 50;
        private readonly static int DEFAULT_TIME_OUT_SECOND = 30 * 1000;
        private readonly string serverUrl;
        private readonly int bulkSize;
        private readonly int requestTimeoutMillisecond;
        private readonly bool isThrowException;
        private BlockingCollection<string> blockingList = new BlockingCollection<string>();
        private Mutex mutex;
        private FileStream fileStream;
        private readonly string fileAbsolutePath;
        private ICallback callback;
        private Timer timer;
        private readonly ScheduledStyle scheduledStyle;
        private readonly object flushLock = new object();

        private Task globalTask;
        CancellationTokenSource cancellationTokenSource;

        public NewClientConsumer(String serverUrl, String fileAbsolutePath, ICallback callback = null)
            : this(serverUrl, fileAbsolutePath, MAX_FLUSH_BULK_SIZE, callback) { }

        public NewClientConsumer(String serverUrl, String fileAbsolutePath, int bulkSize, ICallback callback = null)
            : this(serverUrl, fileAbsolutePath, bulkSize, DEFAULT_TIME_OUT_SECOND, callback) { }

        public NewClientConsumer(String serverUrl, String fileAbsolutePath, ICallback callback = null, ScheduledStyle scheduledStyle = ScheduledStyle.DISABLED, int flushMillisecond = 1000)
            : this(serverUrl, fileAbsolutePath, MAX_FLUSH_BULK_SIZE, callback, scheduledStyle, flushMillisecond) { }

        public NewClientConsumer(String serverUrl, String fileAbsolutePath, int bulkSize, ICallback callback = null, ScheduledStyle scheduledStyle = ScheduledStyle.DISABLED, int flushMillisecond = 1000)
            : this(serverUrl, fileAbsolutePath, bulkSize, DEFAULT_TIME_OUT_SECOND, callback, scheduledStyle, flushMillisecond) { }


        /// <summary>
        /// NewClientConsumer Constructor
        /// </summary>
        /// <param name="serverUrl">数据接收地址</param>
        /// <param name="fileAbsolutePath">日志文件的绝对路径，需确保该文件存在</param>
        /// <param name="bulkSize">缓存事件数目，当缓存大于等于此数，将触发数据上报功能</param>
        /// <param name="requestTimeoutMillisecond">数据上报的网络请求超时时间，单位是 ms</param>
        /// <param name="callback">当处理数据发生异常时，会返回处理异常的数据</param>
        /// <param name="scheduledStyle">定时上报任务。默认 <see cref="ScheduledStyle.DISABLED"/> 表示不开启定时刷新。<see cref="ScheduledStyle.ALWAYS_FLUSH"/>：开启定时任务，当触发定时任务时总是执行 Flush 操作。 <see cref="ScheduledStyle.BULKSIZE_FLUSH"/>：开启定时任务，当触发定时任务时缓存中事件数目超过 bulkSize 才会执行 Flush 操作。
        /// 注意开启定时任务后，当调用 <see cref="SensorsAnalytics.Track(string, string)"/>方法触发事件时并且缓存中事件超过 bulkSize 也不会立即出发 <see cref="Flush"/>，而是在定时任务执行时再上报事件。
        /// </param>
        /// <param name="flushMillisecond">消费者线程定时执行频率，默认 1000 毫秒，最低不能小于 1000ms</param>
        public NewClientConsumer(String serverUrl, String fileAbsolutePath, int bulkSize, int requestTimeoutMillisecond, ICallback callback = null, ScheduledStyle scheduledStyle = ScheduledStyle.DISABLED, int flushMillisecond = 1000)
        {
            this.bulkSize = Math.Min(MAX_FLUSH_BULK_SIZE, bulkSize); ;
            this.serverUrl = serverUrl;
            this.fileAbsolutePath = fileAbsolutePath;
            this.callback = callback;
            if (requestTimeoutMillisecond <= 0)
            {
                requestTimeoutMillisecond = DEFAULT_TIME_OUT_SECOND;
            }
            this.requestTimeoutMillisecond = requestTimeoutMillisecond;

            try
            {
                if (File.Exists(fileAbsolutePath))
                {
                    this.fileStream = new FileStream(fileAbsolutePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
                    string mutexName = "Global\\SensorsAnalytics " + Path.GetFullPath(fileAbsolutePath).Replace('\\', '_').Replace('/', '_');
                    this.mutex = new Mutex(false, mutexName);
                    LoadDataFromFile();
                }
                else
                {
                    if (callback != null)
                    {
                        callback.OnFailed(new FailedData(FailedData.TYPE_LOAD_FROME_FILE_ERROR,
                            "Can not found log file, it is not exists: " + fileAbsolutePath));
                    }
                }

            }
            catch (Exception e)
            {
                if (callback != null)
                {
                    callback.OnFailed(new FailedData(FailedData.TYPE_LOAD_FROME_FILE_ERROR,
                        "Something error when load data from log file: " + e.ToString()));
                }
                logE(e.Message + "\n" + e.StackTrace);
            }
            this.scheduledStyle = scheduledStyle;
            //指定定时任务
            if (scheduledStyle != ScheduledStyle.DISABLED)
            {
                timer = new Timer(TimingProcess, scheduledStyle, Math.Max(1000, flushMillisecond), Math.Max(1000, flushMillisecond));
                log("初始化定时任务，时间间隔：" + Math.Max(1000, flushMillisecond));
            }
        }

        private void TimingProcess(object scheduledStyle)
        {
            log($"开始执行定时任务，类型是: {scheduledStyle}, 缓存中的数据条目为：{blockingList.Count}，bulkSize 为：{bulkSize}");
            ScheduledStyle style = (ScheduledStyle)scheduledStyle;
            if (style == ScheduledStyle.ALWAYS_FLUSH)
            {
                Flush();
            }
            else if (style == ScheduledStyle.BULKSIZE_FLUSH)
            {
                if (blockingList.Count >= bulkSize)
                {
                    Flush();
                }
            }
        }

        private void LoadDataFromFile()
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
                string line;
                int count = 0;
                while ((line = stringReader.ReadLine()) != null)
                {
                    blockingList.Add(line);
                    count++;
                }

                log("There are " + count + " lines data are loaded from file to cache list");
            }
            fileStream.SetLength(0);
            mutex.ReleaseMutex();
            Flush();
        }

        public void Close()
        {
            try
            {
                if (timer != null)
                {
                    timer.Dispose();
                }
                if (cancellationTokenSource != null)
                {
                    cancellationTokenSource.Cancel();
                }
                try
                {
                    if (globalTask != null && (!globalTask.IsCompleted && !globalTask.IsCanceled && !globalTask.IsFaulted))
                    {
                        Task.WaitAll(globalTask);
                    }
                }
                catch (AggregateException e)
                {
                    logE("Wait flush task exception: " + e.ToString());
                }

                SaveCacheToFile();

                blockingList.Dispose();
                if (fileStream != null)
                {
                    fileStream.Close();
                    fileStream = null;
                }
                if (mutex != null)
                {
                    mutex.Close();
                    mutex = null;
                }
            }
            catch (Exception e)
            {
                if (callback != null)
                {
                    callback.OnFailed(new FailedData(FailedData.TYPE_OTHER_ERROR, "Something error when shutdown: " + e.ToString()));
                }
                logE("Something error when shutdown: " + e.ToString());
            }
        }

        public void Flush()
        {
            lock (flushLock)
            {
                //从队列中获取数据并将结果发送的服务端
                //如果获取的数目超过了 bulkSize 或者一定的时间 200ms 未能从队列中获取到数据并且已获取的结果大于 0 就发送数据。
                if (blockingList.Count > 0)
                {
                    if (globalTask != null && (!globalTask.IsCompleted && !globalTask.IsCanceled && !globalTask.IsFaulted))
                    {
                        return;
                    }
                    cancellationTokenSource = new CancellationTokenSource();
                    globalTask = new Task(ThreadRunner, cancellationTokenSource.Token);
                    globalTask.Start();
                    log("开启 Flush 工作线程，准备上报数据");
                }
            }
        }

        private void ThreadRunner()
        {
            try
            {
                if (blockingList.Count > 0)
                {
                    List<string> itemList = new List<string>();
                    StringBuilder stringBuilder = new StringBuilder("[");
                    string dataitem;
                    int count = 0;
                    while (!cancellationTokenSource.IsCancellationRequested && blockingList.TryTake(out dataitem, 200))
                    {
                        itemList.Add(dataitem);
                        stringBuilder.Append(dataitem);
                        stringBuilder.Append(",");
                        count++;
                        //如果超过了 50 条，就先等这部分数据发送完成，然后再发送下一波数据 
                        if (count >= 50)
                        {
                            stringBuilder.Append("]");
                            stringBuilder.Replace(",]", "]");
                            SendToServer(stringBuilder.ToString(), itemList);
                            stringBuilder.Clear();
                            itemList.Clear();
                            stringBuilder.Append("[");
                            count = 0;
                        }
                    }


                    if (count > 0)
                    {
                        //如果取消，就将数据放回缓存列表中
                        if (cancellationTokenSource.IsCancellationRequested)
                        {
                            foreach (string str in itemList)
                            {
                                blockingList.Add(str);
                            }
                        }
                        //如果是因为没有数据，就将已获取的发到服务器
                        else
                        {
                            stringBuilder.Append("]");
                            stringBuilder.Replace(",]", "]");
                            SendToServer(stringBuilder.ToString(), itemList);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                logE("数据发送失败，错误信息：" + e.ToString());
            }
        }

        public void Send(Dictionary<string, object> message)
        {
            try
            {
                blockingList.Add(JsonConvert.SerializeObject(message));
            }
            catch (Exception exception)
            {
                if (callback != null)
                {
                    callback.OnFailed(new FailedData(FailedData.TYPE_DATE_FORMAT_ERROR,
                        "Something error when add data: " + exception.ToString()));
                }

                logE("格式化数据失败:" + exception.ToString());
                return;
            }
            //当设置定时任务的时候，bulkSize 将不起作用
            if (this.scheduledStyle == ScheduledStyle.DISABLED)
            {
                if (blockingList.Count >= bulkSize)
                {
                    Flush();
                }
            }
        }


        private void SaveCacheToFile()
        {
            if (fileStream == null)
            {
                return;
            }

            try
            {
                if (blockingList.Count != 0)
                {
                    mutex.WaitOne();
                    string dataitem;
                    while (blockingList.TryTake(out dataitem, 200))
                    {
                        byte[] bytes = Encoding.UTF8.GetBytes(dataitem + "\n");
                        fileStream.Write(bytes, 0, bytes.Length);
                    }

                    fileStream.Flush();
                    mutex.ReleaseMutex();
                }
            }
            catch (Exception exception)
            {
                if (callback != null)
                {
                    callback.OnFailed(new FailedData(FailedData.TYPE_SAVE_TO_FILE_ERROR,
                        "Something error when save data to log file: " + exception.ToString(), new List<string>(blockingList)));
                }
                logE("保存数据到文件中失败: " + exception.ToString());
            }
        }

        private void SendToServer(string records, List<string> itemList)
        {
            try
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
                    if (callback != null)
                    {
                        callback.OnFailed(new FailedData(FailedData.TYPE_NETWORK_ERROR,
                            "Sensors Analytics SDK send response is not 200, content: " + responseString, itemList));
                    }
                    else
                    {
                        if (itemList.Count > 0)
                        {
                            foreach (string str in itemList)
                            {
                                blockingList.Add(str);
                            }
                        }
                        throw new SystemException("Sensors Analytics SDK send response is not 200, content: " + responseString);
                    }
                }
                else
                {
                    log("数据已发送到服务器");
                }
            }
            catch (Exception e)
            {
                if (callback != null)
                {
                    callback.OnFailed(new FailedData(FailedData.TYPE_NETWORK_ERROR,
                        "Something error when send data to server: " + e.ToString(), itemList));
                }
                else
                {
                    //如果出异常，就将数据放回缓存列表中
                    if (itemList.Count > 0)
                    {
                        foreach (string str in itemList)
                        {
                            blockingList.Add(str);
                        }
                    }
                    throw new SystemException("something wrong with HTTP request: " + e.ToString());
                }
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

        private void log(String logMsg)
        {
            if (IS_DEBUG)
            {
                Console.WriteLine(logMsg);
            }
        }

        private void logE(String logMsg)
        {
            Console.WriteLine(logMsg);
        }

        /// <summary>
        /// 获取时间戳
        /// </summary>
        /// <returns></returns>
        public long GetTimeStamp()
        {
            TimeSpan ts = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0);
            return Convert.ToInt64(ts.TotalMilliseconds);
        }
    }

    /// <summary>
    /// 异常回调接口
    /// </summary>
    public interface ICallback
    {
        void OnFailed(FailedData failedData);
    }

    /// <summary>
    /// 错误数据
    /// </summary>
    public class FailedData
    {
        /// <summary>
        /// 未标注错误类别
        /// </summary>
        public readonly static int TYPE_NONE = 0;
        /// <summary>
        /// 事件数据格式错误
        /// </summary>
        public readonly static int TYPE_DATE_FORMAT_ERROR = 1;
        /// <summary>
        /// 网络错误
        /// </summary>
        public readonly static int TYPE_NETWORK_ERROR = 2;
        /// <summary>
        /// 从日志文件中加载数据错误
        /// </summary>
        public readonly static int TYPE_LOAD_FROME_FILE_ERROR = 3;
        /// <summary>
        /// 保存数据到文件中失败
        /// </summary>
        public readonly static int TYPE_SAVE_TO_FILE_ERROR = 4;
        /// <summary>
        /// 其他错误
        /// </summary>
        public readonly static int TYPE_OTHER_ERROR = 5;
        /// <summary>
        /// 错误数据类别
        /// </summary>
        public int errorType = TYPE_NONE;
        /// <summary>
        /// 错误数据提示消息
        /// </summary>
        public String failedMessage;
        /// <summary>
        /// 错误数据：当类型是：TYPE_NETWORK_ERROR、TYPE_SAVE_TO_FILE_ERROR 时会将原数据放在该列表中
        /// </summary>
        public List<String> failedData;

        /// <summary>
        /// 当类型是：TYPE_NETWORK_ERROR、TYPE_SAVE_TO_FILE_ERROR 时会将原数据放在该列表中
        /// </summary>
        /// <param name="errorType"></param>
        /// <param name="failedMessage"></param>
        public FailedData(int errorType, String failedMessage) : this(errorType, failedMessage, null) { }

        /// <summary>
        /// 当类型是：TYPE_NETWORK_ERROR、TYPE_SAVE_TO_FILE_ERROR 时会将原数据放在该列表中
        /// </summary>
        /// <param name="errorType"></param>
        /// <param name="failedMessage"></param>
        /// <param name="failedData"></param>
        public FailedData(int errorType, String failedMessage, List<String> failedData)
        {
            this.errorType = errorType;
            this.failedData = failedData;
            this.failedMessage = failedMessage;
        }
    }

    /// <summary>
    /// NewClientConsumer 定时上报任务的类型
    /// </summary>
    public enum ScheduledStyle
    {
        /// <summary>
        /// 禁用定时任务
        /// </summary>
        DISABLED,

        /// <summary>
        /// 开启定时任务，当触发定时任务时总是执行 Flush 操作
        /// </summary>
        ALWAYS_FLUSH,

        /// <summary>
        /// 开启定时任务，当触发定时任务时缓存中事件数目超过 bulkSize 才会执行 Flush 操作
        /// </summary>
        BULKSIZE_FLUSH
    }
}
