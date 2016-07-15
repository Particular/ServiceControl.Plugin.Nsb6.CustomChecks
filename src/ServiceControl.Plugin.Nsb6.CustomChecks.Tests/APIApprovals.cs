namespace ServiceControl.Plugin.Nsb6.CustomChecks.Tests
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using ApiApprover;
    using ApprovalTests;
    using Mono.Cecil;
    using NUnit.Framework;
    using ServiceControl.Plugin.CustomChecks;

    [TestFixture]
    public class APIApprovals
    {
        [Test]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void ApprovePublicApi()
        {
            var assemblyPath = Path.GetFullPath(typeof(ICustomCheck).Assembly.Location);
            var asm = AssemblyDefinition.ReadAssembly(assemblyPath);
            var publicApi = Filter(PublicApiGenerator.CreatePublicApiForAssembly(asm));
            Approvals.Verify(publicApi);
        }

        string Filter(string text)
        {
            return string.Join(Environment.NewLine, text.Split(new[]
            {
                Environment.NewLine
            }, StringSplitOptions.RemoveEmptyEntries)
                .Where(l => !string.IsNullOrWhiteSpace(l))
                );
        }

    }
}
