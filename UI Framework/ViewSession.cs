namespace UI_Framework;

public sealed class ViewSession : IDisposable
{
    private readonly Func<View> body;
    private readonly int ownerThread = Environment.CurrentManagedThreadId;
    private readonly Action<IState> subscribe;
    private readonly Action<IState> unsubscribe;
    private HashSet<IState> dependencies = [];
    private bool disposed;
    public event Action? Invalidated;

    public ViewSession(Func<View> body)
    {
        ArgumentNullException.ThrowIfNull(body);
        this.body = body;
        subscribe = state => state.Changed += Invalidate;
        unsubscribe = state => state.Changed -= Invalidate;
    }

    public View Build()
    {
        var result = BuildCandidate(out var commit);
        commit();
        return result;
    }

    internal View BuildCandidate(out Action commit)
    {
        VerifyAccess();
        ObjectDisposedException.ThrowIf(disposed, this);
        var previous = Dependencies.Current;
        var next = new HashSet<IState>();
        Dependencies.Current = next;
        View result;
        try { result = body(); }
        finally { Dependencies.Current = previous; }
        commit = () =>
        {
            // Preserve retained subscriptions; derived values attach only while observed.
            Dependencies.Reconcile(dependencies, next, subscribe, unsubscribe);
            dependencies = next;
        };
        return result;
    }

    private void Invalidate() => Invalidated?.Invoke();
    private void VerifyAccess()
    {
        if (Environment.CurrentManagedThreadId != ownerThread)
            throw new InvalidOperationException("View sessions must be used on the thread that created them.");
    }
    public void Dispose()
    {
        VerifyAccess();
        if (disposed) return;
        disposed = true;
        foreach (var state in dependencies) unsubscribe(state);
        dependencies.Clear();
        Invalidated = null;
    }
}
