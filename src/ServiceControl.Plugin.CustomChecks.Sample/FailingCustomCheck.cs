namespace ServiceControl.Plugin.CustomChecks.Sample
{
    using System.Threading.Tasks;
    using CustomChecks;

    class FailingCustomCheck : CustomCheck
    {
        public FailingCustomCheck()
            : base("FailingCustomCheck", "CustomCheck")
        {
        }

        public override Task PerformCheck()
        {
            return ReportFailed("Some reason");
        }
    }
}