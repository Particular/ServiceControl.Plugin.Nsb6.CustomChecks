[assembly: System.Runtime.CompilerServices.InternalsVisibleToAttribute(@"ServiceControl.UnitTests, PublicKey=0024000004800000940000000602000000240000525341310004000001000100dde965e6172e019ac82c2639ffe494dd2e7dd16347c34762a05732b492e110f2e4e2e1b5ef2d85c848ccfb671ee20a47c8d1376276708dc30a90ff1121b647ba3b7259a6bc383b2034938ef0e275b58b920375ac605076178123693c6c4f1331661a62eba28c249386855637780e3ff5f23a6d854700eaa6803ef48907513b92")]
[assembly: System.Runtime.Versioning.TargetFrameworkAttribute(".NETFramework,Version=v4.5.2", FrameworkDisplayName=".NET Framework 4.5.2")]

namespace NServiceBus
{
    
    public class static CustomCheckPluginExtensions
    {
        public static void CustomCheckPlugin(this NServiceBus.EndpointConfiguration config, string serviceControlQueue) { }
    }
}
namespace ServiceControl.Features
{
    
    public class CustomChecks : NServiceBus.Features.Feature
    {
        protected override void Setup(NServiceBus.Features.FeatureConfigurationContext context) { }
    }
}
namespace ServiceControl.Plugin.CustomChecks
{
    
    public class CheckResult
    {
        public static ServiceControl.Plugin.CustomChecks.CheckResult Pass;
        public CheckResult() { }
        public string FailureReason { get; set; }
        public bool HasFailed { get; set; }
        public static ServiceControl.Plugin.CustomChecks.CheckResult Failed(string reason) { }
    }
    public abstract class CustomCheck : ServiceControl.Plugin.CustomChecks.ICustomCheck
    {
        protected CustomCheck(string id, string category, System.Nullable<System.TimeSpan> repeatAfter = null) { }
        public string Category { get; }
        public string Id { get; }
        public System.Nullable<System.TimeSpan> Interval { get; }
        public abstract System.Threading.Tasks.Task<ServiceControl.Plugin.CustomChecks.CheckResult> PerformCheck();
        [System.ObsoleteAttribute("Use `Use CheckResult.Failed(string reason) directly inside PerformCheck.` instead" +
            ". The member currently throws a NotImplementedException. Will be removed in vers" +
            "ion 4.0.0.", true)]
        public void ReportFailed(string failureReason) { }
        [System.ObsoleteAttribute("Use `Use CheckResult.Pass directly inside PerformCheck.` instead. The member curr" +
            "ently throws a NotImplementedException. Will be removed in version 4.0.0.", true)]
        public void ReportPass() { }
    }
    public interface ICustomCheck
    {
        string Category { get; }
        string Id { get; }
        System.Nullable<System.TimeSpan> Interval { get; }
        System.Threading.Tasks.Task<ServiceControl.Plugin.CustomChecks.CheckResult> PerformCheck();
    }
    [System.ObsoleteAttribute("Use `Inherit from CustomCheck and set repeatAfter in the CustomCheck constructor " +
        "to the desired interval.` instead. Will be removed in version 4.0.0.", true)]
    public abstract class PeriodicCheck
    {
        protected PeriodicCheck() { }
    }
}