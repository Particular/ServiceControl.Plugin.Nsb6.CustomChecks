namespace ServiceControl.Plugin.CustomChecks
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using NServiceBus;
    using NServiceBus.Features;
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
            context.Settings.GetAvailableTypes()
                .Where(t => typeof(ICustomCheck).IsAssignableFrom(t) && !(t.IsAbstract || t.IsInterface))
                .ToList()
                .ForEach(t => context.Container.ConfigureComponent(t, DependencyLifecycle.InstancePerCall));

            context.Settings.GetAvailableTypes()
                .Where(t => typeof(IPeriodicCheck).IsAssignableFrom(t) && !(t.IsAbstract || t.IsInterface))
                .ToList()
                .ForEach(t => context.Container.ConfigureComponent(t, DependencyLifecycle.InstancePerCall));

            // TODO: Way to much builder access
            context.RegisterStartupTask(b => new CustomChecksStartup(b.BuildAll<IPeriodicCheck>(), b.BuildAll<ICustomCheck>(), context.Settings, b.Build<CriticalError>(), b.Build<IDispatchMessages>()));
        }

        class CustomChecksStartup : FeatureStartupTask
        {
            public CustomChecksStartup(IEnumerable<IPeriodicCheck> periodicChecks, IEnumerable<ICustomCheck> customChecks, ReadOnlySettings settings, CriticalError criticalError, IDispatchMessages dispatcher)
            {
                dispatchMessages = dispatcher;
                this.criticalError = criticalError;
                this.settings = settings;
                this.customChecks = customChecks.ToList();
                this.periodicChecks = periodicChecks.ToList();
            }

            protected override async Task OnStart(IBusSession session)
            {
                timerPeriodicChecks = new List<TimerBasedPeriodicCheck>(periodicChecks.Count);

                // TODO: Should we start them concurrently?
                foreach (var check in periodicChecks)
                {
                    var serviceControlBackend = new ServiceControlBackend(dispatchMessages, settings, criticalError);
                    await serviceControlBackend.VerifyIfServiceControlQueueExists().ConfigureAwait(false);

                    var timerBasedPeriodicCheck = new TimerBasedPeriodicCheck(check, serviceControlBackend);
                    timerBasedPeriodicCheck.Start();

                    timerPeriodicChecks.Add(timerBasedPeriodicCheck);
                }

                await Task.WhenAll(customChecks.Select(c => c.PerformCheck())).ConfigureAwait(false);
            }

            protected override Task OnStop(IBusSession session)
            {
                return Task.WhenAll(timerPeriodicChecks.Select(t => t.Stop()).ToArray());
            }

            readonly CriticalError criticalError;
            readonly List<ICustomCheck> customChecks;
            readonly IDispatchMessages dispatchMessages;
            readonly List<IPeriodicCheck> periodicChecks;
            readonly ReadOnlySettings settings;
            List<TimerBasedPeriodicCheck> timerPeriodicChecks;
        }
    }
}