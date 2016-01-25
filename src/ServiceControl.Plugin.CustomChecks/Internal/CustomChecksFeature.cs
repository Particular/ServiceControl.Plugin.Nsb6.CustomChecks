namespace ServiceControl.Plugin.CustomChecks.Internal
{
    using System.Linq;
    using NServiceBus;
    using NServiceBus.Features;

    class CustomChecksFeature : Feature
    {

        public CustomChecksFeature()
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
         
        }
    }
}
