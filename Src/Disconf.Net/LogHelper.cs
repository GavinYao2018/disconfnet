using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using log4net;
using log4net.Appender;

namespace Disconf.Net
{
    /// <summary>
    /// 使用LOG4NET记录日志的功能，在WEB.CONFIG里要配置相应的节点
    /// </summary>
    public class Logger
    {
        //将日记对象缓存起来
        public static Dictionary<string, ILog> LogDic = new Dictionary<string, ILog>();
        static object _islock = new object();

        static Logger()
        {
            //使用代码初始化配置。 
            //log4net.Config.XmlConfigurator.Configure(new FileInfo("test.log4net"));
            // 使用 XmlConfigurator.ConfigureAndWatch() 方法除了初始化配置外，还会监测配置文件的变化，一旦发生修改，将自动刷新配置。 
            //我们还可以使用 XmlConfiguratorAttribute 代替 XmlConfigurator.Configure() / ConfigureAndWatch()，ConfiguratorAttribute 用于定义与 Assembly 相关联的配置文件名。 
            //方式1: 关联到 test.log4net，并监测变化。 
            //[assembly: log4net.Config.XmlConfigurator(ConfigFile = "test.log4net", Watch = true)]
            // 方式2: 关联到 test.exe.log4net(或 test.dll.log4net，文件名前缀为当前程序集名称)，并监测变化。 
            //[assembly: log4net.Config.XmlConfigurator(ConfigFileExtension = "log4net", Watch = true)]
            log4net.Config.XmlConfigurator.Configure();
        }
        
        /// <summary>
        /// Info日志
        /// </summary>
        /// <param name="info"></param>
        public static void Info(string info)
        {
            var loginfo = GetLog("loginfo");
            if (loginfo.IsInfoEnabled)
            {
                loginfo.Info(info);
            }
        }

        /// <summary>
        /// 错误日志
        /// </summary>
        /// <param name="info"></param>
        /// <param name="se"></param>
        public static void Error(string info, Exception ex)
        {
            var logerror = GetLog("logerror");
            if (logerror.IsErrorEnabled)
            {
                logerror.Error(info, ex);
            }
        }

        public static ILog GetLog(string name)
        {
            try
            {
                if (LogDic == null)
                {
                    LogDic = new Dictionary<string, ILog>();
                }
                lock (_islock)
                {
                    if (!LogDic.ContainsKey(name))
                    {
                        LogDic.Add(name, LogManager.GetLogger(name));
                    }
                }
                return LogDic[name];
            }
            catch
            {
                return LogManager.GetLogger("InfoAppender");
            }
        }
    }
}
