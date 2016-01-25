namespace ServiceControl.Plugin.CustomChecks.Internal
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using NServiceBus;
    using NServiceBus.ObjectBuilder;
    using NServiceBus.Transports;
    using NServiceBus.Unicast;

    /// <summary>
    ///     This class will upon startup get the registered PeriodicCheck types
    ///     and will invoke each one's PerformCheck at the desired interval.
    /// </summary>
    class PeriodicCheckMonitor : IWantToRunWhenBusStartsAndStops
    {
        public UnicastBus UnicastBus { get; set; }
        public ISendMessages MessageSender { get; set; }
        public IBuilder Builder { get; set; }

        public Configure Configure { get; set; }
        public CriticalError CriticalError { get; set; }

        public void Start()
        {
            var periodicChecks = Builder.BuildAll<IPeriodicCheck>().ToList();
            timerPeriodicChecks = new List<TimerBasedPeriodicCheck>(periodicChecks.Count);

            foreach (var check in periodicChecks)
            {
                timerPeriodicChecks.Add(new TimerBasedPeriodicCheck(check, MessageSender, Configure, UnicastBus, CriticalError));
            }

            customChecks = Builder.BuildAll<ICustomCheck>().ToList();
        }

        public void Stop()
        {
            Parallel.ForEach(timerPeriodicChecks, t => t.Dispose());
        }

// ReSharper disable NotAccessedField.Local
        List<ICustomCheck> customChecks;
// ReSharper restore NotAccessedField.Local
        List<TimerBasedPeriodicCheck> timerPeriodicChecks;
    }
}