namespace ServiceControl.Features
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using NServiceBus;
    using NServiceBus.Features;
    using NServiceBus.Settings;
    using NServiceBus.Transport;
    using Plugin;
    using Plugin.CustomChecks;

    /// <summary>
    /// The ServiceControl.CustomChecks plugin.
    /// </summary>
    public class CustomChecks : Feature
    {
        internal CustomChecks()
        {
            EnableByDefault();
        }

        /// <summary>Called when the features is activated.</summary>
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
                if (!customChecks.Any())
                {
                    return;
                }

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
                return customChecks.Any() ? Task.WhenAll(timerPeriodicChecks.Select(t => t.Stop()).ToArray()) : Task.FromResult(0);
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