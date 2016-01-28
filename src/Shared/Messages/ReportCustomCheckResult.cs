namespace ServiceControl.Plugin.CustomChecks.Messages
{
    using System;
    using NServiceBus;
    using NServiceBus.Settings;

    class ReportCustomCheckResult
    {
        public ReportCustomCheckResult(string customCheckId, string category, CheckResult result)
        {
            CustomCheckId = customCheckId;
            Category = category;
            HasFailed = result.HasFailed;
            FailureReason = result.FailureReason;
            ReportedAt = DateTime.UtcNow;
        }

        public Guid HostId { get; private set; }
        public string CustomCheckId { get; }
        public string Category { get; }
        public bool HasFailed { get; }
        public string FailureReason { get; }

        public DateTime ReportedAt { get; }
        public string EndpointName { get; private set; }
        public string Host { get; private set; }

        public void Apply(ReadOnlySettings settings)
        {
            HostId = settings.Get<Guid>("NServiceBus.HostInformation.HostId");
            Host = settings.Get<string>("NServiceBus.HostInformation.DisplayName");
            EndpointName = settings.EndpointName().ToString();
        }
    }
}
