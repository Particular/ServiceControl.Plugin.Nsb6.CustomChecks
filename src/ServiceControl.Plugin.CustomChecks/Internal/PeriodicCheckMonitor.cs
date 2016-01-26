namespace ServiceControl.Plugin.CustomChecks.Internal
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using NServiceBus;
    using NServiceBus.ObjectBuilder;
    using NServiceBus.Settings;
    using NServiceBus.Transports;

    /// <summary>
    ///     This class will upon startup get the registered PeriodicCheck types
    ///     and will invoke each one's PerformCheck at the desired interval.
    /// </summary>
    class PeriodicCheckMonitor : IWantToRunWhenBusStartsAndStops
    {
        public IDispatchMessages Dispatcher { get; set; }
        public ReadOnlySettings Settings { get; set; }
        public IBuilder Builder { get; set; }

        public CriticalError CriticalError { get; set; }

// ReSharper disable NotAccessedField.Local
        List<ICustomCheck> customChecks;
// ReSharper restore NotAccessedField.Local
        List<TimerBasedPeriodicCheck> timerPeriodicChecks;
        public Task Start(IBusSession session)
        {
            var periodicChecks = Builder.BuildAll<IPeriodicCheck>().ToList();
            timerPeriodicChecks = new List<TimerBasedPeriodicCheck>(periodicChecks.Count);

            foreach (var check in periodicChecks)
            {
                timerPeriodicChecks.Add(new TimerBasedPeriodicCheck(check, Dispatcher, Settings, CriticalError));
            }

            customChecks = Builder.BuildAll<ICustomCheck>().ToList();
            return Task.FromResult(0);
        }

        public Task Stop(IBusSession session)
        {
            return Task.WhenAll(timerPeriodicChecks.Select(t => t.Stop()).ToArray());
        }
    }
}