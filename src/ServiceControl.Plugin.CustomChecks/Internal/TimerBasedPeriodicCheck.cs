namespace ServiceControl.Plugin.CustomChecks.Internal
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus;
    using NServiceBus.Logging;
    using NServiceBus.Settings;
    using NServiceBus.Transports;
    using ServiceControl.Plugin.CustomChecks.Messages;

    public class TimerBasedPeriodicCheck
    {
        static ILog Logger = LogManager.GetLogger(typeof(TimerBasedPeriodicCheck));

        public TimerBasedPeriodicCheck(IPeriodicCheck periodicCheck, IDispatchMessages messageSender, ReadOnlySettings settings, CriticalError criticalError)
        {
            this.periodicCheck = periodicCheck;
            this.settings = settings;
            serviceControlBackend = new ServiceControlBackend(messageSender, settings, criticalError);

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
                result = periodicCheck.PerformCheck();
            }
            catch (Exception ex)
            {
                var reason = string.Format("'{0}' implementation failed to run.", periodicCheck.GetType());
                result = CheckResult.Failed(reason);
                Logger.Error(reason, ex);
            }

            try
            {
                await ReportToBackend(result, periodicCheck.Id, periodicCheck.Category, TimeSpan.FromTicks(periodicCheck.Interval.Ticks*4)).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to report periodic check to ServiceControl.", ex);
            }
        }

        Task ReportToBackend(CheckResult result, string customCheckId, string category, TimeSpan ttr)
        {
            var reportCustomCheckResult = new ReportCustomCheckResult
            {
                HostId = settings.Get<Guid>("NServiceBus.HostInformation.HostId"),
                Host = settings.Get<string>("NServiceBus.HostInformation.DisplayName"),
                EndpointName = settings.EndpointName().ToString(),
                CustomCheckId = customCheckId,
                Category = category,
                HasFailed = result.HasFailed,
                FailureReason = result.FailureReason,
                ReportedAt = DateTime.UtcNow
            };

            return serviceControlBackend.Send(reportCustomCheckResult, ttr);
        }

        IPeriodicCheck periodicCheck;
        ServiceControlBackend serviceControlBackend;
        ReadOnlySettings settings;
        AsyncTimer timer;
    }
}