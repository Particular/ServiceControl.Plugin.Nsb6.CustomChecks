namespace ServiceControl.Plugin.CustomChecks.Sample
{
    using System.Threading.Tasks;
    using CustomChecks;

    class SuccessfullCustomCheck : CustomCheck
    {
        public SuccessfullCustomCheck()
            : base("SuccessfullCustomCheck", "CustomCheck")
        {
        }

        public override Task PerformCheck()
        {
            return ReportPass();
        }
    }
}