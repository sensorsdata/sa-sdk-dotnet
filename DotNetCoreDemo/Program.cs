using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SensorsData.Analytics;

namespace DotNetCoreDemo
{


    class Program
    {
        static void Main(string[] args)
        {


            testProfileAppend();

        }

        static void testProfileAppend()
        {
            IConsumer consumer = new NewClientConsumer("http://newsdktest.datasink.sensorsdata.cn/sa?project=zhangwei&token=5a394d2405c147ca", "/Users/zhangwei/consumer/sss.txt", 10, 10 * 1000);
            SensorsAnalytics sa = new SensorsAnalytics(consumer, true);

            sa.ProfileAppend("12112", "aaa", "bbb");
            sa.Flush();
            Thread.Sleep(3000);
            Console.WriteLine("1231231");
            sa.Shutdown();
            Console.WriteLine("--End--");
        }

        static void testTrackNull() {
            IConsumer consumer = new NewClientConsumer("http://newsdktest.datasink.sensorsdata.cn/sa?project=zhangwei&token=5a394d2405c147ca", "/Users/zhangwei/consumer/sss.txt", 10, 10 * 1000);
            SensorsAnalytics sa = new SensorsAnalytics(consumer, true);

            Dictionary<string, Object> dic = new Dictionary<string, object>();
            dic.Add("productName", "iPhone 11");
            dic.Add("productPrice", "20000");
            Console.WriteLine("1111===" + DateTime.Now.ToLongTimeString());

            //sa.Track("112131", "ViewProduct22", dic);

            Thread.Sleep(3000);
            Console.WriteLine("1231231");
            sa.Shutdown();
            Console.WriteLine("--End--");
        }

        static void testFileNotFound()
        {
            Console.WriteLine("5555555");
            IConsumer consumer = new NewClientConsumer("http://newsdktest.datasink.sensorsdata.cn/sa?project=zhangwei&token=5a394d2405c147ca", "", 10, 10 * 1000);
            SensorsAnalytics sa = new SensorsAnalytics(consumer, true);
            Dictionary<string, Object> dic = new Dictionary<string, object>();
            dic.Add("productName", "iPhone 11");
            dic.Add("productPrice", "20000");
            Console.WriteLine("1111===" + DateTime.Now.ToLongTimeString());

            //sa.TrackSignUp("8888", "112131", dic);

            //sa.Track("112131", "ViewProduct22", dic);

            sa.ItemSet("item_type1111", "item_id1111", dic);
            //sa.ItemDelete("item_type2222", "item_id2222");
            Console.WriteLine("3333===" + DateTime.Now.ToLongTimeString());

            sa.Flush();

            Console.WriteLine("222222");

            Thread.Sleep(3000);
            Console.WriteLine("1231231");
            sa.Shutdown();
            Console.WriteLine("--End--");
            //sa.ItemSet("item_type1111", "item_id1111", dic);
            Thread.Sleep(2000);
            Console.WriteLine("--End22--");
        }


        static void testThreadClient()
        {

            IConsumer consumer = new NewClientConsumer("http://newsdktest.datasink.sensorsdata.cn/sa?project=zhangwei&token=5a394d2405c147ca", "/Users/zhangwei/consumer/sss.txt", 10, 10 * 1000);
            SensorsAnalytics sa = new SensorsAnalytics(consumer, true);
            Dictionary<string, Object> dic = new Dictionary<string, object>();
            dic.Add("productName", "iPhone 11");
            dic.Add("productPrice", "20000");
            Console.WriteLine("1111===" + DateTime.Now.ToLongTimeString());

            //sa.TrackSignUp("8888", "112131", dic);

            //sa.Track("112131", "ViewProduct22", dic);

            sa.ItemSet("item_type1111", "item_id1111", dic);
            //sa.ItemDelete("item_type2222", "item_id2222");
            Console.WriteLine("3333===" + DateTime.Now.ToLongTimeString());

            sa.Flush();

            Console.WriteLine("222222");

            Thread.Sleep(5000);
            Console.WriteLine("1231231");
            sa.Shutdown();
            Console.WriteLine("--End--");


        }

        static void batchTest()
        {
            IConsumer consumer = new BatchConsumer("http://newsdktest.datasink.sensorsdata.cn/sa?project=zhangwei&token=5a394d2405c147ca");
            SensorsAnalytics sa = new SensorsAnalytics(consumer, true);
            Dictionary<string, Object> dic = new Dictionary<string, object>();
            dic.Add("productName", "iPhone 11");
            dic.Add("productPrice", "20000");
            sa.Track("112131", "ViewProduct", dic);
            sa.Flush();
            Thread.Sleep(15 * 1000);
            sa.Shutdown();
        }

        static void testThread()
        {
            //Task task = new Task(() =>
            //{
            //    Thread.Sleep(100);
            //    Console.WriteLine($"hello, task1的线程ID为{Thread.CurrentThread.ManagedThreadId}");
            //});
            //task.Start();

            //Console.ReadKey();

            Console.WriteLine("44444444");

            IConsumer consumer = new NewClientConsumer("http://newsdktest.datasink.sensorsdata.cn/sa?project=zhangwei&token=5a394d2405c147ca", "/Users/zhangwei/consumer", 10, 10*1000);
            SensorsAnalytics sa = new SensorsAnalytics(consumer, true);
            Dictionary<string, Object> dic = new Dictionary<string, object>();
            dic.Add("productName", "iPhone 11");
            dic.Add("productPrice", "20000");
            sa.Track("112131", "ViewProduct", dic);

            //sa.ItemSet("item_type1111", "item_id1111", dic);
            //sa.ItemDelete("item_type2222", "item_id2222");
            Console.WriteLine("3333");

            sa.Flush();

            Console.WriteLine("222222");

            Thread.Sleep(15000);
            Console.WriteLine("1231231");
            sa.Shutdown();
            Console.WriteLine("--End--");


        }

        static void testBase()
        {
            Console.WriteLine("Hello World222");
            IConsumer consumer = new LoggingConsumer("/Users/zhangwei/consumer");
            SensorsAnalytics sa = new SensorsAnalytics(consumer, true);
            Dictionary<string, Object> dic = new Dictionary<string, object>();
            dic.Add("productName", "iPhone 11");
            dic.Add("productPrice", "20000");
            sa.Track("112131", "ViewProduct", dic);

            sa.ItemSet("item_type1111", "item_id1111", dic);
            sa.ItemDelete("item_type2222", "item_id2222");


            sa.Flush();
            sa.Shutdown();
            Console.WriteLine("--End--");
        }

        /// <summary>
        /// 仅测试使用
        /// </summary>
        static void testConsumer()
        {
            Console.WriteLine("--Start--");
            //ClientConsumer 仅仅是测试使用
            IConsumer consumer = new ClientConsumer("/Users/zhangwei/consumer/log.txt",
                "https://newsdktest.datasink.sensorsdata.cn/sa?project=zhangwei&token=5a394d2405c147ca");
            SensorsAnalytics sa = new SensorsAnalytics(consumer, true);
            Dictionary<string, Object> dic = new Dictionary<string, object>();
            dic.Add("productName", "iPhone 11");
            dic.Add("productPrice", "20000");
            sa.Track("112131", "ViewProduct", dic);

            sa.ItemSet("item_type1111", "item_id1111", dic);
            sa.ItemDelete("item_type2222", "item_id2222");


            sa.Flush();
            sa.Shutdown();
            Console.WriteLine("--End--");
        }
    }
}