namespace UI_Framework;

internal static class StateNotifications
{
    internal static void Deliver(Action? subscribers)
    {
        Dependencies.NotificationDepth++;
        try
        {
            List<Exception>? errors = null;
            foreach (var handler in Delegate.EnumerateInvocationList(subscribers))
            {
                try { handler(); }
                catch (Exception error) { (errors ??= []).Add(error); }
            }
            if (errors is { Count: 1 }) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(errors[0]).Throw();
            if (errors is not null) throw new AggregateException("State observers failed.", errors);
        }
        finally { Dependencies.NotificationDepth--; }
    }
}
