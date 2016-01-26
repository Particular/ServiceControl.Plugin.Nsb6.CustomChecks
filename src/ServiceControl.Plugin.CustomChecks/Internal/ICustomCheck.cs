﻿namespace ServiceControl.Plugin.CustomChecks.Internal
{
    using System.Threading.Tasks;

    // needed for DI
    public interface ICustomCheck : ICheck
    {
        Task PerformCheck();
    }
}
