using System;
using System.Collections.Generic;
using System.Text;
using ZooKeeperNet;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Web.Script.Serialization;

namespace Disconf.Net
{
    public class ZooKeeperHelper
    {
        static object _lockObj = new object();
        /// <summary>
        /// 指纹
        /// </summary>
        static string _sysGUID = Guid.NewGuid().ToString();

        //~ZooKeeperHelper()
        //{
        //    Dispose();
        //}

        private ZooKeeperHelper()
        {
        }

        static ZooKeeperHelper _instance;
        /// <summary>
        /// ZookeeperHelper实例
        /// </summary>
        public static ZooKeeperHelper Instance
        {
            get
            {
                lock (_lockObj)
                {
                    if (_instance == null)
                    {
                        lock (_lockObj)
                        {
                            _instance = new ZooKeeperHelper();
                        }
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        ///  状态检查，若不是连接状态，自动再次初始化
        /// </summary>
        public void ZooKeeperCheck()
        {
            try
            {
                Logger.Info("ZooKeeper状态检查：" + ZK.State.ToString());
            }
            catch (Exception ex)
            {
                Logger.Error("ZooKeeper状态检查异常", ex);
            }
        }

        /// <summary>
        /// Path改变时的通知 path, data
        /// </summary>
        public Action<string, string> OnChange;


        /// <summary>
        /// 需要监听的path
        /// </summary>
        List<string> watchPathList = new List<string>();

        /// <summary>
        /// 获取ZooKeeper服务器列表
        /// </summary>
        /// <returns></returns>
        string GetZooKeeperHost()
        {
            //{"status":1,"message":"","value":"10.1.21.4:2181"}
            string fileUrl = $"{DisconfConfigManager.DisconfDomain}/api/zoo/hosts?t={DateTime.Now.ToString("yyyyMMddHHmmssffffff")}";
            WebClient webClient = new WebClient();
            try
            {
                var str = webClient.DownloadString(fileUrl);
                Logger.Info("获取ZooKeeper hosts：" + str);
                var obj = new JavaScriptSerializer().Deserialize<DisconfZooKeeperHost>(str);
                if (obj.status == 1)
                {
                    return obj.value;
                }
            }
            catch (Exception ex)
            {
                Logger.Error("disconf: 获取zookeeper hosts异常", ex);
            }
            return "";
        }

        /// <summary>
        /// 获取Zookepper实例
        /// </summary>
        /// <returns></returns>
        ZooKeeper GetZooKeeper()
        {
            try
            {
                //为空，则实例化
                if (_zk == null)
                {
                    //Zookeeper服务器  ip:port
                    string server = GetZooKeeperHost();
                    _zk = new ZooKeeper(server, new TimeSpan(0, 0, 0, 50000), new ZooKeeperWatcher());
                    Logger.Info("ZooKeeper实例化...");
                    System.Threading.Thread.Sleep(300);
                }
                //如果是连接中，则100毫秒后再次检测
                if (_zk.State == ZooKeeper.States.CONNECTING)
                {
                    Logger.Info("ZooKeeper等待连接中...");
                    int i = 1;
                    while (true)
                    {
                        System.Threading.Thread.Sleep(500);
                        if (_zk.State == ZooKeeper.States.CONNECTING)
                        {
                            i++;
                            if (i > 10)
                            {
                                Logger.Info("ZooKeeper 5秒未能连接成功");
                                break;
                            }
                        }
                        else
                        {
                            //如果不是连接中的状态
                            Logger.Info("ZooKeeper状态为：" + _zk.State.ToString());
                            break;
                        }
                    }
                }               
            }
            catch (Exception ex)
            {
                Logger.Error("ZooKeeper初始化异常", ex);
            }
            return _zk;
        }


        ZooKeeper _zk;
        /// <summary>
        /// ZooKepper实例
        /// </summary>
        ZooKeeper ZK
        {
            get
            {
                GetZooKeeper();

                //如果不是连接状态，重新实例化
                if (_zk.State != ZooKeeper.States.CONNECTED)
                {
                    Logger.Info("ZooKeeper重新实例化...");
                    _zk = null;
                    GetZooKeeper();

                    //重新实例化后，重新注册监听
                    if (_zk.State == ZooKeeper.States.CONNECTED)
                    {
                        foreach (var path in watchPathList)
                        {
                            _zk.Exists(path, true);
                        }
                        Logger.Info("ZooKeeper重新实例化后，重新注册监听...");
                    }
                }

                return _zk;
            }
        }

        /// <summary>
        /// 监听配置
        /// </summary>
        /// <param name="appName"></param>
        /// <param name="env"></param>
        /// <param name="version"></param>
        /// <param name="configName"></param>
        public void Watch(string appName, string env, string version, string configName, string data = "")
        {
            string path = "/disconf";

            var ip = GetLocalIP();
            path = path + $"/{appName}_{version}_{env}";
            CreatePersistentIfNot(ZK, path);

            path = path + "/file";
            CreatePersistentIfNot(ZK, path);

            path = path + "/" + configName;
            CreateDataIfNot(ZK, path, data);
            //监听列表
            watchPathList.Add(path);
            //监听
            ZK.Exists(path, true);

            //末端节点，disconf站点会列出连接的客户端
            string computName = Dns.GetHostName();
            var pathData = path + $"/{computName}_0_{_sysGUID}";
            //此处的data必须为json字符串
            var ephemeralData = "{}";//data换成json
            CreateEphemeralIfNot(ZK, pathData, ephemeralData);
        }

        internal string GetData(string path, bool watch = true)
        {
            var bytes = ZK.GetData(path, watch, null);
            var data = Encoding.UTF8.GetString(bytes);

            //初次上传，\r被当成文本保存
            //编辑后，\n被当成文本保存
            // " 被替换成\"保存
            //同时前后被"包围
            data = DisconfMgr.Uncode(data);

            //\t 制表符
            //\r 回车符
            //\n 换行符
            //\f 换页符
            //disconf中有此4种转义
            data = data.Replace("\\n", "\n").Replace("\\r", "\r").Replace("\\t", "\t").Replace("\\f", "\f");

            data = data.Replace("\\\"", "\"");
            data = data.Trim('\"');

            //BOM格式的UTF8的文件
            data = UTF8BOMProcess(data);

            return data;
        }

        /// <summary>
        /// BOM格式的UTF8的文件，需要去掉编码格式
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        string UTF8BOMProcess(string data)
        {
            if (data.Length > 1)
            {
                var firstBytes = data.Substring(0, 1).GetBytes();
                if (firstBytes.Length >= 3)
                {
                    //文件头两个字节是255 254，为Unicode编码；
                    //文件头三个字节  254 255 0，为UTF-16BE编码；
                    //文件头三个字节  239 187 191，为UTF-8编码；
                    //因disconf上传的文件是UTF-8格式，故这三个bytes需要删除掉
                    if (firstBytes[0] == 239 && firstBytes[1] == 187 && firstBytes[2] == 191)
                    {
                        data = data.Substring(1);
                    }
                }
            }

            return data;
        }

        /// <summary>
        /// 接收ZooKeeper的通知
        /// </summary>
        /// <param name="path"></param>
        internal void OnNodeChange(string path)
        {
            try
            {
                if (OnChange == null)
                {
                    Rewatch(path);//需要重新监听
                    return;
                }
                var data = GetData(path);//获取数据，顺便重新监听了
                Logger.Info(path + "数据改变了\r\n" + data);
                OnChange(path, data);
            }
            catch (KeeperException ex)
            {
                GetZooKeeper();//重新初始化
                Logger.Error("KeeperException", ex);
            }
            catch (Exception ex)
            {
                Logger.Error("", ex);
            }
        }

        void Rewatch(string path)
        {
            var st = ZK.Exists(path, true);
        }

        /// <summary>
        /// 创建永久节点
        /// </summary>
        /// <param name="zk"></param>
        /// <param name="path"></param>
        /// <param name="data"></param>
        void CreatePersistentIfNot(ZooKeeper zk, string path, string data = "")
        {
            var stat = zk.Exists(path, false);
            if (stat == null)
            {
                zk.Create(path, data.GetBytes(), Ids.OPEN_ACL_UNSAFE, CreateMode.Persistent);
            }
            else
            {
                zk.SetData(path, data.GetBytes(), stat.Version);
            }
        }

        /// <summary>
        /// 创建数据节点（监听节点数据）
        /// </summary>
        /// <param name="zk"></param>
        /// <param name="path"></param>
        /// <param name="data"></param>
        void CreateDataIfNot(ZooKeeper zk, string path, string data = "")
        {
            var stat = zk.Exists(path, false);
            if (stat == null)
            {
                zk.Create(path, data.GetBytes(), Ids.OPEN_ACL_UNSAFE, CreateMode.Persistent);
            }
            else
            {
                var zkData = GetData(path, false);
                if (string.IsNullOrEmpty(zkData))
                {
                    zk.SetData(path, data.GetBytes(), stat.Version);
                }
            }
        }

        /// <summary>
        /// 创建临时节点
        /// </summary>
        /// <param name="zk"></param>
        /// <param name="path"></param>
        /// <param name="data"></param>
        void CreateEphemeralIfNot(ZooKeeper zk, string path, string data)
        {
            var stat = zk.Exists(path, false);
            if (stat == null)
            {
                zk.Create(path, data.GetBytes(), Ids.OPEN_ACL_UNSAFE, CreateMode.Ephemeral);
            }
        }

        /// <summary>
        /// 本机IP
        /// </summary>
        /// <returns></returns>
        string GetLocalIP()
        {
            string strLocalIP = "";
            try
            {
                //获取说有网卡信息
                NetworkInterface[] nics = NetworkInterface.GetAllNetworkInterfaces();
                foreach (NetworkInterface adapter in nics)
                {
                    //判断是否为以太网卡
                    //Wireless80211         无线网卡    Ppp     宽带连接
                    //Ethernet              以太网卡   
                    //这里篇幅有限贴几个常用的，其他的返回值大家就自己百度吧！

                    if (adapter.NetworkInterfaceType == NetworkInterfaceType.Ethernet)
                    {
                        //获取以太网卡网络接口信息
                        IPInterfaceProperties ip = adapter.GetIPProperties();
                        //获取单播地址集
                        UnicastIPAddressInformationCollection ipCollection = ip.UnicastAddresses;
                        foreach (UnicastIPAddressInformation ipadd in ipCollection)
                        {
                            //InterNetwork    IPV4地址      InterNetworkV6        IPV6地址
                            //Max            MAX 位址
                            if (ipadd.Address.AddressFamily == AddressFamily.InterNetwork)
                            //判断是否为ipv4
                            {
                                strLocalIP = ipadd.Address.ToString();//获取ip
                                return strLocalIP;//获取ip
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("disconf获取本机IP异常", ex);
            }
            return strLocalIP;
        }

        /// <summary>
        /// 注销
        /// </summary>
        public void Dispose()
        {
            try
            {
                if (_zk != null) _zk.Dispose();

                Logger.Info("ZooKeeper注销...");
            }
            finally
            {
                _zk = null;
            }
        }
    }

    /// <summary>
    /// 监测者
    /// </summary>
    internal class ZooKeeperWatcher : IWatcher
    {
        public void Process(WatchedEvent @event)
        {
            //创建或改变后都需要通知
            if (@event.Type == EventType.NodeCreated || @event.Type == EventType.NodeDataChanged)
            {
                ZooKeeperHelper.Instance.OnNodeChange(@event.Path);
            }
        }
    }

    /// <summary>
    /// ZooKeeper服务器
    /// </summary>
    class DisconfZooKeeperHost
    {
        public int status { get; set; }
        public string message { get; set; }
        public string value { get; set; }
    }
}
