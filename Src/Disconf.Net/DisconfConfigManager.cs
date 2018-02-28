using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace Disconf.Net
{
    public class DisconfConfigManager
    {
        /// <summary>
        /// 获取注册的客户端
        /// </summary>
        /// <returns></returns>
        public static List<string> GetClients()
        {
            var xmlPath = AppDomain.CurrentDomain.SetupInformation.ConfigurationFile;
            XDocument doc = XDocument.Load(xmlPath);
            var configuration = doc.Elements().FirstOrDefault(s => s.Name.LocalName.EqualsIgnoreCase("configuration"));
            var items = configuration.Elements().Where(s => s.Name.LocalName.EqualsIgnoreCase("Clients"))
                                     .Elements().Where(s => s.Name.LocalName.EqualsIgnoreCase("Client")).ToList();
            List<string> paths = new List<string>();
            foreach (XElement x in items)
            {
                if (x.Attribute("Path") != null)
                {
                    paths.Add(x.Attribute("Path").Value);
                }
            }
            if (paths.Count == 0)
            {
                Logger.Info("读取到0个注册客户端");
            }
            else
            {
                Logger.Info($"读取到注册客户端：\r\n{string.Join("\r\n", paths)}");
            }
            return paths;
        }

        /// <summary>
        /// 获取客户端需要下载的配置文件
        /// </summary>
        /// <param name="clients"></param>
        /// <returns></returns>
        public static List<DisconfClientInfo> GetClientConfig(List<string> clients)
        {
            List<DisconfClientInfo> list = new List<DisconfClientInfo>();
            foreach (var client in clients)
            {
                try
                {
                    XDocument doc = XDocument.Load(client);
                    var configuration = doc.Elements().FirstOrDefault(s => s.Name.LocalName.EqualsIgnoreCase("configuration"));
                    var items = configuration.Elements().Where(s => s.Name.LocalName.EqualsIgnoreCase("Disconf"));
                    if (items.Count() == 0) continue;

                    var disconf = items.First();                   
                    if (disconf.Attribute("FilePath") == null)
                    {
                        throw new Exception("Disconf无FilePath属性");
                    }
                    DisconfClientInfo clientInfo = new DisconfClientInfo();
                    clientInfo.FilePath = disconf.Attribute("FilePath").Value;


                    items = disconf.Elements().Where(s => s.Name.LocalName.EqualsIgnoreCase("Item"));
                    if (items.Count() == 0) continue;

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

                    list.Add(clientInfo);
                }
                catch (Exception ex)
                {
                    Logger.Error($"读取{client}的配置错误", ex);
                }
            }

            return list;
        }

        /// <summary>
        /// disconf域名
        /// </summary>
        public static string DisconfDomain
        {
            get
            {
                var disconfDomain = ConfigurationManager.AppSettings["DisconfDomain"];
                return disconfDomain;
            }
        }

        /// <summary>
        /// disconf环境 如qa, dev, local, online等
        /// </summary>
        public static string DisconfEnvironment
        {
            get
            {
                var disconfDomain = ConfigurationManager.AppSettings["DisconfEnvironment"];
                return disconfDomain;
            }
        }
    }

    public class DisconfClientInfo
    {
        /// <summary>
        /// 配置文件存放路径
        /// </summary>
        public string FilePath { get; set; }
        
        /// <summary>
        /// 需要下载的App
        /// </summary>
        public List<DisconfClientItem> Items { get; set; }
    }

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
