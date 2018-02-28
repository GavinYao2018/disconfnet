using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Caching;
using System.Xml.Linq;

namespace Appsettings
{
    internal class AppSettingXml : AppSettingsBase
    {
        /// <summary>
        /// 缓存Key
        /// </summary>
        private string Key
        {
            get { return "APPSETTINGSLIST_Xml"; }
        }

        /// <summary>
        /// 通过节点名称和Attributes属性名称获取Attributes值
        /// </summary>
        /// <param name="name">节点名称</param>
        /// <param name="attributes">Attributes属性名称</param>
        /// <returns></returns>
        public string GetAttributesValue(string name, string attributes)
        {
            var appSettings = HttpRuntime.Cache.Get(Key);
            if (appSettings == null)
            {
                appSettings = LoadToCache();
            }
            if (appSettings == null)
            {
                return string.Empty;
            }

            //获取节点信息
            var xmls = appSettings as List<XElement>;
            var xml = xmls.FirstOrDefault(s => s.Name.LocalName.EqualsIgnoreCase(name));
            if (xml == null)
            {
                return string.Empty;
            }

            //获取属性信息
            var att = xml.Attributes().FirstOrDefault(s => s.Name.LocalName.EqualsIgnoreCase(attributes));
            if (att != null)
            {
                return att.Value;
            }

            return string.Empty;
        }

        /// <summary>
        /// 加载自定义配置到缓存
        /// </summary>
        /// <returns></returns>
        public List<XElement> LoadToCache()
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
        /// 加载自定义配置
        /// </summary>
        /// <returns></returns>
        public List<XElement> GetAppSettings()
        {
            var list = new List<XElement>();
            foreach (var xmlPath in XmlPaths)
            {
                if (!xmlPath.EndsWithIgnoreCase(".xml")) continue;

                XDocument doc = AppSettingsUtils.LoadXml(xmlPath); //XDocument.Load(xmlPath);
                var appSettings = doc.Elements().FirstOrDefault(s => s.Name.LocalName.EqualsIgnoreCase("AppSettings"));
                if (appSettings == null) continue;

                list.AddRange(appSettings.Elements().ToList());
            }
            return list;
        }
    }
}
