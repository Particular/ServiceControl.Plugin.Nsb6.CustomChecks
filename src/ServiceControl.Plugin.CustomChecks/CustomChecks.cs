namespace ServiceControl.Plugin.CustomChecks
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using NServiceBus;
    using NServiceBus.Features;
    using NServiceBus.ObjectBuilder;
    using NServiceBus.Settings;
    using NServiceBus.Transports;
    using ServiceControl.Plugin.CustomChecks.Internal;

    class CustomChecks : Feature
    {
        public CustomChecks()
        {
            EnableByDefault();
        }

        protected override void Setup(FeatureConfigurationContext context)
        {
            context.RegisterStartupTask(b => new CustomChecksStartup());
        }

        class CustomChecksStartup : FeatureStartupTask
        {
            public IDispatchMessages Dispatcher { get; set; }
            public ReadOnlySettings Settings { get; set; }
            public IBuilder Builder { get; set; }

            public CriticalError CriticalError { get; set; }

            // ReSharper disable NotAccessedField.Local
            List<ICustomCheck> customChecks;
            // ReSharper restore NotAccessedField.Local
            List<TimerBasedPeriodicCheck> timerPeriodicChecks;

            protected override Task OnStart(IBusSession session)
            {
                var periodicChecks = Builder.BuildAll<IPeriodicCheck>().ToList();
                timerPeriodicChecks = new List<TimerBasedPeriodicCheck>(periodicChecks.Count);

                foreach (var check in periodicChecks)
                {
                    var timerBasedPeriodicCheck = new TimerBasedPeriodicCheck(check, new ServiceControlBackend(Dispatcher, Settings, CriticalError));
                    timerBasedPeriodicCheck.Start();

                    timerPeriodicChecks.Add(timerBasedPeriodicCheck);
                }

                customChecks = Builder.BuildAll<ICustomCheck>().ToList();
                return Task.WhenAll(customChecks.Select(c => c.PerformCheck()));
            }

            protected override Task OnStop(IBusSession session)
            {
                return Task.WhenAll(timerPeriodicChecks.Select(t => t.Stop()).ToArray());
            }
        }
    }
}