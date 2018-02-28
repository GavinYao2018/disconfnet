using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text.RegularExpressions;

namespace Appsettings
{
    /// <summary>
    /// 提供对自定义应用程序配置文件的访问（支持深层节点）
    /// <para>1、必须在配置文件.config中的节点configSections添加&lt;section name="Disconf" type="System.Configuration.DictionarySectionHandler"/&gt;，再添加自定义节点Disconf，如
    /// <para> &lt;Disconf Environment="dev" FilePath="D:\Projects\钱途公共类库\SourceCode\Disconf.Net\Config"&gt;</para>
    /// <para>    &lt;Item AppName = "YaoTest" Version="1_0_0_1" Files="FrontConfig.xml,aa.xml"&gt;&lt;/Item&gt;</para>
    /// <para>    &lt;Item AppName = "oas-management" Version="1_0_0_0" Files="config.properties"&gt;&lt;/Item&gt;</para>
    /// <para> &lt;/Disconf&gt;</para>
    /// </para>
    /// <para>2、自定义配置文件必须是以AppSettings节点开始。</para>
    /// <para>3、同时确保自定义配置文件设置了对应的读取权限。</para>
    /// 
    /// 使用方法
    /// <para>1、add键值类型的节点，直接通过Key获取值：AppSettingsManager.AppSettings["name"]</para>
    /// <para>2、其他类型节点，可通过节点名称和属性名称获取属性值：AppSettingsManager.GetAttributesValue("Person", "Name")</para>
    /// <para>3、和实体一致的节点，可直接传入实体类型获取对应的节点信息：AppSettingsManager.GetEntity&lt;Person&gt;()</para>
    /// </summary>
    public static class AppSettingsManager
    {
        private static AppSettingsValue _AppValue = new AppSettingsValue();

        private static AppSettingXml _AppXml = new AppSettingXml();

        /// <summary>
        /// 获取自定义配置的数据。
        /// </summary>
        public static NameValueCollection AppSettings
        {
            get
            {
                return _AppValue.AppSettings;
            }
        }

        /// <summary>
        /// 获取自定义配置属性值
        /// </summary>
        /// <param name="name">节点名称</param>
        /// <param name="attributes">属性名称</param>
        /// <returns></returns>
        public static string GetAttributesValue(string name, string attributes)
        {
            return _AppXml.GetAttributesValue(name, attributes);
        }

        /// <summary>
        /// 根据实体获取对应的自定义配置，实体名和属性要和节点一致
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T GetEntity<T>(string xmlSubPath = null)
        {
            return (new AppSettingsClass<T>()).GetEntity(xmlSubPath);
        }

        /// <summary>
        /// 根据实体获取对应的自定义配置，实体名和属性要和节点一致
        /// </summary>
        /// <typeparam name="T">条件</typeparam>
        /// <returns></returns>
        public static T GetEntity<T>(Func<T, bool> predicate, string xmlSubPath = null)
        {
            return (new AppSettingsClass<T>()).GetEntity(predicate, xmlSubPath);
        }

        /// <summary>
        /// 根据实体获取对应的自定义配置，实体名和属性要和节点一致
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="xmlSubPath">xml路径</param>
        /// <returns></returns>
        public static List<T> GetEntityList<T>(string xmlSubPath = null)
        {
            return (new AppSettingsClass<T>()).GetEntityList(xmlSubPath);
        }

        /// <summary>
        /// 根据实体获取对应的自定义配置，实体名和属性要和节点一致
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="predicate">条件</param>
        /// <param name="xmlSubPath">xml路径</param>
        /// <returns></returns>
        public static List<T> GetEntityList<T>(Func<T, bool> predicate, string xmlSubPath = null)
        {
            return (new AppSettingsClass<T>()).GetEntityList(predicate, xmlSubPath);
        }

        /// <summary>
        /// 设置自定义配置到缓存中
        /// </summary>
        /// <returns></returns>
        public static void InitAppSettingsCache()
        {
            _AppValue.LoadToCache();
            _AppXml.LoadToCache();
        }

        /// <summary>
        /// 设置自定义配置到缓存中
        /// </summary>
        /// <returns></returns>
        public static void InitAppSettingsCache<T>()
        {
            (new AppSettingsClass<T>()).LoadToCache();
        }

        /// <summary>
        /// 获取程序执行的目录（包括站点、应用程序）
        /// </summary>
        /// <returns></returns>
        public static string GetExePath()
        {
            var arr = AppDomain.CurrentDomain.BaseDirectory.Split(new char[] { '\\' }, StringSplitOptions.RemoveEmptyEntries);
            return arr[arr.Length - 1];
        }

        /// <summary>
        /// 在数据库连接字符串中插入app=xxx;（xxx=当前程序运行的目录，“.”、“ ”被替换成“_”）。若已存在app配置，则忽略
        /// </summary>
        /// <param name="connString"></param>
        /// <returns></returns>
        public static string BuildDBConnString(string connString)
        {
            if (string.IsNullOrEmpty(connString))
            {
                throw new ArgumentNullException(connString);
            }

            //如果已经包含了App=xxx;
            string pattern = @"app\s*=\s*";
            var isMatch = Regex.IsMatch(connString, pattern, RegexOptions.IgnoreCase);
            if (isMatch)
            {
                return connString;
            }

            //构造App=xxx;
            var appName = GetExePath().Replace(".", "_").Replace(" ", "_");
            string app = "App=" + appName + ";";

            //连接字符串数据库使用Initial Catalog=xxx;
            pattern = @"Initial Catalog\s*=\s*(.*?);";
            var match = Regex.Match(connString, pattern, RegexOptions.IgnoreCase);
            if (match.Length > 0)
            {
                var database = match.Value;
                connString = connString.Replace(database, database + app);
                return connString;
            }

            //连接字符串数据库使用Server=xxx;
            pattern = @"Server\s*=\s*(.*?);";
            match = Regex.Match(connString, pattern, RegexOptions.IgnoreCase);
            if (match.Length > 0)
            {
                var database = match.Value;
                connString = connString.Replace(database, database + app);
                return connString;
            }

            //否则，原样返回
            return connString;
        }

        /// <summary>
        /// 返回配置文件所在路径
        /// </summary>
        public static string ConfigPath
        {
            get
            {
                return AppSettingsBase.ConfigPath;
            }
        }
    }
}
