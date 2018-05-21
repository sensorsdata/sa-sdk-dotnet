using System;
using System.Collections.Generic;
using SensorsData.Analytics;

namespace TestSensorsAnalytics
{


    class Program
    {
        static void Main(string[] args)
        {
            IConsumer consumer = new LoggingConsumer("D:/test", "wsc");
            SensorsAnalytics sa = new SensorsAnalytics(consumer, true);

            Console.WriteLine("1:");
            sa.Track("333", "helloword");

            Console.WriteLine("2:");
            Dictionary<string, Object> dic2 = new Dictionary<string, object>();
            dic2.Add("wsc1", "王守闯");
            sa.Track("333", "helloword1", dic2);

            Console.WriteLine("3:");
            sa.TrackSignUp("333-444", "333");

            Console.WriteLine("4:");
            Dictionary<string, Object> dic4 = new Dictionary<string, object>();
            dic2.Add("mingcheng", "王守闯");
            sa.TrackSignUp("333-444", "333", dic2);

            Console.WriteLine("5:");
            Dictionary<string, Object> dic5 = new Dictionary<string, object>();
            dic5.Add("mingcheng", "花肚皮");
            sa.ProfileSet("333-444", dic5);

            Console.WriteLine("6:");
            sa.ProfileSet("333-444", "mingcheng", "花肚皮update");


            Console.WriteLine("7:");
            sa.ProfileIncrement("333-444", "nnlingling", 30);

            Console.WriteLine("8:");
            Dictionary<string, Object> dic8 = new Dictionary<string, object>();
            List<string> like = new List<string>();
            like.Add("苹果");
            like.Add("橘子");
            dic8.Add("mingcheng", "花肚皮1");
            dic8.Add("like", like);
            sa.ProfileSet("333-444", dic8);

            Console.WriteLine("9:");
            Dictionary<string, Object> dic9 = new Dictionary<string, object>();
            List<string> like9 = new List<string>();
            like.Add("苹果1");
            like.Add("橘子1");
            dic9.Add("like", like9);
            sa.ProfileAppend("333-444", dic9);

            Console.WriteLine("10:");
            Dictionary<string, Object> dic10 = new Dictionary<string, object>();
            dic10.Add("sbyte1", (sbyte)1);
            dic10.Add("short1", (short)1);
            dic10.Add("int1", (int)1);
            dic10.Add("long1", (long)1);
            dic10.Add("byte1", (byte)1);
            dic10.Add("ushort1", (ushort)1);
            dic10.Add("uint1", (uint)1);
            dic10.Add("ulong1", (ulong)1);
            dic10.Add("decimal1", (decimal)1);
            dic10.Add("Single1", (Single)1);
            dic10.Add("float1", (float)1);
            dic10.Add("double1", (double)1);
            dic10.Add("string1", "string");
            dic10.Add("boolean1", true);
            dic10.Add("DateTime4", DateTime.Now);
            List<string> list = new List<string>();
            list.Add("逢佳节");
            list.Add("稳德福");
            dic10.Add("list1", list);

            sa.Track("555", "wsc_type", dic10);
            sa.ProfileSet("555", dic10);

            sa.Shutdown();
            Console.ReadLine();
        }
    }
}