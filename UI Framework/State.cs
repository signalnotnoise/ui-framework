namespace UI_Framework;

public sealed class State<T>(T initial) : ObservableState
{
    private T value = initial;
    private Binding<T>? binding;
    public Binding<T> Binding()
    {
        VerifyAccess();
        return binding ??= new(() => Value, value => Value = value, this);
    }
    public Binding<TValue> Binding<TValue>(Func<T, TValue> get, Func<T, TValue, T> set) => Binding().Select(get, set);
    public Computed<TValue> Select<TValue>(Func<T, TValue> select)
    {
        VerifyAccess();
        ArgumentNullException.ThrowIfNull(select);
        return new(() => select(Value), this);
    }
    public T Value
    {
        get { Read(); return value; }
        set
        {
            VerifyAccess();
            if (EqualityComparer<T>.Default.Equals(this.value, value)) return;
            this.value = value;
            NotifyChanged();
        }
    }
}
