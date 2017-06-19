namespace ServiceControl.Plugin.CustomChecks
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.Logging;
    using Messages;

    class TimerBasedPeriodicCheck
    {
        static ILog Logger = LogManager.GetLogger(typeof(TimerBasedPeriodicCheck));

        public TimerBasedPeriodicCheck(ICustomCheck customCheck, ServiceControlBackend serviceControlBackend)
        {
            this.customCheck = customCheck;
            this.serviceControlBackend = serviceControlBackend;
        }

        public void Start()
        {
            timer = new AsyncTimer();
            timer.Start(Run, customCheck.Interval, e => { /* should not happen */});
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
                result = await customCheck.PerformCheck().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                var reason = $"'{customCheck.GetType()}' implementation failed to run.";
                result = CheckResult.Failed(reason);
                Logger.Error(reason, ex);
            }

            try
            {
                await ReportToBackend(result, customCheck.Interval.HasValue ? TimeSpan.FromTicks( customCheck.Interval.Value.Ticks*4) : TimeSpan.MaxValue).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to report periodic check to ServiceControl.", ex);
            }
        }

        Task ReportToBackend(CheckResult result, TimeSpan timeToBeReceived)
        {
            return serviceControlBackend.Send(new ReportCustomCheckResult(customCheck.Id, customCheck.Category, result), timeToBeReceived);
        }

        ICustomCheck customCheck;
        ServiceControlBackend serviceControlBackend;
        AsyncTimer timer;
    }
}