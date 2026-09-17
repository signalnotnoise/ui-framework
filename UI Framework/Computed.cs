namespace UI_Framework;

/// <summary>A derived value that notifies readers only when its result changes.</summary>
public sealed class Computed<T> : IState
{
    // Diagnostic reference mode, set before types initialize. Normal applications leave this off.
    private static readonly bool unfiltered = AppContext.TryGetSwitch("UI_Framework.UnfilteredObservation", out var enabled) && enabled;
    private readonly Func<T> get;
    private readonly IEqualityComparer<T> comparer;
    private readonly IState? fixedSource;
    private readonly int ownerThread = Environment.CurrentManagedThreadId;
    private HashSet<IState> dependencies = [];
    private Action? changed;
    private T current = default!;
    private bool evaluating;

    public Computed(Func<T> get, IEqualityComparer<T>? comparer = null)
    {
        ArgumentNullException.ThrowIfNull(get);
        this.get = get;
        this.comparer = comparer ?? EqualityComparer<T>.Default;
    }

    // Bindings/selectors originating from one State already know their source;
    // avoid building and diffing a dependency set on each read or notification.
    internal Computed(Func<T> get, IState source) : this(get) => fixedSource = source;

    public T Value
    {
        get
        {
            VerifyAccess();
            if (unfiltered)
            {
                if (evaluating) throw new InvalidOperationException("A computed value cannot depend on itself.");
                evaluating = true;
                try { return get(); }
                finally { evaluating = false; }
            }
            // Refresh even when observed: custom getters may read non-observable props.
            // Capture their dependencies separately so the outer session observes only us.
            var result = Evaluate();
            // A prop-driven build establishes a new displayed baseline. During a
            // source notification, keep the old baseline until our own callback:
            // another dependent may read us before that callback has run.
            if (changed is not null && Dependencies.NotificationDepth == 0) current = result;
            Dependencies.Track(this);
            return result;
        }
    }

    public event Action? Changed
    {
        add
        {
            VerifyAccess();
            if (value is null) return;
            var first = changed is null;
            changed += value;
            if (!first) return;
            try { current = Evaluate(); }
            catch { changed -= value; Detach(); throw; }
        }
        remove
        {
            VerifyAccess();
            changed -= value;
            if (changed is null) Detach();
        }
    }

    private T Evaluate()
    {
        if (evaluating) throw new InvalidOperationException("A computed value cannot depend on itself.");
        evaluating = true;
        var outer = Dependencies.Current;
        var next = fixedSource is null ? new HashSet<IState>() : null;
        Dependencies.Current = next;
        T result;
        try { result = get(); }
        finally { Dependencies.Current = outer; evaluating = false; }
        if (changed is not null)
        {
            if (fixedSource is not null)
            {
                if (dependencies.Count == 0)
                {
                    fixedSource.Changed += OnDependencyChanged;
                    dependencies.Add(fixedSource);
                }
                return result;
            }
            ArgumentNullException.ThrowIfNull(next);
            List<IState> added = [];
            try
            {
                foreach (var dependency in next.Except(dependencies))
                {
                    dependency.Changed += OnDependencyChanged;
                    added.Add(dependency);
                }
            }
            catch
            {
                foreach (var dependency in added) dependency.Changed -= OnDependencyChanged;
                throw;
            }
            foreach (var dependency in dependencies.Except(next)) dependency.Changed -= OnDependencyChanged;
            dependencies = next;
        }
        return result;
    }

    private void OnDependencyChanged()
    {
        VerifyAccess();
        if (changed is null) return;
        var previous = current;
        var next = Evaluate();
        current = next;
        if (!comparer.Equals(previous, next)) changed?.Invoke();
    }

    private void Detach()
    {
        foreach (var dependency in dependencies) dependency.Changed -= OnDependencyChanged;
        dependencies.Clear();
    }

    private void VerifyAccess()
    {
        if (Environment.CurrentManagedThreadId != ownerThread)
            throw new InvalidOperationException("Computed values must be accessed on the thread that created them.");
    }
}
