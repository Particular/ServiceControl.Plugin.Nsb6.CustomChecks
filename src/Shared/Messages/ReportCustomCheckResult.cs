﻿namespace ServiceControl.Plugin.CustomChecks.Messages
{
    using System;
    using NServiceBus;
    using NServiceBus.Settings;
    using Internal;

    class ReportCustomCheckResult
    {
        public ReportCustomCheckResult(ICheck check, CheckResult result)
        {
            CustomCheckId = check.Id;
            Category = check.Category;
            HasFailed = result.HasFailed;
            FailureReason = result.FailureReason;
            ReportedAt = DateTime.UtcNow;
        }

        public string CustomCheckId { get; }
        public string Category { get; }
        public bool HasFailed { get; }
        public string FailureReason { get; }
        public DateTime ReportedAt { get; }

        public Guid HostId { get; private set; }
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
