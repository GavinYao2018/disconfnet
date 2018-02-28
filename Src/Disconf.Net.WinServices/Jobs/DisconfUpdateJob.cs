using Quartz;
using System;

namespace Disconf.Net.WinServices.Jobs
{
    [DisallowConcurrentExecution]
    public class DisconfServiceCheckJob : IJob
    {
        public void Execute(IJobExecutionContext context)
        {
            Init();
        }

        public static void Init()
        {
            try
            {
                DisconfMgr.Init();
            }
            catch (Exception ex)
            {
                Logger.Error("DisconfServiceCheckJob异常", ex);
            }
        }
    }
}
