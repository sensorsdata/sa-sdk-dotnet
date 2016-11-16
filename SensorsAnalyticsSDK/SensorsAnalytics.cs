using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Text;
using System.Text.RegularExpressions;

namespace SensorsData.Analytics
{
    public class SensorsAnalytics
    {
        private static readonly String SDK_VERSION = "1.0";
        private static readonly Regex KEY_PATTERN = new Regex("^((?!^distinct_id$|^original_id$|^time$|^properties$|^id$|^first_id$|^second_id$|^users$|^events$|^event$|^user_id$|^date$|^datetime$)[a-zA-Z_$][a-zA-Z\\d_$]{0,99})$", RegexOptions.IgnoreCase);
        private static readonly DateTime Jan1st1970 = TimeZone.CurrentTimeZone.ToLocalTime(new DateTime(1970, 1, 1, 0, 0, 0, 0));
        private IConsumer consumer;
        private ConcurrentDictionary<String, Object> superProperties;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="consumer"></param>
        public SensorsAnalytics(IConsumer consumer)
        {
            this.consumer = consumer;
            this.superProperties = new ConcurrentDictionary<String, Object>();
            this.ClearSuperProperties();
        }

        /// <summary>
        /// 设置每个事件都带有的一些公共属性
        /// </summary>
        /// <remarks>
        /// 当track的Properties，superProperties和SDK自动生成的automaticProperties有相同的key时，遵循如下的优先级：
        ///     track.properties 高于 superProperties 高于 automaticProperties
        /// 
        /// 另外，当这个接口被多次调用时，是用新传入的数据去merge先前的数据
        /// 例如，在调用接口前，dict是 {"a":1, "b": "bbb"}，传入的dict是 {"b": 123, "c": "asd"}，则merge后的结果是 {"a":1, "b": 123, "c": "asd"}
        /// </remarks>
        /// <param name="properties">一个或多个公共属性</param>
        public void RegisterSuperPropperties(ConcurrentDictionary<String, Object> properties)
        {
            foreach (KeyValuePair<String, Object> kvp in properties)
            {
                this.superProperties.AddOrUpdate(kvp.Key, kvp.Value, (k, v) => kvp.Value);
            }
        }

        /// <summary>
        /// 清除公共属性
        /// </summary>
        public void ClearSuperProperties()
        {
            this.superProperties.Clear();
            this.superProperties.AddOrUpdate("$lib", "DotNET", (k, v) => "DotNET");
            this.superProperties.AddOrUpdate("$lib_version", SensorsAnalytics.SDK_VERSION, (k, v) => SensorsAnalytics.SDK_VERSION);
            this.superProperties.AddOrUpdate("$lib_method", "code", (k, v) => "code");
        }

        /// <summary>
        /// 记录一个没有任何属性的事件
        /// </summary>
        /// <param name="distinctId">用户ID</param>
        /// <param name="eventName">事件名称</param>
        public void Track(String distinctId, String eventName)
        {
            AddEvent(distinctId, null, "track", eventName, null);
        }

        /// <summary>
        /// 记录一个拥有一个或多个属性的事件。
        /// 属性取值可接受类型为Number, String, Date和List，若属性包含 $time 字段，
        /// 则它会覆盖事件的默认时间属性，该字段只接受DateTime类型
        /// </summary>
        /// <param name="distinctId">用户ID</param>
        /// <param name="eventName">事件名称</param>
        /// <param name="properties">事件的属性集合</param>
        public void Track(String distinctId, String eventName, Dictionary<String, Object> properties)
        {
            AddEvent(distinctId, null, "track", eventName, properties);
        }

        /// <summary>
        /// 记录用户注册事件
        /// 这个接口是一个较为复杂的功能，请在使用前先阅读相关说明:
        /// http://www.sensorsdata.cn/manual/track_signup.html
        /// 并在必要时联系我们的技术支持人员。
        /// </summary>
        /// <param name="distinctId">新的用户ID</param>
        /// <param name="originDistinctId">旧的用户ID</param>
        public void TrackSignUp(String distinctId, String originDistinctId)
        {
            AddEvent(distinctId, originDistinctId, "track_signup", "$SignUp", null);
        }

        /// <summary>
        /// 记录用户注册事件
        /// 这个接口是一个较为复杂的功能，请在使用前先阅读相关说明:
        /// http://www.sensorsdata.cn/manual/track_signup.html
        /// 并在必要时联系我们的技术支持人员。
        /// </summary>
        /// <param name="distinctId">新的用户ID</param>
        /// <param name="originDistinctId">旧的用户ID</param>
        /// <param name="properties">事件的属性集合</param>
        public void TrackSignUp(String distinctId, String originDistinctId, Dictionary<String, Object> properties)
        {
            AddEvent(distinctId, originDistinctId, "track_signup", "$SignUp", properties);
        }

        /// <summary>
        /// 设置用户的属性。属性取值可接受类型为Number, String, Date和List<String>，
        /// 若属性包含 $time 字段，则它会覆盖事件的默认时间属性，该字段只接受DateTime 类型
        /// 如果要设置的properties的key，之前在这个用户的profile中已经存在，则覆盖，否则，新创建
        /// </summary>
        /// <param name="distinctId">用户ID</param>
        /// <param name="properties">用户的属性集合</param>
        public void ProfileSet(String distinctId, Dictionary<String, Object> properties)
        {
            AddEvent(distinctId, null, "profile_set", null, properties);
        }

        /// <summary>
        /// 设置用户的属性。这个接口只能设置单个key对应的内容，同样，如果已经存在，则覆盖，否则，新创建
        /// </summary>
        /// <param name="distinctId">用户ID</param>
        /// <param name="property">属性名称</param>
        /// <param name="value">属性的值</param>
        public void ProfileSet(String distinctId, String property, Object value)
        {
            Dictionary<String, Object> properties = new Dictionary<String, Object>();
            properties.Add(property, value);
            AddEvent(distinctId, null, "profile_set", null, properties);
        }

        /// <summary>
        /// 首次设置用户的属性。
        /// 属性取值可接受类型为Number, String, DateTime和List，
        /// 若属性包含 $time 字段，则它会覆盖事件的默认时间属性，该字段只接受DateTime类型。
        /// 与profileSet接口不同的是：如果要设置的properties的key，在这个用户的profile中已经存在，则不处理，否则，新创建
        /// </summary>
        /// <param name="distinctId">用户ID</param>
        /// <param name="properties">用户属性集合</param>
        public void ProfileSetOnce(String distinctId, Dictionary<String, Object> properties)
        {
            AddEvent(distinctId, null, "profile_set_once", null, properties);
        }

        /// <summary>
        /// 首次设置用户的属性。这个接口只能设置单个key对应的内容。
        /// 与profileSet接口不同的是，如果key的内容之前已经存在，则不处理，否则，重新创建
        /// </summary>
        /// <param name="distinctId">用户ID</param>
        /// <param name="property">属性名称</param>
        /// <param name="value">属性的值</param>
        public void ProfileSetOnce(String distinctId, String property, Object value)
        {
            Dictionary<String, Object> properties = new Dictionary<String, Object>();
            properties.Add(property, value);
            AddEvent(distinctId, null, "profile_set_once", null, properties);
        }

        /// <summary>
        /// 为用户的一个或多个数值类型的属性累加一个数值，若该属性不存在，则创建它并设置默认值为0。属性取值只接受Number类型
        /// </summary>
        /// <param name="distinctId">用户ID</param>
        /// <param name="properties">用户的属性</param>
        public void ProfileIncrement(String distinctId, Dictionary<String, Object> properties)
        {
            AddEvent(distinctId, null, "profile_increment", null, properties);
        }

        /// <summary>
        /// 为用户的数值类型的属性累加一个数值，若该属性不存在，则创建它并设置默认值为0
        /// </summary>
        /// <param name="distinctId">用户ID</param>
        /// <param name="property">属性名称</param>
        /// <param name="value">属性的值</param>
        public void ProfileIncrement(String distinctId, String property, long value)
        {
            Dictionary<String, Object> properties = new Dictionary<String, Object>();
            properties.Add(property, value);
            AddEvent(distinctId, null, "profile_increment", null, properties);
        }

        /// <summary>
        /// 为用户的一个或多个数组类型的属性追加字符串，属性取值类型必须为 List，
        /// 且列表中元素的类型必须为 String
        /// </summary>
        /// <param name="distinctId">用户ID</param>
        /// <param name="properties">用户的属性</param>
        public void ProfileAppend(String distinctId, Dictionary<String, Object> properties)
        {
            AddEvent(distinctId, null, "profile_append", null, properties);
        }

        /// <summary>
        /// 为用户的数组类型的属性追加一个字符串
        /// </summary>
        /// <param name="distinctId">用户ID</param>
        /// <param name="property">属性名称</param>
        /// <param name="value">属性的值</param>
        public void ProfileAppend(String distinctId, String property, String value)
        {
            ArrayList values = new ArrayList();
            values.Add(value);
            Dictionary<String, Object> properties = new Dictionary<String, Object>();
            properties.Add(property, values);
            AddEvent(distinctId, null, "profile_append", null, properties);
        }

        /// <summary>
        /// 删除用户某一个属性
        /// </summary>
        /// <param name="distinctId">用户ID</param>
        /// <param name="property">属性名称</param>
        public void ProfileUnset(String distinctId, String property)
        {
            Dictionary<String, Object> properties = new Dictionary<String, Object>();
            properties.Add(property, true);
            AddEvent(distinctId, null, "profile_unset", null, properties);
        }

        /// <summary>
        /// 删除用户所有属性
        /// </summary>
        /// <param name="distinctId">用户ID</param>
        public void ProfileDelete(String distinctId)
        {
            AddEvent(distinctId, null, "profile_delete", null, new Dictionary<String, Object>());
        }

        /// <summary>
        /// 立即发送缓存中的所有日志
        /// </summary>
        public void Flush()
        {
            this.consumer.Flush();
        }

        public void Shutdown()
        {
            this.consumer.Close();
        }


        private void AssertKey(String type, String key)
        {
            if (key == null || key.Length < 1)
            {
                throw new ArgumentNullException("The " + type + " is empty.");
            }
            if (key.Length > 255)
            {
                throw new ArgumentOutOfRangeException("The " + type + " is too long, max length is 255.");
            }
        }

        private void AssertKeyWithRegex(String type, String key)
        {
            AssertKey(type, key);
            if (!KEY_PATTERN.IsMatch(key))
            {
                throw new ArgumentException("The " + type + "'" + key + "' is invalid.");
            }
        }

        private bool IsNumber(Object value)
        {
            return (value is sbyte) || (value is short) || (value is int) || (value is long) || (value is byte)
                    || (value is ushort) || (value is uint) || (value is ulong) || (value is decimal) || (value is Single)
                    || (value is float) || (value is double);
        }

        private void AssertProperties(String eventType, Dictionary<String, Object> properties)
        {
            if (null == properties)
            {
                return;
            }
            foreach (KeyValuePair<String, Object> kvp in properties)
            {
                string key = kvp.Key;
                Object value = kvp.Value;

                AssertKeyWithRegex("property", kvp.Key);

                if (!this.IsNumber(value) && !(value is string) && !(value is DateTime) && !(value is bool) && !(value is List<string>))
                {
                    throw new ArgumentException("The property value should be a basic type: Number, String, Date, Boolean, List<String>.");
                }

                if (key == "$time" && !(value is DateTime))
                {
                    throw new ArgumentException("The property value of key '$time' should be a java.util.Date type.");
                }

                // String 类型的属性值，长度不能超过 8192
                if ((value is string) && value != null && ((string)value).Length > 8191)
                {
                    properties[key] = ((string)value).Substring(0, 8191);
                }

                if (eventType == "profile_increment" && !this.IsNumber(value))
                {
                    throw new ArgumentException("The property value of PROFILE_INCREMENT should be a Number.");
                }

                if (eventType == "profile_append" && !(value is List<string>))
                {
                    throw new ArgumentException("The property value of PROFILE_INCREMENT should be a List<String>.");
                }
            }

        }

        private Dictionary<String, String> GetLibProperties()
        {
            Dictionary<String, String> libProperties = new Dictionary<String, String>();
            libProperties.Add("$lib", "DotNET");
            libProperties.Add("$lib_version", SDK_VERSION);
            libProperties.Add("$lib_method", "code");

            if (this.superProperties.ContainsKey("$app_version"))
            {
                libProperties.Add("$app_version", (String)this.superProperties["$app_version"]);
            }

            // 从当前向上数，第4层正好是用户的调用代码
            System.Diagnostics.StackFrame[] stackFrames = (new System.Diagnostics.StackTrace(true)).GetFrames();
            if (stackFrames.Length > 3)
            {
                string libDetail = stackFrames[3].GetMethod().ReflectedType.Name + "##" + stackFrames[3].GetMethod().Name +
                    "##" + stackFrames[3].GetFileName() + "##" + stackFrames[3].GetFileLineNumber();
                libProperties.Add("$lib_detail", libDetail);
            }

            return libProperties;
        }


        private void AddEvent(String distinctId, String originDistinceId, String actionType, String eventName, Dictionary<String, Object> properties)
        {
            AssertKey("Distinct Id", distinctId);
            AssertProperties(actionType, properties);
            if (actionType.Equals("track"))
            {
                AssertKey("Event Name", eventName);
                AssertKeyWithRegex("Event Name", eventName);
            }
            else if (actionType.Equals("track_signup"))
            {
                AssertKey("Original Distinct Id", originDistinceId);
            }

            // Event time
            long time = (long)(DateTime.UtcNow - Jan1st1970).TotalMilliseconds;
            if (properties != null && properties.ContainsKey("$time"))
            {
                DateTime eventTime = (DateTime)properties["$time"];
                properties.Remove("$time");
                time = (long)(eventTime - Jan1st1970).TotalMilliseconds;
            }

            Dictionary<String, Object> eventProperties = new Dictionary<String, Object>();
            if (actionType.Equals("track") || actionType.Equals("track_signup"))
            {
                foreach (KeyValuePair<String, Object> kvp in superProperties)
                {
                    eventProperties.Add(kvp.Key, kvp.Value);
                }
            }
            if (properties != null)
            {
                foreach (KeyValuePair<String, Object> kvp in properties)
                {
                    if (eventProperties.ContainsKey(kvp.Key))
                    {
                        eventProperties.Remove(kvp.Key);
                    }

                    if (kvp.Value is DateTime)
                    {
                        eventProperties.Add(kvp.Key, ((DateTime)kvp.Value).ToString("yyyy-MM-dd HH:mm:ss.fff"));
                    }
                    else
                    {
                        eventProperties.Add(kvp.Key, kvp.Value);
                    }
                }
            }

            Dictionary<String, String> libProperties = GetLibProperties();
            Dictionary<String, Object> evt = new Dictionary<String, Object>();
            evt.Add("type", actionType);
            evt.Add("time", time);
            evt.Add("distinct_id", distinctId);
            evt.Add("properties", eventProperties);
            evt.Add("lib", libProperties);


            if (actionType.Equals("track"))
            {
                evt.Add("event", eventName);
            }
            else if (actionType.Equals("track_signup"))
            {
                evt.Add("event", eventName);
                evt.Add("original_id", originDistinceId);
            }

            this.consumer.Send(evt);
        }
    }
}
