using System;
using System.Collections.Generic;

/// <summary>
/// IDM 3.0 相关的资源
/// </summary>
namespace SensorsData.Analytics
{


    public partial class SensorsAnalytics
    {
        /// <summary>
        /// 绑定用户标识
        /// </summary>
        /// <param name="identities">用户标识 List</param>
        public void Bind(List<SensorsAnalyticsIdentity> identities)
        {
            if (identities == null || identities.Count < 2)
            {
                throw new ArgumentException("The identities is invalid，you should have at least two identities.");
            }
            CheckIdentityValid(identities, "track_id_bind");
            AddEvent(identities, GetDistinctId(identities), null, "track_id_bind", "$BindID", null);
        }

        /// <summary>
        /// 解绑用户标识
        /// </summary>
        /// <param name="identity">用户标识</param>
        public void Unbind(SensorsAnalyticsIdentity identity)
        {
            CheckIdentityValid(identity, "track_id_unbind");
            List<SensorsAnalyticsIdentity> identities = new List<SensorsAnalyticsIdentity>()
            {
                identity
            };
            AddEvent(identities, GetDistinctId(identities), null, "track_id_unbind", "$UnbindID", null);
        }

        /// <summary>
        /// 解绑用户标识
        /// </summary>
        /// <param name="key">用户标识 key</param>
        /// <param name="value">用户标识 value</param>
        public void Unbind(String key, String value)
        {
            Unbind(new SensorsAnalyticsIdentity(key, value));
        }

        /// <summary>
        /// 使用用户标识 3.0 方式进行时间埋点
        /// </summary>
        /// <param name="identity">用户标识</param>
        /// <param name="eventName">事件名</param>
        /// <param name="properties">事件属性</param>
        public void TrackById(SensorsAnalyticsIdentity identity, String eventName, Dictionary<String, Object> properties)
        {
            CheckIdentityValid(identity, "track");
            List<SensorsAnalyticsIdentity> identities = new List<SensorsAnalyticsIdentity>()
            {
                identity
            };
            AddEvent(identities, GetDistinctId(identities), null, "track", eventName, properties);
        }

        /// <summary>
        /// 设置用户的属性。如果要设置的 properties 的 key，之前在这个用户的 profile 中已经存在，则覆盖，否则，新创建
        /// </summary>
        /// <param name="identities">用户标识 List</param>
        /// <param name="properties">用户属性</param>
        public void ProfileSetById(List<SensorsAnalyticsIdentity> identities, Dictionary<String, Object> properties)
        {
            ProfileOptionById(identities, "profile_set", properties);
        }

        /// <summary>
        /// 设置用户的属性。如果要设置的 properties 的 key，之前在这个用户的 profile 中已经存在，则覆盖，否则，新创建
        /// </summary>
        /// <param name="identity">用户标识</param>
        /// <param name="properties">用户属性</param>
        public void ProfileSetById(SensorsAnalyticsIdentity identity, Dictionary<String, Object> properties)
        {
            List<SensorsAnalyticsIdentity> identities = new List<SensorsAnalyticsIdentity>()
            {
                identity
            };
            ProfileSetById(identities, properties);
        }

        /// <summary>
        /// 首次设置用户的属性。与  ProfileSetById 接口不同的是：如果要设置的 properties 的 key，在这个用户的 profile 中已经存在，则不处理，否则，新创建
        /// </summary>
        /// <param name="identities">用户标识 List</param>
        /// <param name="properties">用户属性</param>
        public void ProfileSetOnceById(List<SensorsAnalyticsIdentity> identities, Dictionary<String, Object> properties)
        {
            ProfileOptionById(identities, "profile_set_once", properties);
        }

        /// <summary>
        /// 首次设置用户的属性。与  ProfileSetById 接口不同的是：如果要设置的 properties 的 key，在这个用户的 profile 中已经存在，则不处理，否则，新创建
        /// </summary>
        /// <param name="identity">用户标识</param>
        /// <param name="properties">用户属性</param>
        public void ProfileSetOnceById(SensorsAnalyticsIdentity identity, Dictionary<String, Object> properties)
        {
            List<SensorsAnalyticsIdentity> identities = new List<SensorsAnalyticsIdentity>()
            {
                identity
            };
            ProfileSetOnceById(identities, properties);
        }

        /// <summary>
        /// 删除用户某一个属性
        /// </summary>
        /// <param name="identities">用户标识 List</param>
        /// <param name="propertyName">用户属性名</param>
        public void ProfileUnsetById(List<SensorsAnalyticsIdentity> identities, String propertyName)
        {
            Dictionary<String, Object> properties = new Dictionary<string, object>()
            {
                { propertyName, true }
            };
            ProfileOptionById(identities, "profile_unset", properties);
        }

        /// <summary>
        /// 删除用户某一个属性
        /// </summary>
        /// <param name="identity">用户标识</param>
        /// <param name="propertyName">用户属性名</param>
        public void ProfileUnsetById(SensorsAnalyticsIdentity identity, String propertyName)
        {
            List<SensorsAnalyticsIdentity> identities = new List<SensorsAnalyticsIdentity>()
            {
                identity
            };
            ProfileUnsetById(identities, propertyName);
        }

        /// <summary>
        /// 为用户的一个或多个数组类型的属性追加字符串.
        /// </summary>
        /// <param name="identities">用户标识 List</param>
        /// <param name="properties">用户属性</param>
        public void ProfileAppendById(List<SensorsAnalyticsIdentity> identities, Dictionary<String, Object> properties)
        {
            ProfileOptionById(identities, "profile_append", properties);
        }

        /// <summary>
        /// 为用户的一个或多个数组类型的属性追加字符串.
        /// </summary>
        /// <param name="identity">用户标识</param>
        /// <param name="properties">用户属性</param>
        public void ProfileAppendById(SensorsAnalyticsIdentity identity, Dictionary<String, Object> properties)
        {
            List<SensorsAnalyticsIdentity> identities = new List<SensorsAnalyticsIdentity>()
            {
                identity
            };
            ProfileAppendById(identities, properties);
        }

        /// <summary>
        /// 删除用户属性
        /// </summary>
        /// <param name="key">用户标识 key</param>
        /// <param name="value">用户标识 value</param>
        public void ProfileDeleteById(String key, String value)
        {
            ProfileOptionById(new SensorsAnalyticsIdentity(key, value), "profile_delete", null);
        }

        /// <summary>
        /// 为用户的一个或多个数值类型的属性累加一个数值，若该属性不存在，则创建它并设置默认值为0。属性取值只接受Number类型
        /// </summary>
        /// <param name="identities">用户标识 List</param>
        /// <param name="properties">用户属性</param>
        public void ProfileIncrementById(List<SensorsAnalyticsIdentity> identities, Dictionary<String, Object> properties)
        {
            ProfileOptionById(identities, "profile_increment", properties);
        }

        /// <summary>
        /// 为用户的一个或多个数值类型的属性累加一个数值，若该属性不存在，则创建它并设置默认值为0。属性取值只接受Number类型
        /// </summary>
        /// <param name="identity">用户标识</param>
        /// <param name="properties">用户属性</param>
        public void ProfileIncrementById(SensorsAnalyticsIdentity identity, Dictionary<String, Object> properties)
        {
            List<SensorsAnalyticsIdentity> identities = new List<SensorsAnalyticsIdentity>()
            {
                identity
            };
            ProfileIncrementById(identities, properties);
        }

        /// <summary>
        /// 为用户的数值类型的属性累加一个数值，若该属性不存在，则创建它并设置默认值为0
        /// </summary>
        /// <param name="identities">用户标识 List</param>
        /// <param name="property">属性名称</param>
        /// <param name="value">属性值</param>
        public void ProfileIncrementById(List<SensorsAnalyticsIdentity> identities, String property, long value)
        {
            Dictionary<String, Object> properties = new Dictionary<String, Object>();
            properties.Add(property, value);
            ProfileIncrementById(identities, properties);
        }

        /// <summary>
        /// 为用户的数值类型的属性累加一个数值，若该属性不存在，则创建它并设置默认值为0
        /// </summary>
        /// <param name="identity">用户标识</param>
        /// <param name="property">属性名称</param>
        /// <param name="value">属性值</param>
        public void ProfileIncrementById(SensorsAnalyticsIdentity identity, String property, long value)
        {
            List<SensorsAnalyticsIdentity> identities = new List<SensorsAnalyticsIdentity>()
            {
                identity
            };
            ProfileIncrementById(identities, property, value);
        }

        private void ProfileOptionById(SensorsAnalyticsIdentity identity, String type, Dictionary<String, Object> properties)
        {

            List<SensorsAnalyticsIdentity> identities = new List<SensorsAnalyticsIdentity>()
            {
                identity
            };
            ProfileOptionById(identities, type, properties);
        }

        private void ProfileOptionById(List<SensorsAnalyticsIdentity> identities, String type, Dictionary<String, Object> properties)
        {

            CheckIdentityValid(identities, type);
            AddEvent(identities, GetDistinctId(identities), null, type, null, properties);
        }

        private String GetDistinctId(List<SensorsAnalyticsIdentity> identities)
        {
            if (identities == null || identities.Count == 0)
            {
                return null;
            }
            String distinctId = identities[0].key + "+" + identities[0].value;
            foreach (SensorsAnalyticsIdentity item in identities)
            {
                if (SensorsAnalyticsIdentity.LOGIN_ID.Equals(item.key))
                {
                    distinctId = item.value;
                    break;
                }
            }

            return distinctId;
        }

        private void CheckIdentityValid(SensorsAnalyticsIdentity identity, String type)
        {
            if (identity == null)
            {
                throw new ArgumentException("Identity can not be null.");
            }
            CheckIdentityValid(new List<SensorsAnalyticsIdentity>() { identity }, type);
        }

        private void CheckIdentityValid(List<SensorsAnalyticsIdentity> identities, String type)
        {
            if (identities == null || identities.Count == 0)
            {
                throw new ArgumentException("Identity can not be null.");
            }
            foreach (SensorsAnalyticsIdentity item in identities)
            {

                AssertKeyWithRegex(type, item.key);
                AssertValue(type, item.value);
            }
        }

        private Dictionary<String, String> GetIdentitiesProperties(List<SensorsAnalyticsIdentity> identities)
        {
            Dictionary<String, String> result = new Dictionary<string, string>();
            foreach (SensorsAnalyticsIdentity item in identities)
            {
                result.Add(item.key, item.value);
            }

            return result;
        }
    }

    public class SensorsAnalyticsIdentity
    {
        /// <summary>
        /// 用户登录 
        /// </summary>
        public static readonly String LOGIN_ID = "$identity_login_id";
        /// <summary>
        /// 手机号
        /// </summary>
        public static readonly String MOBILE = "$identity_mobile";
        /// <summary>
        /// 邮箱
        /// </summary>
        public static readonly String EMAIL = "$identity_email";


        public String key;
        public String value;


        public SensorsAnalyticsIdentity(String key, String value)
        {
            this.key = key;
            this.value = value;
        }
    }

    public class SensorsAnalyticsIdentityHelper
    {

        public static Builder CreateBuilder()
        {
            return new Builder();
        }


        public class Builder
        {

            private List<SensorsAnalyticsIdentity> identities = new List<SensorsAnalyticsIdentity>();


            public Builder AddIdentityProperty(String key, String value)
            {
                this.identities.Add(new SensorsAnalyticsIdentity(key, value));
                return this;
            }

            public List<SensorsAnalyticsIdentity> Build()
            {
                return identities;
            }
        }
    }

}
