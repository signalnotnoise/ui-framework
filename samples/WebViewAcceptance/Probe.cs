using System.Windows.Threading;

namespace WebViewAcceptance;

internal static class Probe
{
    internal static TaskCompletionSource Signal() => new(TaskCreationOptions.RunContinuationsAsynchronously);
    internal static async Task Await(Task task) => await task.WaitAsync(TimeSpan.FromSeconds(30));
    internal static async Task Flush() => await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
    internal static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
