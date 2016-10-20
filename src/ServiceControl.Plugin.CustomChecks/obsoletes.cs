namespace ServiceControl.Plugin.CustomChecks
{
    using System;

    [ObsoleteEx(ReplacementTypeOrMember = "Inherit from CustomCheck and set repeatAfter in the CustomCheck constructor to the desired interval.", TreatAsErrorFromVersion = "1.0", RemoveInVersion = "3.0")]
#pragma warning disable 1591
    public abstract class PeriodicCheck { }


    public partial class CustomCheck
    {
        [ObsoleteEx(ReplacementTypeOrMember = "Use CheckResult.Pass directly inside PerformCheck.", TreatAsErrorFromVersion = "1.0", RemoveInVersion = "3.0")]
        public void ReportPass()
        {
            throw new NotImplementedException();
        }

        [ObsoleteEx(ReplacementTypeOrMember = "Use CheckResult.Failed(string reason) directly inside PerformCheck.", TreatAsErrorFromVersion = "1.0", RemoveInVersion = "3.0")]
        public void ReportFailed(string failureReason)
        {
            throw new NotImplementedException();
        }
    }
#pragma warning restore 1591
}