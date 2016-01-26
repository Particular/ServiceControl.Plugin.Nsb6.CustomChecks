namespace ServiceControl.Plugin.CustomChecks.Internal
{
    using System;
    using System.Threading.Tasks;

    // needed for DI
    public interface IPeriodicCheck : ICheck
    {
        TimeSpan Interval { get; }

        Task<CheckResult> PerformCheck();
    }
}
