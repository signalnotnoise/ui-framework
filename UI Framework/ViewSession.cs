namespace UI_Framework;

public sealed class ViewSession(Func<View> body) : IDisposable
{
    private readonly int ownerThread = Environment.CurrentManagedThreadId;
    private HashSet<IState> dependencies = [];
    private bool disposed;
    public event Action? Invalidated;

    public View Build()
    {
        VerifyAccess();
        ObjectDisposedException.ThrowIf(disposed, this);
        var previous = Dependencies.Current;
        var next = new HashSet<IState>();
        Dependencies.Current = next;
        View result;
        try { result = body(); }
        finally { Dependencies.Current = previous; }
        // Preserve retained subscriptions; derived values attach only while observed.
        List<IState> added = [];
        try
        {
            foreach (var state in next.Except(dependencies))
            {
                state.Changed += Invalidate;
                added.Add(state);
            }
        }
        catch
        {
            foreach (var state in added) state.Changed -= Invalidate;
            throw;
        }
        foreach (var state in dependencies.Except(next)) state.Changed -= Invalidate;
        dependencies = next;
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
        foreach (var state in dependencies) state.Changed -= Invalidate;
        dependencies.Clear();
        Invalidated = null;
    }
}
