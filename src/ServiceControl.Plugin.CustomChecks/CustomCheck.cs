namespace ServiceControl.Plugin.CustomChecks
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Internal;
    using NServiceBus;
    using Messages;
    using NServiceBus.ObjectBuilder;
    using NServiceBus.Settings;
    using NServiceBus.Transports;

    public abstract class CustomCheck : ICustomCheck
    {
        public ReadOnlySettings Settings { get; set; }
        public CriticalError CriticalError { get; set; }

        Timer delayExecutionPassTimer;
        Timer delayExecutionFailTimer; 

        public IBuilder Builder { get; set; }
        
        protected CustomCheck(string id, string category)
        {
            Category = category;
            Id = id;
        }

        public string Category { get; private set; }

        public void ReportPass()
        {
            // Delay execution if builder is null until builder is available.
            DelayExecutionForPass(null);
        }

        void DelayExecutionForPass(object state)
        {
            if (Builder != null)
            {
                delayExecutionPassTimer?.Dispose();
                ReportToBackend(CheckResult.Pass);
            }
            else
            {
                if (delayExecutionPassTimer != null)
                {
                    delayExecutionPassTimer.Change(2000, -1);
                }
                else
                {
                    delayExecutionPassTimer = new Timer(DelayExecutionForPass, state, 2000, -1);
                }
            }
        }

        void DelayExecutionForFail(object state)
        {
            if (Builder != null)
            {
                delayExecutionFailTimer?.Dispose();
                ReportToBackend(CheckResult.Failed((string)state));
            }
            else
            {
                if (delayExecutionFailTimer != null)
                {
                    delayExecutionFailTimer.Change(2000, -1);
                }
                else
                {
                    delayExecutionFailTimer = new Timer(DelayExecutionForFail, state, 2000, -1);
                }
            }
        }

        public void ReportFailed(string failureReason)
        {
            DelayExecutionForFail(failureReason);
        }

        public string Id { get; private set; }

        Task ReportToBackend(CheckResult result)
        {
            var dispatcher = Builder.Build<IDispatchMessages>();

            var serviceControlBackend = new ServiceControlBackend(dispatcher, Settings, CriticalError);
            return serviceControlBackend.Send(new ReportCustomCheckResult
            {
                HostId = Settings.Get<Guid>("NServiceBus.HostInformation.HostId"),
                Host = Settings.Get<string>("NServiceBus.HostInformation.DisplayName"),
                EndpointName = Settings.EndpointName().ToString(),
                CustomCheckId = Id,
                Category = Category,
                HasFailed = result.HasFailed,
                FailureReason = result.FailureReason,
                ReportedAt = DateTime.UtcNow
            });
        }
    }
}