namespace ServiceControl.Plugin.CustomChecks
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using NServiceBus;
    using NServiceBus.Features;
    using NServiceBus.Settings;
    using NServiceBus.Transports;

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

            context.RegisterStartupTask(b => new CustomChecksStartup(b.BuildAll<ICustomCheck>(), context.Settings, b.Build<CriticalError>(), b.Build<IDispatchMessages>()));
        }

        class CustomChecksStartup : FeatureStartupTask
        {
            public CustomChecksStartup(IEnumerable<ICustomCheck> customChecks, ReadOnlySettings settings, CriticalError criticalError, IDispatchMessages dispatcher)
            {
                dispatchMessages = dispatcher;
                this.criticalError = criticalError;
                this.settings = settings;
                this.customChecks = customChecks.ToList();
            }

            protected override async Task OnStart(IMessageSession session)
            {
                timerPeriodicChecks = new List<TimerBasedPeriodicCheck>(customChecks.Count);
                serviceControlBackend = new ServiceControlBackend(dispatchMessages, settings, criticalError);
                await serviceControlBackend.VerifyIfServiceControlQueueExists().ConfigureAwait(false);

                foreach (var check in customChecks)
                {
                    var timerBasedPeriodicCheck = new TimerBasedPeriodicCheck(check, serviceControlBackend);
                    timerBasedPeriodicCheck.Start();

                    timerPeriodicChecks.Add(timerBasedPeriodicCheck);
                }
            }

            protected override Task OnStop(IMessageSession session)
            {
                return Task.WhenAll(timerPeriodicChecks.Select(t => t.Stop()).ToArray());
            }

            readonly CriticalError criticalError;
            readonly List<ICustomCheck> customChecks;
            readonly IDispatchMessages dispatchMessages;
            readonly ReadOnlySettings settings;
            List<TimerBasedPeriodicCheck> timerPeriodicChecks;
            ServiceControlBackend serviceControlBackend;
        }
    }
}