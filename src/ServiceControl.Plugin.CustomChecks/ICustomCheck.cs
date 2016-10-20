namespace ServiceControl.Plugin.CustomChecks
{
    using System;
    using System.Threading.Tasks;
    using JetBrains.Annotations;

    /// <summary>
    /// Interface to implement a custom check.
    /// </summary>
    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    public interface ICustomCheck
    {
        /// <summary>
        /// Category for the check.
        /// </summary>
        string Category { get; }

        /// <summary>
        /// Check Id.
        /// </summary>
        string Id { get; }

        /// <summary>
        /// Periodic execution interval.
        /// </summary>
        TimeSpan? Interval { get; }

        /// <summary>
        /// Perfoms the check.
        /// </summary>
        /// <returns>The result of the check.</returns>
        Task<CheckResult> PerformCheck();
    }
}
