namespace ServiceControl.Plugin.CustomChecks
{
    using System.Threading.Tasks;
    using NServiceBus;
    using NServiceBus.ObjectBuilder;
    using NServiceBus.Settings;
    using NServiceBus.Transports;
    using ServiceControl.Plugin.CustomChecks.Internal;
    using ServiceControl.Plugin.CustomChecks.Messages;

    public abstract class CustomCheck : ICustomCheck
    {
        protected CustomCheck(string id, string category)
        {
            Category = category;
            Id = id;
        }

        public ReadOnlySettings Settings { get; set; }
        public CriticalError CriticalError { get; set; }
        public IBuilder Builder { get; set; }

        public string Category { get; }

        public string Id { get; }

        public abstract Task PerformCheck();

        protected Task ReportPass()
        {
            return ReportToBackend(CheckResult.Pass);
        }

        protected Task ReportFailed(string failureReason)
        {
            return ReportToBackend(CheckResult.Failed(failureReason));
        }

        async Task ReportToBackend(CheckResult result)
        {
            var dispatcher = Builder.Build<IDispatchMessages>();

            var serviceControlBackend = new ServiceControlBackend(dispatcher, Settings, CriticalError);
            await serviceControlBackend.VerifyIfServiceControlQueueExists().ConfigureAwait(false);
            await serviceControlBackend.Send(new ReportCustomCheckResult(this, result)).ConfigureAwait(false);
        }
    }
}