namespace ServiceControl.Plugin.CustomChecks
{
    using System;
    using System.Threading.Tasks;
    using JetBrains.Annotations;

    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    public interface ICustomCheck
    {
        string Category { get; }
        string Id { get; }

        TimeSpan? Interval { get; }
        Task<CheckResult> PerformCheck();
    }
}
