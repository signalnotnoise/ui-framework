namespace UI_Framework;

public abstract class ObservableState : IState
{
    private readonly int ownerThread = Environment.CurrentManagedThreadId;
    public event Action? Changed;

    protected void VerifyAccess()
    {
        if (Environment.CurrentManagedThreadId != ownerThread)
            throw new InvalidOperationException("Observable state must be accessed on the thread that created it. Dispatch background results to the UI thread.");
    }

    protected void Read()
    {
        VerifyAccess();
        Dependencies.Track(this);
    }

    protected void NotifyChanged()
    {
        Dependencies.NotificationDepth++;
        try { Changed?.Invoke(); }
        finally { Dependencies.NotificationDepth--; }
    }
}
