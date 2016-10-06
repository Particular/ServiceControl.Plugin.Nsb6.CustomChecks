namespace ServiceControl.Plugin.CustomChecks
{
    using System;
    using System.Threading.Tasks;

    public interface ICustomCheck
    {
        string Category { get; }
        string Id { get; }

        TimeSpan? Interval { get; }
        Task<CheckResult> PerformCheck();
    }
}
