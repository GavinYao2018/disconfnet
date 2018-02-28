using System;
using System.IO;
using System.Xml.Linq;

namespace Appsettings
{
    /// <summary>
    /// 工具类
    /// </summary>
    public class AppSettingsUtils
    {
        /// <summary>
        /// 读取xml
        /// 如果抛出资源未释放异常，则尝试读取3次，每次间隔50ms
        /// 若再次抛出异常，则将此异常抛出，不再尝试
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static XDocument LoadXml(string path)
        {
            return LoadResource(() => { return XDocument.Load(path); });
        }

        /// <summary>
        /// 读取properties
        /// 如果抛出资源未释放异常，则尝试读取3次，每次间隔50ms
        /// 若再次抛出异常，则将此异常抛出，不再尝试
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static string[] LoadProperties(string path)
        {
            return LoadResource(() => { return File.ReadAllLines(path); });
        }
        
        /// <summary>
        /// 读取资源
        /// 如果抛出资源未释放异常，则尝试读取3次，每次间隔50ms
        /// 若再次抛出异常，则将此异常抛出，不再尝试
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="func"></param>
        /// <returns></returns>
        private static T LoadResource<T>(Func<T> func)
        {
            int tryCount = 0;
            while (true)
            {
                try
                {
                    return func();
                }
                catch (Exception ex)
                {
                    tryCount++;
                    if (tryCount >= 3)
                    {
                        AppSettingsBase.Log(ex);
                        throw ex;
                    }
                    System.Threading.Thread.Sleep(50);
                }
            }
        }
    }
}
