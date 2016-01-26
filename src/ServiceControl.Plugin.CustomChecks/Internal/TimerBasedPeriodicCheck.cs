namespace ServiceControl.Plugin.CustomChecks.Internal
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.Logging;
    using ServiceControl.Plugin.CustomChecks.Messages;

    class TimerBasedPeriodicCheck
    {
        static ILog Logger = LogManager.GetLogger(typeof(TimerBasedPeriodicCheck));

        public TimerBasedPeriodicCheck(IPeriodicCheck periodicCheck, ServiceControlBackend serviceControlBacked)
        {
            this.periodicCheck = periodicCheck;
            this.serviceControlBackend = serviceControlBacked;
        }

        public void Start()
        {
            timer = new AsyncTimer();
            timer.Start(Run, periodicCheck.Interval, e => { /* should not happen */});
        }

        public Task Stop()
        {
            return timer.Stop();
        }

        async Task Run()
        {
            CheckResult result;
            try
            {
                result = await periodicCheck.PerformCheck().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                var reason = $"'{periodicCheck.GetType()}' implementation failed to run.";
                result = CheckResult.Failed(reason);
                Logger.Error(reason, ex);
            }

            try
            {
                await ReportToBackend(result, TimeSpan.FromTicks(periodicCheck.Interval.Ticks*4)).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to report periodic check to ServiceControl.", ex);
            }
        }

        Task ReportToBackend(CheckResult result, TimeSpan ttr)
        {
            return serviceControlBackend.Send(new ReportCustomCheckResult(periodicCheck, result), ttr);
        }

        IPeriodicCheck periodicCheck;
        ServiceControlBackend serviceControlBackend;
        AsyncTimer timer;
    }
}