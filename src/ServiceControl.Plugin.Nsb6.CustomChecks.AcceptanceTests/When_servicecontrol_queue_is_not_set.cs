namespace ServiceControl.Plugin.Nsb6.CustomChecks.AcceptanceTests
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NUnit.Framework;
    using ServiceControl.Plugin.CustomChecks;

    class When_servicecontrol_queue_is_not_set : NServiceBusAcceptanceTest
    {
        [Test]
        public void The_endpoint_should_not_start_with_custom_checks()
        {
            var ex = Assert.ThrowsAsync<AggregateException>(async () => await Scenario.Define<Context>()
                .WithEndpoint<Sender>()
                .Run());

            // ReSharper disable once PossibleNullReferenceException
            Assert.IsTrue(ex.InnerExceptions[0].InnerException.Message.Contains("ServiceControl queue is not specified"));
        }

        class Context : ScenarioContext
        {
        }

        class Sender : EndpointConfigurationBuilder
        {
            public Sender()
            {
                EndpointSetup<DefaultServer>();
            }
        }

        class MyCustomCheck : CustomCheck
        {
            public MyCustomCheck()
                : base("SuccessfulCustomCheck", "CustomCheck")
            {
            }

            public override Task<CheckResult> PerformCheck()
            {
                return CheckResult.Pass;
            }
        }
    }
}