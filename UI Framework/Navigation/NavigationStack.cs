namespace UI_Framework;

/// <summary>UI-thread-owned navigation history. Each push has a distinct screen identity.</summary>
public sealed class NavigationStack<TRoute> : ObservableState where TRoute : notnull
{
    private long nextId;
    private readonly List<NavigationEntry<TRoute>> entries = [];

    public NavigationStack(TRoute root)
    {
        ArgumentNullException.ThrowIfNull(root);
        entries.Add(new(++nextId, root));
    }

    public IReadOnlyList<NavigationEntry<TRoute>> Entries { get { Read(); return entries.ToArray(); } }
    public TRoute Current { get { Read(); return entries[^1].Route; } }
    public bool CanGoBack { get { Read(); return entries.Count > 1; } }

    public void Push(TRoute route)
    {
        VerifyAccess();
        ArgumentNullException.ThrowIfNull(route);
        entries.Add(new(++nextId, route));
        NotifyChanged();
    }

    public bool Back()
    {
        VerifyAccess();
        if (entries.Count == 1) return false;
        entries.RemoveAt(entries.Count - 1);
        NotifyChanged();
        return true;
    }

    public void PopToRoot()
    {
        VerifyAccess();
        if (entries.Count == 1) return;
        entries.RemoveRange(1, entries.Count - 1);
        NotifyChanged();
    }

    public void Reset(TRoute root)
    {
        VerifyAccess();
        ArgumentNullException.ThrowIfNull(root);
        entries.Clear();
        entries.Add(new(++nextId, root));
        NotifyChanged();
    }
}
