using System.Windows.Threading;

internal static class DispatcherTaskPump
{
    internal static void Complete(Task task)
    {
        if (!task.IsCompleted)
        {
            var dispatcher = Dispatcher.CurrentDispatcher;
            var frame = new DispatcherFrame();
            var timedOut = false;
            var timer = new DispatcherTimer(DispatcherPriority.Send) { Interval = TimeSpan.FromSeconds(10) };
            EventHandler timeout = (_, _) => { timedOut = true; frame.Continue = false; };
            timer.Tick += timeout;
            var wake = task.ContinueWith(_ => dispatcher.BeginInvoke(DispatcherPriority.Send,
                new Action(() => frame.Continue = false)), TaskScheduler.Default);
            timer.Start();
            try { Dispatcher.PushFrame(frame); }
            finally { timer.Stop(); timer.Tick -= timeout; }
            if (timedOut) throw new TimeoutException("Native initialization did not settle while pumping the UI dispatcher.");
            // Ensure the continuation has queued its wakeup before draining the queue.
            wake.GetAwaiter().GetResult();
            TestVisualTree.Flush();
        }
        task.GetAwaiter().GetResult();
    }
}
