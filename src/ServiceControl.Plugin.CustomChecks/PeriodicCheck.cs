namespace ServiceControl.Plugin.CustomChecks
{
    using System;
    using Internal;

    public abstract class PeriodicCheck : IPeriodicCheck
    {
        protected PeriodicCheck(string id, string category, TimeSpan interval)
        {
            Category = category;
            Id = id;
            Interval = interval;
        }

        public abstract CheckResult PerformCheck();

        public TimeSpan Interval { get; private set; }

        public string Category { get; private set; }

        public string Id { get; private set; }
    }
}