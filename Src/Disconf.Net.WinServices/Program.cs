using System;
using System.ServiceProcess;

namespace Disconf.Net.WinServices
{
    static class Program
    {
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        static void Main()
        {
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);
            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[]
            {
                new DisconfService()
            };
            ServiceBase.Run(ServicesToRun);
        }
        static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Logger.Error("Disconf.Net UnhandledException", e.ExceptionObject as Exception);
        }
    }
}
