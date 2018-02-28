using System;
using System.Collections.Specialized;
using System.Linq;
using System.Web;
using System.Web.Caching;
using System.Xml.Linq;

namespace Appsettings
{
    internal class AppSettingsValue : AppSettingsBase
    {
        /// <summary>
        /// 缓存Key
        /// </summary>
        private string Key
        {
            get { return "APPSETTINGSLIST_Default"; }
        }

        /// <summary>
        /// 获取自定义配置的数据。
        /// </summary>
        public NameValueCollection AppSettings
        {
            get
            {
                object appSettings = HttpRuntime.Cache.Get(Key);
                if (appSettings == null)
                {
                    appSettings = LoadToCache();
                }
                return (NameValueCollection)appSettings;
            }
        }

        /// <summary>
        /// 加载键值类型的自定义配置到缓存
        /// </summary>
        /// <returns></returns>
        public NameValueCollection LoadToCache()
        {
            try
            {
                var appSettings = GetAppSettings();
                if (HttpRuntime.Cache[Key] != null)
                {
                    HttpRuntime.Cache.Remove(Key);
                }

                if (appSettings != null && appSettings.Count > 0)
                {
                    CacheDependency cdd = new CacheDependency(XmlPaths); //缓存依赖文件
                    HttpRuntime.Cache.Insert(Key, appSettings, cdd, DateTime.MaxValue, Cache.NoSlidingExpiration);
                }
                return appSettings;
            }
            catch (Exception ex)
            {
                AppSettingsBase.Log(ex);
                throw ex;
            }
        }

        /// <summary>
        /// 加载键值类型的自定义配置
        /// </summary>
        /// <returns></returns>
        public NameValueCollection GetAppSettings()
        {
            NameValueCollection nv = new NameValueCollection();

            foreach (var xmlPath in XmlPaths)
            {
                if (xmlPath.EndsWithIgnoreCase(".xml"))
                {
                    ReadFromXML(nv, xmlPath);
                }
                else
                {
                    ReadFromProperties(nv, xmlPath);
                }
            }

            return nv;
        }

        /// <summary>
        /// 读取xml中的key value
        /// </summary>
        /// <param name="nv"></param>
        /// <param name="xmlPath"></param>
        void ReadFromXML(NameValueCollection nv, string xmlPath)
        {
            XDocument doc = AppSettingsUtils.LoadXml(xmlPath); //XDocument.Load(xmlPath);
            var appSettings = doc.Elements().FirstOrDefault(s => s.Name.LocalName.EqualsIgnoreCase("AppSettings"));
            if (appSettings == null) return;

            var adds = appSettings.Elements().Where(s => s.Name.LocalName.EqualsIgnoreCase("add")).ToList();
            foreach (XElement x in adds)
            {
                var key = x.Attributes().FirstOrDefault(s => s.Name.LocalName.EqualsIgnoreCase("key"));
                var value = x.Attributes().FirstOrDefault(s => s.Name.LocalName.EqualsIgnoreCase("value"));
                if (key != null && value != null)
                {
                    if (!nv.AllKeys.Contains(key.Value))
                    {
                        nv.Add(key.Value, value.Value);
                    }
                }
            }
        }

        /// <summary>
        /// 读取properties中的key value
        /// </summary>
        /// <param name="nv"></param>
        /// <param name="xmlPath"></param>
        void ReadFromProperties(NameValueCollection nv, string xmlPath)
        {
            var arr = AppSettingsUtils.LoadProperties(xmlPath); //File.ReadAllLines(xmlPath);
            foreach (var item in arr)
            {
                //空行
                if (string.IsNullOrEmpty(item)) continue;

                //注释
                if (item.StartsWith("#")) continue;

                var iIndex = item.IndexOf("=");
                //无=，第一个字符不能是=
                if (iIndex < 1) continue;

                var key = item.Substring(0, iIndex).Trim();
                var value = item.Substring(iIndex + 1).Trim();

                if (!nv.AllKeys.Contains(key))
                {
                    nv.Add(key, value);
                }
            }
        }
    }
}
