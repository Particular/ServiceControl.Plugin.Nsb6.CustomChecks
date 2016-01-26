namespace ServiceControl.Plugin.CustomChecks
{
    using System.Threading.Tasks;

    public class CheckResult
    {
        public bool HasFailed { get; set; }
        public string FailureReason { get; set; }

        public static CheckResult Pass => new CheckResult();

        public static CheckResult Failed(string reason)
        {
            return new CheckResult
            {
                HasFailed = true,
                FailureReason = reason
            };
        }

        public static implicit operator Task<CheckResult>(CheckResult result)
        {
            return Task.FromResult(result);
        }
    }
}