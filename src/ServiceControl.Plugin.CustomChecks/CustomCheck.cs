namespace ServiceControl.Plugin.CustomChecks
{
    using System;
    using System.Threading.Tasks;

    public abstract class CustomCheck : ICustomCheck
    {
        protected CustomCheck(string id, string category, TimeSpan? repeatAfter = null)
        {
            Category = category;
            Id = id;
            Interval = repeatAfter;
        }

        public string Category { get; }

        public string Id { get; }

        public TimeSpan? Interval { get; }

        public abstract Task<CheckResult> PerformCheck();
    }
}