namespace ServiceControl.Plugin.CustomChecks
{
    using System;
    using System.Threading.Tasks;
    using ServiceControl.Plugin.CustomChecks.Internal;

    public abstract class PeriodicCheck : IPeriodicCheck
    {
        protected PeriodicCheck(string id, string category, TimeSpan interval)
        {
            Category = category;
            Id = id;
            Interval = interval;
        }

        public TimeSpan Interval { get; }

        public string Category { get; }

        public string Id { get; }

        public abstract Task<CheckResult> PerformCheck();
    }
}