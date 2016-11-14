using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Web.Script.Serialization;
using log4net;
using log4net.Appender;
using log4net.Repository.Hierarchy;
using log4net.Layout;

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

    public class LoggingConsumer : IConsumer
    {
        private static readonly Regex KEY_PATTERN = new Regex("^((?!^distinct_id$|^original_id$|^time$|^properties$|^id$|^first_id$|^second_id$|^users$|^events$|^event$|^user_id$|^date$|^datetime$)[a-zA-Z_$][a-zA-Z\\d_$]{0,99})$", RegexOptions.IgnoreCase);
        private static RollingFileAppender roller;
        private static ILog logger;
        private string project = "";


        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="logPath">日志文件存放的路径</param>
        public LoggingConsumer(string logPath)
        {
            this.Init(logPath);
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="logPath">日志文件存放的路径</param>
        /// <param name="project">合法变量名且长度不超过255</param>
        public LoggingConsumer(string logPath, string project)
        {
            if (project == null || project.Length == 0 || !KEY_PATTERN.IsMatch(project) || project.Length > 255)
            {
                throw new ArgumentException("The project is invalid.");
            }
            this.project = project;
            this.Init(logPath);
        }

        public void Send(Dictionary<string, Object> message)
        {
            if (this.project != null && this.project.Length > 0)
            {
                message.Add("project", this.project);
            }
            JavaScriptSerializer js = new JavaScriptSerializer();
            logger.Info(js.Serialize(message));
        }

        public void Flush()
        {

        }

        public void Close()
        {
            roller.Close();
        }

        private void Init(string logPath)
        {
            string logName = "SensorsData.Analytics.LoggingConsumer";

            Hierarchy hierarchy = (Hierarchy)LogManager.GetRepository();
            hierarchy.Name = logName;
            TraceAppender tracer = new TraceAppender();

            PatternLayout patternLayout = new PatternLayout();
            patternLayout.ConversionPattern = "%message%newline";
            patternLayout.ActivateOptions();

            tracer.Layout = patternLayout;
            tracer.ActivateOptions();
            hierarchy.Root.AddAppender(tracer);

            roller = new RollingFileAppender();
            roller.Layout = patternLayout;
            roller.AppendToFile = true;
            roller.StaticLogFileName = false;
            roller.File = logPath;
            roller.RollingStyle = RollingFileAppender.RollingMode.Date;
            roller.DatePattern = @"yyyyMMdd'.txt'";
            roller.Encoding = Encoding.UTF8;
            roller.ActivateOptions();

            hierarchy.Root.AddAppender(roller);
            hierarchy.Root.Level = log4net.Core.Level.All;
            hierarchy.Configured = true;

            logger = LogManager.GetLogger(logName);
        }
    }

}
