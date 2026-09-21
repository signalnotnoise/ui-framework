namespace UI_Framework;

internal static class Dependencies
{
    [ThreadStatic] internal static HashSet<IState>? Current;
    [ThreadStatic] internal static int NotificationDepth;
    internal static void Track(IState state) => Current?.Add(state);

    internal static void Reconcile(
        HashSet<IState> current,
        HashSet<IState> next,
        Action<IState> subscribe,
        Action<IState> unsubscribe)
    {
        List<IState>? added = null;
        try
        {
            foreach (var state in next)
            {
                if (current.Contains(state)) continue;
                subscribe(state);
                (added ??= []).Add(state);
            }
        }
        catch
        {
            if (added is not null)
                foreach (var state in added) unsubscribe(state);
            throw;
        }

        foreach (var state in current)
            if (!next.Contains(state)) unsubscribe(state);
    }
}
