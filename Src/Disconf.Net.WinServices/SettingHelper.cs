using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml;

namespace Disconf.Net.WinServices
{
    public class SettingHelper : IDisposable
    {
        private string _ServiceName;
        private string _DisplayName;
        private string _Description;

        public SettingHelper()
        {
            string root = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string xmlfile = root.Remove(root.LastIndexOf('\\') + 1) + "ServiceNameSetting.xml";
            if (File.Exists(xmlfile))
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(xmlfile);
                XmlNode xn = doc.SelectSingleNode("Settings/ServiceName");
                _ServiceName = xn.InnerText;
                xn = doc.SelectSingleNode("Settings/DisplayName");
                _DisplayName = xn.InnerText;
                xn = doc.SelectSingleNode("Settings/Description");
                _Description = xn.InnerText;
                doc = null;
            }
            else
            {
                throw new FileNotFoundException("未能找到服务名称配置文件 ServiceNameSetting.xml！");
            }
        }

        /// <summary> 
        /// 系统用于标志此服务的名称 
        /// </summary> 
        public string ServiceName
        {
            get { return _ServiceName; }
        }
        /// <summary> 
        /// 向用户标志服务的友好名称 
        /// </summary> 
        public string DisplayName
        {
            get { return _DisplayName; }
        }
        /// <summary> 
        /// 服务的说明 
        /// </summary> 
        public string Description
        {
            get { return _Description; }
        }

        #region IDisposable 成员
        private bool disposed = false;
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    //managed dispose 
                    _ServiceName = null;
                    _DisplayName = null;
                    _Description = null;
                }
                //unmanaged dispose 
            }
            disposed = true;
        }
        ~SettingHelper()
        {
            Dispose(false);
        }
        #endregion 
    }
}
