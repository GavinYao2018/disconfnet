using System;

namespace Appsettings
{
    /// <summary>
    /// 
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    public class ConfigSettingAttributes : Attribute
    {
        /// <summary>
        /// 
        /// </summary>
        public ConfigSettingAttributes() { }

        private string _xmlPath = string.Empty;//xml路径名

        /// <summary>
        /// Xml路径
        /// </summary>
        public string XmlPath
        {
            get { return _xmlPath; }
            set { _xmlPath = value; }
        }

    }
}
