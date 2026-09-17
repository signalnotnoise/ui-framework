namespace UI_Framework;

internal static class Dependencies
{
    [ThreadStatic] internal static HashSet<IState>? Current;
    [ThreadStatic] internal static int NotificationDepth;
    internal static void Track(IState state) => Current?.Add(state);
}
