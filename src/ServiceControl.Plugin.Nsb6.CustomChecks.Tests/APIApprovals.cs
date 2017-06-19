using System.IO;
using System.Runtime.CompilerServices;
using ApiApprover;
using NUnit.Framework;
using ServiceControl.Plugin.CustomChecks;

[TestFixture]
public class APIApprovals
{
    [Test]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public void ApprovePublicApi()
    {
        Directory.SetCurrentDirectory(TestContext.CurrentContext.TestDirectory);
        PublicApiApprover.ApprovePublicApi(typeof(ICustomCheck).Assembly);
    }
}