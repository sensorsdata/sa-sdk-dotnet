using System;
using System.Collections.Generic;
using SensorsData.Analytics;

namespace DotNetFrameworkDemo
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World222");
            IConsumer consumer = new LoggingConsumer("/Users/zhangwei/consumer");
            SensorsAnalytics sa = new SensorsAnalytics(consumer, true);
            Dictionary<string, Object> dic = new Dictionary<string, object>();
            dic.Add("productName", "iPhone 11");
            dic.Add("productPrice", "20000");
            sa.Track("112131", "ViewProduct", dic);
            sa.Flush();
            sa.Shutdown();
            Console.WriteLine("--End--");
        }
    }
}
