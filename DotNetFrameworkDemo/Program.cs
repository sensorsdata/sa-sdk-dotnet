using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SensorsData.Analytics;

namespace DotNetFrameworkDemo
{
    class Program
    {
        static void Main(string[] args)
        {

            testThread();
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

            IConsumer consumer = new NewClientConsumer("http://newsdktest.datasink.sensorsdata.cn/sa?project=zhangwei&token=5a394d2405c147ca", "/Users/zhangwei/consumer/sss.txt", 10, 10 * 1000);
            SensorsAnalytics sa = new SensorsAnalytics(consumer, true);
            Dictionary<string, Object> dic = new Dictionary<string, object>();
            dic.Add("productName", "iPhone 11");
            dic.Add("productPrice", "20000");
            Console.WriteLine("1111===" + DateTime.Now.ToLongTimeString());

            //sa.TrackSignUp("8888", "112131", dic);

            sa.Track("112131", "ViewProduct", dic);

            //sa.ItemSet("item_type1111", "item_id1111", dic);
            //sa.ItemDelete("item_type2222", "item_id2222");
            Console.WriteLine("3333===" + DateTime.Now.ToLongTimeString());

            sa.Flush();

            Console.WriteLine("222222");

            Thread.Sleep(5000);
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
            Thread.Sleep(2000);
            Console.WriteLine("--End--");
        }






        /// <summary>
        /// 测试网络请求失败以后重试
        /// </summary>
        static void testThread2()
        {
            IConsumer consumer = new NewClientConsumer("http://newsdktest.datasink.s22ensorsdata.cn/sa?project=zhangwei&token=5a394d2405c147ca", "/Users/zhangwei/consumer/sss.txt", 10, 10 * 1000);
            SensorsAnalytics sa = new SensorsAnalytics(consumer, true);

            Dictionary<string, Object> dic = new Dictionary<string, object>();
            dic.Add("productName", "iPhone 11");
            dic.Add("productPrice", "20000");
            sa.Track("112131", "ViewProduct", dic);
            sa.Track("112131", "ViewProduct22", dic);
            sa.Flush();
            Console.WriteLine("=====第一次 flush");
            Thread.Sleep(2000);
            Console.WriteLine("=====第二次 flush");
            sa.Flush();
            Thread.Sleep(3000);
           // sa.Shutdown();
            Console.WriteLine("=====111111");
            Thread.Sleep(10000);
            sa.Flush();
            sa.Shutdown();
            Console.WriteLine("=====333333");

        }


        /// <summary>
        /// 测试在任务执行中取消，然后等待任务介绍，需要在 ThreadRunner 中添加 sleep 模拟执行
        /// </summary>
        static void testThread1()
        {
            IConsumer consumer = new NewClientConsumer("http://newsdktest.datasink.sensorsdata.cn/sa?project=zhangwei&token=5a394d2405c147ca", "/Users/zhangwei/consumer/sss.txt", 10, 10 * 1000);
            SensorsAnalytics sa = new SensorsAnalytics(consumer, true);

            Dictionary<string, Object> dic = new Dictionary<string, object>();
            dic.Add("productName", "iPhone 11");
            dic.Add("productPrice", "20000");
            sa.Track("112131", "ViewProduct" , dic);
            sa.Flush();
            Console.WriteLine("=====222222");
            Thread.Sleep(1000);
            sa.Shutdown();
            Console.WriteLine("=====111111");
            Thread.Sleep(10000);
            Console.WriteLine("=====333333");
        }

        static void testThread33()
        {
            Console.WriteLine("44444444");

            IConsumer consumer = new NewClientConsumer("http://newsdktest.datasink.sensorsdata.cn/sa?project=zhangwei&token=5a394d2405c147ca", "/Users/zhangwei/consumer/sss.txt", 10, 10 * 1000);
            SensorsAnalytics sa = new SensorsAnalytics(consumer, true);

            Task t1 = Task.Run(() => {
                for (int i = 0; i < 2; i++)
                {
                    Dictionary<string, Object> dic = new Dictionary<string, object>();
                    dic.Add("productName", "iPhone 11");
                    dic.Add("productPrice", "20000");
                    sa.Track("112131", "Task11" + i, dic);
                }
            });

            Task t2 = Task.Run(() => {
                for (int i = 0; i < 2; i++)
                {
                    Dictionary<string, Object> dic = new Dictionary<string, object>();
                    dic.Add("productName", "iPhone 11");
                    dic.Add("productPrice", "20000");
                    sa.Track("112131", "Task22" + i, dic);
                    sa.Flush();
                }
            });

            Task t3 = Task.Run(() => {
                for (int i = 0; i < 2; i++)
                {
                    Dictionary<string, Object> dic = new Dictionary<string, object>();
                    dic.Add("productName", "iPhone 11");
                    dic.Add("productPrice", "20000");
                    sa.Track("112131", "Task33" + i, dic);
                    sa.Flush();
                }
            });


            Task.WaitAll(t1, t2, t3);
            sa.Flush();

            Console.WriteLine("222222");

            Thread.Sleep(15000);
            Console.WriteLine("1231231");
            sa.Shutdown();
            Console.WriteLine("--End--");

            Thread.Sleep(5000);


        }
    }
}
