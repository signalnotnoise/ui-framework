using System;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Runtime.CompilerServices;
using System.Diagnostics;

internal static class StaTestRunner
{
    public static void Run(Action test, [CallerMemberName] string testName = "unknown")
    {
        Exception? failure = null;

        var elapsed = Stopwatch.StartNew();
        var thread = new Thread(() =>
        {
            try
            {
                test();
            }
            catch (Exception error)
            {
                failure = error;
            }
        }) { IsBackground = true, Name = $"STA: {testName}" };

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        if (!thread.Join(TimeSpan.FromSeconds(60)))
        {
            // An abandoned WPF thread can corrupt subsequent tests. Terminate the
            // isolated test host; the CI runner retains the crash diagnostic.
            Environment.FailFast($"STA test '{testName}' exceeded 60 seconds; elapsed={elapsed.Elapsed}; thread={thread.ManagedThreadId}; state={thread.ThreadState}.");
        }

        if (failure is not null)
            ExceptionDispatchInfo.Capture(failure).Throw();
    }
}
