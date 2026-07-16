using System;
using System.Collections.Generic;
using System.IO;
using SensorsData.Analytics;

namespace DotNetCoreDemo
{
    internal static class VscodeSmokeTest
    {
        private const string DemoResultDirectoryName = "test-results";
        private static readonly string DemoOutputDirectory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", DemoResultDirectoryName));

        public static int Main(string[] args)
        {
            string outputDirectory = args.Length > 0
                ? args[0]
                : DemoOutputDirectory;

            Directory.CreateDirectory(outputDirectory);
            string logFile = Path.Combine(outputDirectory, DateTime.Now.ToString("yyyyMMdd") + ".txt");

            // 默认只写本地日志，避免 demo 启动后误连历史测试地址或真实项目。
            IConsumer consumer = new LoggingConsumer(outputDirectory);
            SensorsAnalytics sa = new SensorsAnalytics(consumer, true);

            try
            {
                Dictionary<string, object> properties = new Dictionary<string, object>
                {
                    { "test_source", "vscode" },
                    { "demo_name", "DotNetCoreDemo" },
                    { "run_time", DateTime.Now }
                };

                sa.Track("vscode_test_user", "VSCodeSdkSmokeTest", properties);
                sa.Flush();
            }
            finally
            {
                sa.Shutdown();
            }

            Console.WriteLine("demo_ok=" + File.Exists(logFile).ToString().ToLowerInvariant());
            Console.WriteLine("log_file=" + logFile);

            return File.Exists(logFile) ? 0 : 2;
        }
    }
}
