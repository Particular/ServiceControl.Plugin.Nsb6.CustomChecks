namespace ServiceControl.Plugin.CustomChecks.Internal
{
    using System;
    using System.Threading;
    using Messages;
    using NServiceBus;
    using NServiceBus.Logging;
    using NServiceBus.Transports;
    using NServiceBus.Unicast;

    public class TimerBasedPeriodicCheck : IDisposable
    {
        public TimerBasedPeriodicCheck(IPeriodicCheck periodicCheck, ISendMessages messageSender, Configure configure, UnicastBus unicastBus, CriticalError criticalError)
        {

            this.periodicCheck = periodicCheck;
            this.configure = configure;
            this.unicastBus = unicastBus;
            serviceControlBackend = new ServiceControlBackend(messageSender, configure, criticalError);

            timer = new Timer(Run, null, TimeSpan.Zero, periodicCheck.Interval);
        }

        public void Dispose()
        {
            using (var waitHandle = new ManualResetEvent(false))
            {
                timer.Dispose(waitHandle);

                waitHandle.WaitOne();
            }
        }

        void ReportToBackend(CheckResult result, string customCheckId, string category, TimeSpan ttr)
        {
            var reportCustomCheckResult = new ReportCustomCheckResult
            {
                HostId = unicastBus.HostInformation.HostId,
                Host = unicastBus.HostInformation.DisplayName,
                EndpointName = configure.Settings.EndpointName(),
                CustomCheckId = customCheckId,
                Category = category,
                HasFailed = result.HasFailed,
                FailureReason = result.FailureReason,
                ReportedAt = DateTime.UtcNow
            };
            serviceControlBackend.Send(reportCustomCheckResult, ttr);
        }

        void Run(object state)
        {
            CheckResult result;
            try
            {
                result = periodicCheck.PerformCheck();
            }
            catch (Exception ex)
            {
                var reason = String.Format("'{0}' implementation failed to run.", periodicCheck.GetType());
                result = CheckResult.Failed(reason);
                Logger.Error(reason, ex);
            }

            try
            {
                ReportToBackend(result, periodicCheck.Id, periodicCheck.Category,
                    TimeSpan.FromTicks(periodicCheck.Interval.Ticks*4));
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to report periodic check to ServiceControl.", ex);
            }
        }

        static ILog Logger = LogManager.GetLogger(typeof(TimerBasedPeriodicCheck));
        IPeriodicCheck periodicCheck;
        ServiceControlBackend serviceControlBackend;
        Timer timer;
        UnicastBus unicastBus;
        Configure configure;
    }
}