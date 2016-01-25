namespace ServiceControl.Plugin.CustomChecks.Sample
{
    using CustomChecks;

    class SuccessfullCustomCheck : CustomCheck
    {
        public SuccessfullCustomCheck()
            : base("SuccessfullCustomCheck", "CustomCheck")
        {
            ReportPass();
        }
    }
}