using System;
using System.Collections.Generic;
using SensorsData.Analytics;
using Xunit;

namespace SAUnitTest
{
    public class UnitTest1
    {
        [Fact]
        public void Bind_Test()
        {
            IConsumer consumer = new LoggingConsumer("/Users/zhangwei/consumer");
            SensorsAnalytics sa = new SensorsAnalytics(consumer, true);
            sa.Bind(SensorsAnalyticsIdentityHelper.CreateBuilder()
                .AddIdentityProperty("ss1", "vv1")
                .AddIdentityProperty("ss2", "vv2").Build());
            sa.Flush();
            sa.Shutdown();
        }

        [Fact]
        public void Bind_With_LOGINID_Test()
        {
            IConsumer consumer = new LoggingConsumer("/Users/zhangwei/consumer");
            SensorsAnalytics sa = new SensorsAnalytics(consumer, true);
            sa.Bind(SensorsAnalyticsIdentityHelper.CreateBuilder()
                .AddIdentityProperty("ss1", "vv1")
                .AddIdentityProperty(SensorsAnalyticsIdentity.LOGIN_ID, "xiaoming")
                .Build());
            sa.Flush();
            sa.Shutdown();
        }

        [Fact]
        public void BindException_Test()
        {
            IConsumer consumer = new LoggingConsumer("/Users/zhangwei/consumer");
            SensorsAnalytics sa = new SensorsAnalytics(consumer, true);
            Action act = () => sa.Bind(SensorsAnalyticsIdentityHelper.CreateBuilder().AddIdentityProperty("ss2", "vv2").Build());
            Assert.ThrowsAny<Exception>(act);
            sa.Flush();
            sa.Shutdown();
        }

        [Fact]
        public void BindException2_Test()
        {
            IConsumer consumer = new LoggingConsumer("/Users/zhangwei/consumer");
            SensorsAnalytics sa = new SensorsAnalytics(consumer, true);
            Action act = () => sa.Bind(SensorsAnalyticsIdentityHelper.CreateBuilder().AddIdentityProperty("   ", "2vv1").AddIdentityProperty("1ss2", " ").Build());
            Assert.ThrowsAny<Exception>(act);
            sa.Flush();
            sa.Shutdown();
        }

        [Fact]
        public void UnBind_Test()
        {
            IConsumer consumer = new LoggingConsumer("/Users/zhangwei/consumer");
            SensorsAnalytics sa = new SensorsAnalytics(consumer, true);
            sa.Unbind(new SensorsAnalyticsIdentity("ss2", "vv2"));
            sa.Flush();
            sa.Shutdown();
        }

        [Fact]
        public void UnBindException_Test()
        {
            IConsumer consumer = new LoggingConsumer("/Users/zhangwei/consumer");
            SensorsAnalytics sa = new SensorsAnalytics(consumer, true);
            Action act1 = () => sa.Unbind(new SensorsAnalyticsIdentity("ss", ""));
            Assert.ThrowsAny<Exception>(act1);
            Action act2 = () => sa.Unbind(null);
            Assert.ThrowsAny<Exception>(act2);
            Action act3 = () => sa.Unbind(new SensorsAnalyticsIdentity("", "vv"));
            Assert.ThrowsAny<Exception>(act3);
            Action act4 = () => sa.Unbind(new SensorsAnalyticsIdentity(null, "vv"));
            Assert.ThrowsAny<Exception>(act4);
            Action act5 = () => sa.Unbind(new SensorsAnalyticsIdentity("ss", null));
            Assert.ThrowsAny<Exception>(act5);
            sa.Flush();
            sa.Shutdown();
        }

        [Fact]
        public void TrackByID_Test()
        {
            IConsumer consumer = new LoggingConsumer("/Users/zhangwei/consumer");
            SensorsAnalytics sa = new SensorsAnalytics(consumer, true);
            Dictionary<String, object> properties = new Dictionary<string, object>();
            properties.Add("k1", "v1");
            sa.TrackById(new SensorsAnalyticsIdentity(SensorsAnalyticsIdentity.LOGIN_ID, "v11"), "hello", properties);
            sa.Flush();
            sa.Shutdown();
        }

        [Fact]
        public void Track_Test()
        {
            IConsumer consumer = new LoggingConsumer("/Users/zhangwei/consumer");
            SensorsAnalytics sa = new SensorsAnalytics(consumer, true);
            Dictionary<String, object> properties = new Dictionary<string, object>();
            properties.Add("k1", "v1");
            sa.Track("abc123", "hello", properties);
            sa.Flush();
            sa.Shutdown();
        }

        [Fact]
        public void TrackByIDException_Test()
        {
            IConsumer consumer = new LoggingConsumer("/Users/zhangwei/consumer");
            SensorsAnalytics sa = new SensorsAnalytics(consumer, true);
            Dictionary<String, object> properties = new Dictionary<string, object>();

            Action act1 = () => sa.TrackById(null, "hello", properties);
            Assert.ThrowsAny<Exception>(act1);
            Action act2 = () => sa.TrackById(new SensorsAnalyticsIdentity("k11", "v11"), "", properties);
            Assert.ThrowsAny<Exception>(act2);
            Action act3 = () => sa.TrackById(new SensorsAnalyticsIdentity("", "v11"), "hello", properties);
            Assert.ThrowsAny<Exception>(act3);
            Action act4 = () => sa.TrackById(new SensorsAnalyticsIdentity("k11", ""), "hello", properties);
            Assert.ThrowsAny<Exception>(act4);
            Action act5 = () => sa.TrackById(new SensorsAnalyticsIdentity(null, "v11"), "hello", properties);
            Assert.ThrowsAny<Exception>(act5);

            sa.Flush();
            sa.Shutdown();
        }


        [Fact]
        public void ProfileSetById_Test()
        {
            IConsumer consumer = new LoggingConsumer("/Users/zhangwei/consumer");
            SensorsAnalytics sa = new SensorsAnalytics(consumer, true);
            Dictionary<String, object> properties = new Dictionary<string, object>();
            properties.Add("p1", "pv1");
            sa.ProfileSetById(SensorsAnalyticsIdentityHelper
                .CreateBuilder()
                .AddIdentityProperty("ss1", "vv1")
                .AddIdentityProperty(SensorsAnalyticsIdentity.LOGIN_ID, "vv2").Build(), properties);
            sa.Flush();
            sa.Shutdown();
        }

        [Fact]
        public void ProfileSetOnceById_Test()
        {
            IConsumer consumer = new LoggingConsumer("/Users/zhangwei/consumer");
            SensorsAnalytics sa = new SensorsAnalytics(consumer, true);
            Dictionary<String, object> properties = new Dictionary<string, object>();
            properties.Add("p1", "pv1");
            sa.ProfileSetOnceById(new SensorsAnalyticsIdentity("ss1", "vv1"), properties);
            sa.Flush();
            sa.Shutdown();
        }

        [Fact]
        public void ProfileSetOnceByIDException_Test()
        {
            IConsumer consumer = new LoggingConsumer("/Users/zhangwei/consumer");
            SensorsAnalytics sa = new SensorsAnalytics(consumer, true);
            Dictionary<String, object> properties = new Dictionary<string, object>();

            Action act3 = () => sa.ProfileSetOnceById(new SensorsAnalyticsIdentity("", "v11"), properties);
            Assert.ThrowsAny<Exception>(act3);
            Action act4 = () => sa.ProfileSetOnceById(new SensorsAnalyticsIdentity("k11", ""), properties);
            Assert.ThrowsAny<Exception>(act4);
            Action act5 = () => sa.ProfileSetOnceById(new SensorsAnalyticsIdentity(null, "v11"), properties);
            Assert.ThrowsAny<Exception>(act5);
            sa.Flush();
            sa.Shutdown();
        }


        [Fact]
        public void ProfileAppendById_Test()
        {
            IConsumer consumer = new LoggingConsumer("/Users/zhangwei/consumer");
            SensorsAnalytics sa = new SensorsAnalytics(consumer, true);
            Dictionary<String, object> properties = new Dictionary<string, object>();
            properties.Add("p1", new List<String>() { "v1", "v2" });
            sa.ProfileAppendById(new SensorsAnalyticsIdentity("ss1", "vv1"), properties);

            sa.Flush();
            sa.Shutdown();
        }

        [Fact]
        public void ProfileAppendByIdException_Test()
        {
            IConsumer consumer = new LoggingConsumer("/Users/zhangwei/consumer");
            SensorsAnalytics sa = new SensorsAnalytics(consumer, true);
            Dictionary<String, object> properties = new Dictionary<string, object>();
            properties.Add("p1", new List<String>() { "v1", "v2" });
            //sa.ProfileAppendById(new List<SensorsAnalyticsIdentity>(), properties);
            Action act1 = () => sa.ProfileAppendById(new List<SensorsAnalyticsIdentity>(), properties);
            Assert.ThrowsAny<Exception>(act1);

            sa.Flush();
            sa.Shutdown();
        }


        [Fact]
        public void ProfileUnsetById_Test()
        {
            IConsumer consumer = new LoggingConsumer("/Users/zhangwei/consumer");
            SensorsAnalytics sa = new SensorsAnalytics(consumer, true);
            Dictionary<String, object> properties = new Dictionary<string, object>();
            properties.Add("p1", new List<String>() { "v1", "v2" });
            sa.ProfileUnsetById(new SensorsAnalyticsIdentity("ss1", "vv1"), "sss1");

            sa.Flush();
            sa.Shutdown();
        }

        [Fact]
        public void ProfileDeleteById_Test()
        {
            IConsumer consumer = new LoggingConsumer("/Users/zhangwei/consumer");
            SensorsAnalytics sa = new SensorsAnalytics(consumer, true);
            sa.ProfileDeleteById("ss1", "vv1");
            sa.Flush();
            sa.Shutdown();
        }


        [Fact]
        public void ProfileDeleteByIdException_Test()
        {
            IConsumer consumer = new LoggingConsumer("/Users/zhangwei/consumer");
            SensorsAnalytics sa = new SensorsAnalytics(consumer, true);
            Action act1 = () => sa.ProfileDeleteById(null, "vv1");
            Assert.ThrowsAny<Exception>(act1);
            Action act2 = () => sa.ProfileDeleteById(null, null);
            Assert.ThrowsAny<Exception>(act2);
            Action act3 = () => sa.ProfileDeleteById("ss", null);
            Assert.ThrowsAny<Exception>(act3);
            sa.Flush();
            sa.Shutdown();
        }


        [Fact]
        public void ProfileIncrementById_Test()
        {
            IConsumer consumer = new LoggingConsumer("/Users/zhangwei/consumer");
            SensorsAnalytics sa = new SensorsAnalytics(consumer, true);
            Dictionary<String, object> properties = new Dictionary<string, object>();
            properties.Add("p1", 11);
            sa.ProfileIncrementById(new SensorsAnalyticsIdentity("ss1", "vv1"), properties);

            sa.Flush();
            sa.Shutdown();
        }

    }
}
