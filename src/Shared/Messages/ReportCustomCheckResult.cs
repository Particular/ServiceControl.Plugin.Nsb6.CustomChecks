namespace ServiceControl.Plugin.CustomChecks.Messages
{
    using System;
    using System.Runtime.Serialization;
    using NServiceBus;
    using NServiceBus.Settings;

    [DataContract]
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

        [DataMember]
        public Guid HostId { get; private set; }
        [DataMember]
        public string CustomCheckId { get; private set; }
        [DataMember]
        public string Category { get; private set; }
        [DataMember]
        public bool HasFailed { get; private set; }
        [DataMember]
        public string FailureReason { get; private set; }
        [DataMember]
        public DateTime ReportedAt { get; private set; }
        [DataMember]
        public string EndpointName { get; private set; }
        [DataMember]
        public string Host { get; private set; }

        public void Apply(ReadOnlySettings settings)
        {
            HostId = settings.Get<Guid>("NServiceBus.HostInformation.HostId");
            Host = settings.Get<string>("NServiceBus.HostInformation.DisplayName");
            EndpointName = settings.EndpointName().ToString();
        }
    }
}
