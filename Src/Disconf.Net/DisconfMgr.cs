using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace Disconf.Net
{
    public class DisconfMgr
    {
        /// <summary>
        /// 是否初始化过了
        /// </summary>
        static bool _inited = false;

        /// <summary>
        /// 初始化
        /// </summary>
        public static void Init()
        {
            if (_inited)
            {
                //如果已经初始化过，则进行状态检查
                ZooKeeperHelper.Instance.ZooKeeperCheck();
                return;
            }

            //第一次的状态检查
            ZooKeeperHelper.Instance.ZooKeeperCheck();

            //注册disconf改变时的通知事件
            ZooKeeperHelper.Instance.OnChange = OnChange;

            var clientInfoList = GetClientConfig();
            DisconfFileCheck(clientInfoList);

            _inited = true;
        }

        /// <summary>
        /// 获取注册的客户端列表
        /// </summary>
        /// <returns></returns>
        static List<DisconfClientInfo> GetClientConfig()
        {
            var list = DisconfConfigManager.GetClients();
            var list2 = DisconfConfigManager.GetClientConfig(list);
            return list2;
        }

        /// <summary>
        /// 远程下载配置文件
        /// </summary>
        /// <param name="clientInfoList"></param>
        static void DisconfFileCheck(List<DisconfClientInfo> clientInfoList)
        {
            /// <summary>
            /// 临时记录已经下载的配置文件 key=应用+环境+版本+文件名  value=存放路径
            /// </summary>
            Dictionary<string, string> dicConfigKey = new Dictionary<string, string>();
            foreach (var clientInfo in clientInfoList)
            {
                DownloadConfig(clientInfo, dicConfigKey);
            }
        }

        /// <summary>
        /// 下载配置文件，ZooKeeper监听
        /// </summary>
        /// <param name="clientInfo"></param>
        private static void DownloadConfig(DisconfClientInfo clientInfo, Dictionary<string, string> dicConfigKey)
        {
            if (!Directory.Exists(clientInfo.FilePath))
            {
                Directory.CreateDirectory(clientInfo.FilePath);
            }
            var tmp = GetTempPath(clientInfo.FilePath);

            foreach (var item in clientInfo.Items)
            {
                var files = item.Files.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

                foreach (var file in files)
                {
                    //如果是同应用，同环境，同版本，同一配置文件，且存放目录也一致，则不重复下载
                    var configKey = $"{item.AppName}_{DisconfConfigManager.DisconfEnvironment}_{item.Version}_{file}";
                    if (dicConfigKey.ContainsKey(configKey) && dicConfigKey[configKey].EqualsIgnoreCase(clientInfo.FilePath))
                    {
                        Logger.Info($"目标文件{clientInfo.FilePath}\\{file}已下载，忽略");
                        continue;
                    }

                    try
                    {
                        var data = DownloadData(clientInfo, item, tmp, file);
                        CreateFile(tmp, file, clientInfo.FilePath, data);
                        ZooKeeperHelper.Instance.Watch(item.AppName, DisconfConfigManager.DisconfEnvironment, item.Version, file, data);
                        dicConfigKey[configKey] = clientInfo.FilePath;
                    }
                    catch (ZooKeeperNet.KeeperException ex)
                    {
                        Init();//重新初始化
                        Logger.Error("ZooKeeperException", ex);
                    }
                    catch (Exception ex)
                    {
                        Logger.Error("", ex);
                    }
                }
            }
        }

        /// <summary>
        /// ZooKeeper通知配置变更
        /// </summary>
        /// <param name="path"></param>
        /// <param name="data"></param>
        static void OnChange(string path, string data)
        {
            // path = "/disconf/YaoTest_1_0_0_1_dev/file/aa.xml"
            if (string.IsNullOrEmpty(path)) return;
            var arr = path.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            if (arr.Length != 4) return;
            try
            {
                //根据path找到对应的配置信息
                var appVersion = arr[1];
                var version = Regex.Match(appVersion, @"\d+_\d+_\d+_\d+").Value;
                var vIndex = appVersion.IndexOf(version);
                var app = appVersion.Substring(0, vIndex - 1);
                var env = appVersion.Substring(vIndex + version.Length + 1);
                var file = arr[3];

                //记录本次已更新的目标文件
                Dictionary<string, string> dicConfigKey = new Dictionary<string, string>();

                var clientInfoList = GetClientConfig();
                foreach (var clientInfo in clientInfoList)
                {
                    if (DisconfConfigManager.DisconfEnvironment.EqualsIgnoreCase(env))
                    {
                        var item = clientInfo.Items.Where(n => n.AppName.EqualsIgnoreCase(app) && n.Version.EqualsIgnoreCase(version) && n.Files.Contains(file)).FirstOrDefault();
                        if (item == null) continue;


                        //如果是同应用，同环境，同版本，同一配置文件，且存放目录也一致，则不重复下载
                        var configKey = $"{item.AppName}_{DisconfConfigManager.DisconfEnvironment}_{item.Version}_{file}";
                        if (dicConfigKey.ContainsKey(configKey) && dicConfigKey[configKey].EqualsIgnoreCase(clientInfo.FilePath))
                        {
                            //Logger.Info($"目标文件{clientInfo.FilePath}\\{file}已更新，忽略");
                            continue;
                        }

                        try
                        {
                            var tmp = GetTempPath(clientInfo.FilePath);
                            //重新下载，因为disconf管理端将数据写入zookeeper中时，会将\等转义而丢失
                            data = DownloadData(clientInfo, item, tmp, file);

                            try
                            {
                                CreateFile(tmp, file, clientInfo.FilePath, data);
                            }
                            catch
                            {
                                System.Threading.Thread.Sleep(300);
                                Logger.Info($"第一次创建文件失败，尝试第二次：{clientInfo.FilePath}/{file}");
                                CreateFile(tmp, file, clientInfo.FilePath, data);
                            }

                            dicConfigKey[configKey] = clientInfo.FilePath;
                        }
                        catch (Exception ex)
                        {
                            Logger.Error("下载配置文件" + configKey + "失败", ex);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("", ex);
            }
        }

        /// <summary>
        /// 构造临时文件夹路径
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        static string GetTempPath(string filePath)
        {
            var tmp = Path.Combine(filePath, "tmp");
            if (!Directory.Exists(tmp))
            {
                Directory.CreateDirectory(tmp);
            }
            return tmp;
        }

        /// <summary>
        /// 远程下载
        /// </summary>
        /// <param name="clientInfo"></param>
        /// <param name="item"></param>
        /// <param name="tmpPath"></param>
        /// <param name="file"></param>
        /// <returns></returns>
        public static string DownloadData(DisconfClientInfo clientInfo, DisconfClientItem item, string tmpPath, string file)
        {
            //http://disconf.frontpay.cn/api/config/file?version=1_0_0_1&app=YaoTest&env=dev&key=FrontConfig.xml
            string fileUrl = $"{DisconfConfigManager.DisconfDomain}/api/config/file?version={item.Version}&app={item.AppName}&env={DisconfConfigManager.DisconfEnvironment}&key={file}";
            WebClient webClient = new WebClient();
            var str = webClient.DownloadString(fileUrl);
            str = str.Replace("\\ufeff", "");//BOM格式，需要去掉头部的四个\ufeff
            str = Uncode(str); //转换中文
            return str;
        }

        /// <summary>
        /// 将配置写入文件
        /// </summary>
        /// <param name="tmpPath"></param>
        /// <param name="file"></param>
        /// <param name="filePath"></param>
        /// <param name="data"></param>
        static void CreateFile(string tmpPath, string file, string filePath, string data)
        {
            var sourceFile = tmpPath + "\\" + file;
            WriteFile(sourceFile, data);

            var targetFile = filePath + "\\" + file;
            //如果目标下没有该文件，则拷贝过去
            if (!File.Exists(targetFile))
            {
                File.Copy(sourceFile, targetFile);
                Logger.Info($"目标文件{targetFile}为空，拷贝过去");
                return;
            }
            var sourceFileMd5 = MD5Checker.GetMD5ByHashAlgorithm(sourceFile);
            var targetFileMd5 = MD5Checker.GetMD5ByHashAlgorithm(targetFile);
            if (sourceFileMd5.Equals(targetFileMd5))
            {
                Logger.Info($"目标文件{targetFile}与下载文件内容一致，忽略");
                return;
            }
            File.Copy(sourceFile, targetFile, true);
            Logger.Info($"目标文件{targetFile}与下载文件内容不一致，覆盖");
        }

        /// <summary>
        /// 写文件
        /// </summary>
        /// <param name="path"></param>
        /// <param name="data"></param>
        static void WriteFile(string path, string data)
        {
            try
            {
                File.WriteAllText(path, data);
            }
            catch (Exception ex)
            {
                Logger.Error("", ex);
            }
        }

        /// <summary>
        /// 将unicode转换成中文，可以包括其他字符   
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string Uncode(string str)
        {
            string outStr = "";
            Regex reg = new Regex(@"(?i)\\u([0-9a-f]{4})");
            outStr = reg.Replace(str, delegate (Match m1)
            {
                return ((char)Convert.ToInt32(m1.Groups[1].Value, 16)).ToString();
            });
            return outStr;
        }


        /// <summary>
        /// 退出
        /// </summary>
        public static void Stop()
        {
            ZooKeeperHelper.Instance.Dispose();
        }
    }
}
