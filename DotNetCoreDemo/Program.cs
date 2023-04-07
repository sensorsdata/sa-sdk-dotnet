using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SensorsData.Analytics;

namespace DotNetCoreDemo
{


    class Program
    {

        static SensorsAnalytics sa;
        static void Main(string[] args)
        {

            testNewClientConsumerTiming();
        }

        static void callFlush()
        {
            Thread.Sleep(40000);
            Console.WriteLine("开始 shutdown");
            if(sa != null)
            {
                sa.Shutdown();//或者 Flush
            }
        }


        static void testNewClientConsumerTiming3()
        {
            IConsumer consumer = new NewClientConsumer("http://10.129.28.106:8106/sa?project=default", "/Users/zhangwei/consumer/sss.txt", 5, null, ScheduledStyle.BULKSIZE_FLUSH, 10000);
            sa = new SensorsAnalytics(consumer, true);
            Dictionary<String, object> properties = new Dictionary<string, object>();
            sa.Track("abc12311231", "buyPhone");
            sa.Track("abc12311231", "buyPhone2");
            sa.Track("abc12311231", "buyPhone3");
            Console.WriteLine("等待 15 秒钟");
            Thread.Sleep(15000);
            sa.Track("abc12311231", "buyPhone4");
            Console.WriteLine("等待 25 秒钟");
            Thread.Sleep(25000);
            sa.Shutdown();
            Console.WriteLine("结束 track");
            Task.Factory.StartNew(callFlush);
        }


        static void testNewClientConsumerTiming()
        {
            IConsumer consumer = new NewClientConsumer("http://10.129.28.106:8106/sa?project=default", "/Users/zhangwei/consumer/sss.txt", 5,10000,null,ScheduledStyle.ALWAYS_FLUSH);
            sa = new SensorsAnalytics(consumer, true);
            Dictionary<String, object> properties = new Dictionary<string, object>();
            sa.Track("abc12311231", "buyPhone");
            sa.Track("abc12311231", "buyPhone2");
            sa.Track("abc12311231", "buyPhone3");
            Console.WriteLine("等待 15 秒钟");
            Thread.Sleep(15000);
            sa.Shutdown();
            Console.WriteLine("结束 track");
            Task.Factory.StartNew(callFlush);
        }

        static void testNewClientConsumerTiming2()
        {
            IConsumer consumer = new NewClientConsumer("http://10.129.28.106:8106/sa?project=default", "/Users/zhangwei/consumer/sss.txt", 3, null, ScheduledStyle.BULKSIZE_FLUSH, 5000);
            sa = new SensorsAnalytics(consumer, true);
            Console.WriteLine("开始 track");
            sa.Track("abc12311231", "buyPhone");
            sa.Track("abc12311231", "buyPhone2");
            sa.Track("abc12311231", "buyPhone3");
            Console.WriteLine("等待 50 秒钟2");

            Thread.Sleep(50000);
            Console.WriteLine("结束等待 50 秒钟2");
            sa.Shutdown();
            Console.WriteLine("end all");
        }

        static void testNewClientConsumerTiming1()
        {
            IConsumer consumer = new NewClientConsumer("http://10.129.28.106:8106/sa?project=default", "/Users/zhangwei/consumer/sss.txt", 5, null, ScheduledStyle.DISABLED, 10000);
            sa = new SensorsAnalytics(consumer, true);
            sa.Track("abc12311231", "buyPhone");
            sa.Track("abc12311231", "buyPhone2");
            //sa.Track("abc12311231", "buyPhone3");
            //sa.Track("abc12311231", "buyPhone4"); 
            //sa.Track("abc12311231", "buyPhone5");
            Console.WriteLine("等待 50 秒钟");
            Thread.Sleep(50000);
            Console.WriteLine("结束等待 50 秒钟");
            sa.Shutdown();
            Console.WriteLine("end all");
        }

        static void testNewClientConsumer1()
        {
            IConsumer consumer = new NewClientConsumer("http://10.129.28.106:8106/sa?project=default", "/Users/zhangwei/consumer/sss.txt",5,10000,null,ScheduledStyle.DISABLED,20000);
            sa = new SensorsAnalytics(consumer, true);
            Dictionary<String, object> properties = new Dictionary<string, object>();
            properties.Add("p1", 11);
            sa.Track("abc12311231", "buyPhone");
            sa.Track("abc12311231", "buyPhone2");
            Console.WriteLine("结束 track");
            Thread.Sleep(30000);
            Console.WriteLine("开始 track");
            sa.Flush();
            Thread.Sleep(30000);
            sa.Track("abc12311231", "buyPhone31");
            sa.Track("abc12311231", "buyPhone32");
            sa.Track("abc12311231", "buyPhone33");
            sa.Track("abc12311231", "buyPhone34");
            sa.Track("abc12311231", "buyPhone35");
            Thread.Sleep(30000);
            sa.Shutdown();
            Console.WriteLine("结束 track");
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

            IConsumer consumer = new NewClientConsumer("http://10.129.20.62:8106/sa?project=default", "/Users/zhangwei/consumer/sss.txt", 10, 10 * 1000);
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