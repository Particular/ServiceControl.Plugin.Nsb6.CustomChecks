namespace ServiceControl.Plugin.CustomChecks.Sample
{
    using CustomChecks;

    class FailingCustomCheck : CustomCheck
    {
        public FailingCustomCheck()
            : base("FailingCustomCheck", "CustomCheck")
        {
            ReportFailed("Some reason");
        }
    }
}