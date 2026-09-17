using System.Collections;

namespace UI_Framework;

/// <summary>Tracks reads and structural mutations. Item properties still need their own State values.</summary>
public sealed class StateList<T> : ObservableState, IReadOnlyList<T>
{
    private readonly List<T> items;

    public StateList() => items = [];
    public StateList(IEnumerable<T> initial) => items = new(initial);

    public int Count { get { Read(); return items.Count; } }
    public T this[int index]
    {
        get { Read(); return items[index]; }
        set
        {
            VerifyAccess();
            if (EqualityComparer<T>.Default.Equals(items[index], value)) return;
            items[index] = value;
            NotifyChanged();
        }
    }

    public void Add(T item)
    {
        VerifyAccess();
        items.Add(item);
        NotifyChanged();
    }

    public bool Remove(T item)
    {
        VerifyAccess();
        if (!items.Remove(item)) return false;
        NotifyChanged();
        return true;
    }

    public void RemoveAt(int index)
    {
        VerifyAccess();
        items.RemoveAt(index);
        NotifyChanged();
    }

    public void Move(int from, int to)
    {
        VerifyAccess();
        if ((uint)from >= (uint)items.Count) throw new ArgumentOutOfRangeException(nameof(from));
        if ((uint)to >= (uint)items.Count) throw new ArgumentOutOfRangeException(nameof(to));
        if (from == to) return;
        var item = items[from];
        items.RemoveAt(from);
        items.Insert(to, item);
        NotifyChanged();
    }

    public void Clear()
    {
        VerifyAccess();
        if (items.Count == 0) return;
        items.Clear();
        NotifyChanged();
    }

    /// <summary>Atomically replaces the sequence and notifies once; identities belong to item keys.</summary>
    public void ReplaceAll(IEnumerable<T> values)
    {
        VerifyAccess();
        ArgumentNullException.ThrowIfNull(values);
        var next = values.ToArray(); // Enumerate before changing anything, including self-enumeration.
        if (items.SequenceEqual(next)) return;
        items.Clear();
        items.AddRange(next);
        NotifyChanged();
    }

    // Enumerate a snapshot so an event callback cannot invalidate an active enumerator.
    public IEnumerator<T> GetEnumerator()
    {
        Read();
        return ((IEnumerable<T>)items.ToArray()).GetEnumerator();
    }
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
