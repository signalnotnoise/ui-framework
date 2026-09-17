namespace UI_Framework;

/// <summary>A live read/write connection. Observation comes from state read by its getter.</summary>
public sealed class Binding<T>
{
    private readonly Computed<T> observed;
    private readonly Func<T> get;
    private readonly Action<T> set;
    private readonly IState? source;

    public Binding(Func<T> get, Action<T> set) : this(get, set, null) { }

    internal Binding(Func<T> get, Action<T> set, IState? source)
    {
        ArgumentNullException.ThrowIfNull(get);
        ArgumentNullException.ThrowIfNull(set);
        this.get = get;
        this.source = source;
        observed = source is null ? new(get) : new(get, source);
        this.set = set;
    }

    public T Value { get => observed.Value; set => set(value); }

    /// <summary>Projects a field. The setter receives the latest parent, never a captured snapshot.</summary>
    public Binding<TValue> Select<TValue>(Func<T, TValue> get, Func<T, TValue, T> set)
    {
        ArgumentNullException.ThrowIfNull(get);
        ArgumentNullException.ThrowIfNull(set);
        // Flatten projections onto the original source. Intermediate bindings do not
        // need their own subscription graph or repeated dependency capture.
        return new(() => get(this.get()), value => this.set(set(this.get(), value)), source);
    }
}
