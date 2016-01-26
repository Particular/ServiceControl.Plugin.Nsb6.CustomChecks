namespace ServiceControl.Plugin.CustomChecks.Sample
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using NServiceBus;
    using CustomChecks;

    class InteractiveCustomCheck : PeriodicCheck
    {
        public InteractiveCustomCheck()
            : base("InteractiveCustomCheck", "CustomCheck",TimeSpan.FromSeconds(5))
        {
            
        }

        public static bool ShouldFail { get; set; }
        public override CheckResult PerformCheck()
        {
            return ShouldFail ? CheckResult.Failed("User asked me to fail") : CheckResult.Pass;
        }
    }

    class CommandLineHook: IWantToRunWhenBusStartsAndStops
    {
        private CancellationTokenSource tokenSource = new CancellationTokenSource();
        private Task loopTask;

        public Task Start(IBusSession session)
        {
            var token = tokenSource.Token;
            loopTask = Task.Run(() =>
            {
                while (!token.IsCancellationRequested)
                {
                    Console.Out.WriteLine("Hit any key to toggle the interactive check (max delay == 5 s)");

                    Console.ReadKey();

                    InteractiveCustomCheck.ShouldFail = !InteractiveCustomCheck.ShouldFail;
                }
            }, CancellationToken.None);
            return Task.FromResult(0);
        }

        public Task Stop(IBusSession session)
        {
            tokenSource.Cancel();
            return loopTask;
        }
    }
}