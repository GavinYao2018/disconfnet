using System;
using System.Collections.Generic;
using System.Linq;
using System.Configuration;
using System.Xml.Linq;
using System.IO;
using System.Diagnostics;

namespace Appsettings
{
    /// <summary>
    /// 基类
    /// </summary>
    internal class AppSettingsBase
    {
        /// <summary>
        /// 自定义配置文件物理路径
        /// </summary>
        public string[] XmlPaths
        {
            get
            {
                string[] files = null;
                var clientInfo = GetDisconfClientInfo();
                if (clientInfo != null)
                {
                    var list = new List<string>();
                    foreach (var item in clientInfo.Items)
                    {
                        var itemFiles = item.Files.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                        foreach (var file in itemFiles)
                        {
                            string path = Path.Combine(clientInfo.FilePath, file);
                            list.Add(path);
                        }
                    }
                    files = list.ToArray();
                }
                else
                {
                    string dirKey = GetOldConfigPath();
                    if (!Directory.Exists(dirKey))
                    {
                        throw new ConfigurationErrorsException("自定义配置文件配置目录未找到");
                    }
                    files = Directory.GetFiles(dirKey, "*.xml");
                }

                if (files.Length == 0)
                {
                    throw new ConfigurationErrorsException("自定义配置文件不存在");
                }
                return files;
            }
        }

        /// <summary>
        /// 转换数据类型
        /// </summary>
        /// <param name="value"></param>
        /// <param name="conversionType"></param>
        /// <returns></returns>
        public static object ChangeValueType(object value, Type conversionType)
        {
            return ChangeValueType(value, conversionType, null);
        }

        /// <summary>
        /// 转换数据类型
        /// </summary>
        /// <param name="value"></param>
        /// <param name="conversionType"></param>
        /// <param name="provider"></param>
        /// <returns></returns>
        public static object ChangeValueType(object value, Type conversionType, IFormatProvider provider)
        {
            if (conversionType == null)
            {
                throw new ArgumentNullException("conversionType");
            }

            bool valueCanbeNull = valueNaullable(conversionType);
            if (valueCanbeNull && (value == null || value.ToString().Length == 0))//如果Nullable<>类型，且值是空，则直接返回空
            {
                return null;
            }
            if (value == null)
            {
                if (conversionType.IsValueType)
                {
                    throw new InvalidCastException(string.Format("值为空。"));
                }
                return null;
            }
            IConvertible convertible = value as IConvertible;
            if (convertible == null)
            {
                if (value.GetType() != conversionType)
                {
                    throw new InvalidCastException(string.Format("值不能被转换。"));
                }
                return value;
            }
            if (conversionType == typeof(System.Boolean) || conversionType == typeof(Nullable<System.Boolean>))
            {
                if (value.ToString() == "1")
                    return true;
                if (value.ToString() == "0")
                    return false;
                return convertible.ToBoolean(provider);
            }
            if (conversionType == typeof(System.Char) || conversionType == typeof(Nullable<System.Char>))
            {
                return convertible.ToChar(provider);
            }
            if (conversionType == typeof(System.SByte) || conversionType == typeof(Nullable<System.SByte>))
            {
                return convertible.ToSByte(provider);
            }
            if (conversionType == typeof(System.Byte) || conversionType == typeof(Nullable<System.Byte>))
            {
                return convertible.ToByte(provider);
            }
            if (conversionType == typeof(System.Int16) || conversionType == typeof(Nullable<System.Int16>))
            {
                return convertible.ToInt16(provider);
            }
            if (conversionType == typeof(System.UInt16) || conversionType == typeof(Nullable<System.UInt16>))
            {
                return convertible.ToUInt16(provider);
            }
            if (conversionType == typeof(System.Int32) || conversionType == typeof(Nullable<System.Int32>))
            {
                return convertible.ToInt32(provider);
            }
            if (conversionType == typeof(System.UInt32) || conversionType == typeof(Nullable<System.UInt32>))
            {
                return convertible.ToUInt32(provider);
            }
            if (conversionType == typeof(System.Int64) || conversionType == typeof(Nullable<System.Int64>))
            {
                return convertible.ToInt64(provider);
            }
            if (conversionType == typeof(System.UInt64) || conversionType == typeof(Nullable<System.UInt64>))
            {
                return convertible.ToUInt64(provider);
            }
            if (conversionType == typeof(System.Single) || conversionType == typeof(Nullable<System.Single>))
            {
                return convertible.ToSingle(provider);
            }
            if (conversionType == typeof(System.Double) || conversionType == typeof(Nullable<System.Double>))
            {
                return convertible.ToDouble(provider);
            }
            if (conversionType == typeof(System.Decimal) || conversionType == typeof(Nullable<System.Decimal>))
            {
                return convertible.ToDecimal(provider);
            }
            if (conversionType == typeof(System.DateTime) || conversionType == typeof(Nullable<System.DateTime>))
            {
                return convertible.ToDateTime(provider);
            }
            if (conversionType == typeof(System.String))
            {
                return convertible.ToString(provider);
            }
            if (conversionType == typeof(System.Object))
            {
                return value;
            }
            return value;
        }

        /// <summary>
        /// 判断该类型是否是可为空值的数据类型
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        private static bool valueNaullable(Type type)
        {
            if (type == typeof(Nullable<System.Boolean>))
                return true;
            if (type == typeof(Nullable<System.Char>))
                return true;
            if (type == typeof(Nullable<System.SByte>))
                return true;
            if (type == typeof(Nullable<System.Byte>))
                return true;
            if (type == typeof(Nullable<System.Int16>))
                return true;
            if (type == typeof(Nullable<System.UInt16>))
                return true;
            if (type == typeof(Nullable<System.Int32>))
                return true;
            if (type == typeof(Nullable<System.UInt32>))
                return true;
            if (type == typeof(Nullable<System.Int64>))
                return true;
            if (type == typeof(Nullable<System.UInt64>))
                return true;
            if (type == typeof(Nullable<System.Single>))
                return true;
            if (type == typeof(Nullable<System.Double>))
                return true;
            if (type == typeof(Nullable<System.Decimal>))
                return true;
            if (type == typeof(Nullable<System.DateTime>))
                return true;
            return false;
        }

        /// <summary>
        /// 获取注册的客户端信息
        /// </summary>
        /// <returns></returns>
        private static DisconfClientInfo GetDisconfClientInfo()
        {
            var xmlPath = AppDomain.CurrentDomain.SetupInformation.ConfigurationFile;
            XDocument doc = XDocument.Load(xmlPath);
            var configuration = doc.Elements().FirstOrDefault(s => s.Name.LocalName.EqualsIgnoreCase("configuration"));
            var items = configuration.Elements().Where(s => s.Name.LocalName.EqualsIgnoreCase("Disconf"));
            if (items.Count() == 0) return null;

            var disconf = items.First();
            //if (disconf.Attribute("Environment") == null)
            //{
            //    throw new Exception("Disconf无Environment属性");
            //}
            if (disconf.Attribute("FilePath") == null)
            {
                throw new Exception("Disconf无FilePath属性");
            }
            DisconfClientInfo clientInfo = new DisconfClientInfo();
            //clientInfo.Environment = disconf.Attribute("Environment").Value;
            clientInfo.FilePath = disconf.Attribute("FilePath").Value;


            items = disconf.Elements().Where(s => s.Name.LocalName.EqualsIgnoreCase("Item"));
            if (items.Count() == 0) return null;

            clientInfo.Items = new List<DisconfClientItem>();
            foreach (XElement item in items)
            {
                DisconfClientItem clientItem = new DisconfClientItem();
                if (item.Attribute("AppName") == null)
                {
                    throw new Exception("某项Item无AppName属性");
                }
                if (item.Attribute("Version") == null)
                {
                    throw new Exception("某项Item无Version属性");
                }
                if (item.Attribute("Files") == null)
                {
                    throw new Exception("某项Item无Files属性");
                }
                clientItem.AppName = item.Attribute("AppName").Value;
                clientItem.Version = item.Attribute("Version").Value;
                clientItem.Files = item.Attribute("Files").Value;
                clientInfo.Items.Add(clientItem);
            }

            return clientInfo;
        }

        private static string GetOldConfigPath()
        {
            string dirKey = ConfigurationManager.AppSettings["AppSettingsPath"];
            if (string.IsNullOrEmpty(dirKey))
            {
                throw new ConfigurationErrorsException("自定义配置文件配置AppSettingsPath未找到");
            }

            #region 兼容以前直接配置文件的模式

            if (dirKey.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
            {
                dirKey = dirKey.Substring(0, dirKey.LastIndexOf("\\")); Path.GetPathRoot(dirKey);
            }

            #endregion

            return dirKey;
        }

        /// <summary>
        /// 返回配置文件所在路径
        /// </summary>
        public static string ConfigPath
        {
            get
            {
                var info = GetDisconfClientInfo();
                if (info != null)
                {
                    return info.FilePath;
                }
                else
                {
                    string dirKey = GetOldConfigPath();
                    return dirKey;
                }
            }
        }

        /// <summary>
        /// 记录异常日志到系统日志中
        /// </summary>
        /// <param name="ex"></param>
        public static void Log(Exception ex)
        {
            try
            {
                if (ex == null) return;

                EventLog RecordLog = new EventLog();
                RecordLog.Source = "EFinance.Common.Appsettings";
                RecordLog.WriteEntry(ex.ToString(), EventLogEntryType.Error);
            }
            finally
            { }
        }
    }

    /// <summary>
    /// 配置信息
    /// </summary>
    public class DisconfClientInfo
    {
        /// <summary>
        /// 配置文件存放路径
        /// </summary>
        public string FilePath { get; set; }

        /// <summary>
        /// 环境，如qa, dev, local, online等，必须是disconf管理端存在的
        /// </summary>
        public string Environment { get; set; }

        /// <summary>
        /// 需要下载的App
        /// </summary>
        public List<DisconfClientItem> Items { get; set; }
    }

    /// <summary>
    /// 配置项
    /// </summary>
    public class DisconfClientItem
    {
        /// <summary>
        /// 应用名称
        /// </summary>
        public string AppName { get; set; }

        /// <summary>
        /// 版本
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        /// 文件名，多个用,隔开
        /// </summary>
        public string Files { get; set; }
    }
}
