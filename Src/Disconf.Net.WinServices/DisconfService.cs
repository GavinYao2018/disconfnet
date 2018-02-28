using System.ServiceProcess;
using Quartz;
using System;
using System.Text;
using Quartz.Impl.Triggers;
using System.Collections;
using System.Configuration;
using Disconf.Net.WinServices.Jobs;
using Quartz.Impl;

namespace Disconf.Net.WinServices
{
    public partial class DisconfService : ServiceBase
    {
        IScheduler _sched = null;
        private const string MyGroupName = "DisconfJobGroup";
        private const string TrigGroupName = "DisconfTrigerGroup";

        public DisconfService()
        {
            InitializeComponent();
        }


        protected override void OnStart(string[] args)
        {
            try
            {
                Logger.Info("启动第一次查询");
                DisconfServiceCheckJob.Init();
                Logger.Info("第一次查询完毕");

                ISchedulerFactory sf = new StdSchedulerFactory();
                _sched = sf.GetScheduler();

                Logger.Info("Disconf任务启动");
                AddSchedule<DisconfServiceCheckJob>(ref _sched, "DisconfServiceCheckJob", TrigGroupName, MyGroupName);
                _sched.Start();

                Logger.Info("任务启动完毕");
            }
            catch (Exception ex)
            {
                Logger.Error($"【服务启动异常】", ex);
            }
        }


        public void AddSchedule<T>(ref IScheduler sched, string croname, string trigroup, string trigGroupName) where T : IJob
        {
            var jobKey = new JobKey(croname + "key", trigroup);
            var jobDetail = JobBuilder.Create<T>().WithIdentity(jobKey).Build();
            var jobTrig = new CronTriggerImpl(croname + "Trig", trigGroupName, SectionString("Crons", croname));
            sched.ScheduleJob(jobDetail, jobTrig);
        }
        /// <summary>
        /// 读取其它节点信息
        /// </summary>
        /// <param name="SectionName"></param>
        /// <param name="Key"></param>
        /// <returns></returns>
        public static string SectionString(string SectionName, string Key)
        {
            IDictionary Dicts = (IDictionary)ConfigurationManager.GetSection(SectionName);
            if (Dicts != null)
            {
                return Dicts[Key] == null ? "" : Dicts[Key].ToString();
            }
            else { return ""; }
        }

        protected override void OnStop()
        {
            var msg = new StringBuilder("");
            try
            {
                msg.AppendLine("定时任务停止");
                _sched.Shutdown(false);

                msg.AppendLine("释放Disconf资源");
                DisconfMgr.Stop();

                msg.AppendLine("服务停止");
                Logger.Info(msg.ToString());
            }
            catch (Exception ex)
            {
                msg.AppendLine($"【服务停止异常】{ex.ToString()}");
                Logger.Error(msg.ToString(), ex);
            }
        }
    }
}
