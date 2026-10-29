using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;

namespace UI_Framework.Wpf;

// WPF consumes IList and incremental notifications. An implicit AVL tree avoids
// scanning for a row or shifting an array on every move. The renderer alone mutates it.
internal sealed class VirtualRowCollection : IList, IReadOnlyList<VirtualRow>, INotifyCollectionChanged, INotifyPropertyChanged
{
    private VirtualRow? root;
    private int version;
    private bool notifying;
    private VirtualRow? indexedNode;
    private int indexedPosition;
    private static readonly PropertyChangedEventArgs countChanged = new(nameof(Count));
    private static readonly PropertyChangedEventArgs itemChanged = new("Item[]");

    public int Count => Size(root);
    public VirtualRow this[int index] => NodeAt(index);
    public event NotifyCollectionChangedEventHandler? CollectionChanged;
    public event PropertyChangedEventHandler? PropertyChanged;

    internal int IndexOf(VirtualRow row)
    {
        if (row.Height == 0) return -1;
        var node = row;
        var index = Size(node.Left);
        while (node.Parent is { } parent)
        {
            if (ReferenceEquals(node, parent.Right)) index += Size(parent.Left) + 1;
            node = parent;
        }
        return ReferenceEquals(node, root) ? index : -1;
    }

    // Walk unchanged runs in linear time rather than performing a tree lookup
    // for every row. A mutation ends the run; the caller starts a fresh cursor.
    internal int SkipMatching(IReadOnlyList<VirtualRow> desired, int index)
    {
        if (index >= Count) return index;
        var node = NodeAt(index);
        while (node is not null && index < desired.Count && ReferenceEquals(node, desired[index]))
        {
            index++;
            node = Next(node);
        }
        return index;
    }

    internal void Insert(int index, VirtualRow row)
    {
        CheckMutation();
        if ((uint)index > (uint)Count) throw new ArgumentOutOfRangeException(nameof(index));
        if (row.Height != 0) throw new ArgumentException("Row already belongs to a collection.", nameof(row));
        row.Height = row.Count = 1;
        root = Insert(root, index, row);
        root.Parent = null;
        Changed(new(NotifyCollectionChangedAction.Add, row, index), true);
    }

    internal void RemoveAt(int index)
    {
        CheckMutation();
        ValidateIndex(index);
        root = Remove(root!, index, out var removed);
        if (root is not null) root.Parent = null;
        Detach(removed);
        Changed(new(NotifyCollectionChangedAction.Remove, removed, index), true);
    }

    internal void Move(int oldIndex, int newIndex)
    {
        CheckMutation();
        ValidateIndex(oldIndex);
        ValidateIndex(newIndex);
        if (oldIndex == newIndex) return;
        root = Remove(root!, oldIndex, out var moved);
        if (root is not null) root.Parent = null;
        Detach(moved);
        moved.Height = moved.Count = 1;
        root = Insert(root, newIndex, moved);
        root.Parent = null;
        Changed(new(NotifyCollectionChangedAction.Move, moved, newIndex, oldIndex), false);
    }

    internal void Clear()
    {
        CheckMutation();
        // A caller may retain an invalidated iterator. It may keep Current alive,
        // but must not retain the other rows through the node's tree links.
        ClearTree(root);
        root = null;
        Changed(new(NotifyCollectionChangedAction.Reset), true);
    }

    private static void ClearTree(VirtualRow? node)
    {
        if (node is null) return;
        ClearTree(node.Left);
        ClearTree(node.Right);
        Detach(node);
    }

    private static void Detach(VirtualRow node)
    {
        node.Left = node.Right = node.Parent = null;
        node.Height = node.Count = 0;
    }

    private void CheckMutation()
    {
        if (notifying) throw new InvalidOperationException("Cannot mutate virtual rows during a collection notification.");
    }

    private void Changed(NotifyCollectionChangedEventArgs change, bool count)
    {
        indexedNode = null;
        version++;
        notifying = true;
        try
        {
            if (count) PropertyChanged?.Invoke(this, countChanged);
            PropertyChanged?.Invoke(this, itemChanged);
            CollectionChanged?.Invoke(this, change);
        }
        finally { notifying = false; }
    }

    private void ValidateIndex(int index)
    {
        if ((uint)index >= (uint)Count) throw new ArgumentOutOfRangeException(nameof(index));
    }

    private VirtualRow NodeAt(int index)
    {
        ValidateIndex(index);
        // WPF's variable-height extent calculation reads consecutive indices for
        // each notification. Walk that run instead of searching the tree anew.
        if (indexedNode is not null)
        {
            if (index == indexedPosition) return indexedNode;
            if (index == indexedPosition + 1)
            {
                indexedPosition = index;
                return indexedNode = Next(indexedNode)!;
            }
        }
        indexedPosition = index;
        var node = root!;
        while (true)
        {
            var left = Size(node.Left);
            if (index == left) return indexedNode = node;
            if (index < left) node = node.Left!;
            else { index -= left + 1; node = node.Right!; }
        }
    }

    private static int Size(VirtualRow? node) => node?.Count ?? 0;
    private static int Height(VirtualRow? node) => node?.Height ?? 0;
    private static void Update(VirtualRow node)
    {
        node.Count = 1 + Size(node.Left) + Size(node.Right);
        node.Height = 1 + Math.Max(Height(node.Left), Height(node.Right));
        if (node.Left is not null) node.Left.Parent = node;
        if (node.Right is not null) node.Right.Parent = node;
    }

    private static VirtualRow RotateLeft(VirtualRow node)
    {
        var top = node.Right!;
        top.Parent = node.Parent;
        node.Right = top.Left;
        top.Left = node;
        Update(node);
        Update(top);
        return top;
    }

    private static VirtualRow RotateRight(VirtualRow node)
    {
        var top = node.Left!;
        top.Parent = node.Parent;
        node.Left = top.Right;
        top.Right = node;
        Update(node);
        Update(top);
        return top;
    }

    private static VirtualRow Balance(VirtualRow node)
    {
        Update(node);
        var balance = Height(node.Left) - Height(node.Right);
        if (balance > 1)
        {
            if (Height(node.Left!.Left) < Height(node.Left.Right)) node.Left = RotateLeft(node.Left);
            return RotateRight(node);
        }
        if (balance < -1)
        {
            if (Height(node.Right!.Right) < Height(node.Right.Left)) node.Right = RotateRight(node.Right);
            return RotateLeft(node);
        }
        return node;
    }

    private static VirtualRow Insert(VirtualRow? node, int index, VirtualRow inserted)
    {
        if (node is null) return inserted;
        var left = Size(node.Left);
        if (index <= left) node.Left = Insert(node.Left, index, inserted);
        else node.Right = Insert(node.Right, index - left - 1, inserted);
        return Balance(node);
    }

    private static VirtualRow? RemoveFirst(VirtualRow node, out VirtualRow first)
    {
        if (node.Left is null) { first = node; return node.Right; }
        node.Left = RemoveFirst(node.Left, out first);
        return Balance(node);
    }

    private static VirtualRow? Remove(VirtualRow node, int index, out VirtualRow removed)
    {
        var left = Size(node.Left);
        if (index < left) node.Left = Remove(node.Left!, index, out removed);
        else if (index > left) node.Right = Remove(node.Right!, index - left - 1, out removed);
        else
        {
            removed = node;
            if (node.Left is null) return node.Right;
            if (node.Right is null) return node.Left;
            var right = RemoveFirst(node.Right, out var successor);
            successor.Left = node.Left;
            successor.Right = right;
            successor.Parent = node.Parent;
            return Balance(successor);
        }
        return Balance(node);
    }

    public IEnumerator<VirtualRow> GetEnumerator() => Enumerate(version);
    private IEnumerator<VirtualRow> Enumerate(int expectedVersion)
    {
        var node = root;
        if (node is not null) while (node.Left is not null) node = node.Left;
        while (node is not null)
        {
            if (version != expectedVersion) throw new InvalidOperationException("Virtual rows changed during enumeration.");
            yield return node;
            if (version != expectedVersion) throw new InvalidOperationException("Virtual rows changed during enumeration.");
            node = Next(node);
        }
    }

    private static VirtualRow? Next(VirtualRow node)
    {
        if (node.Right is { } right)
        {
            node = right;
            while (node.Left is not null) node = node.Left;
            return node;
        }
        while (node.Parent is { } parent && ReferenceEquals(parent.Right, node)) node = parent;
        return node.Parent;
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    object? IList.this[int index] { get => this[index]; set => throw new NotSupportedException(); }
    bool IList.IsReadOnly => true;
    bool IList.IsFixedSize => false;
    bool ICollection.IsSynchronized => false;
    object ICollection.SyncRoot => this;
    int IList.IndexOf(object? value) => value is VirtualRow row ? IndexOf(row) : -1;
    bool IList.Contains(object? value) => value is VirtualRow row && IndexOf(row) >= 0;
    int IList.Add(object? value) => throw new NotSupportedException();
    void IList.Insert(int index, object? value) => throw new NotSupportedException();
    void IList.Remove(object? value) => throw new NotSupportedException();
    void IList.RemoveAt(int index) => throw new NotSupportedException();
    void IList.Clear() => throw new NotSupportedException();
    void ICollection.CopyTo(Array array, int index)
    {
        ArgumentNullException.ThrowIfNull(array);
        if (array.Rank != 1 || array.GetLowerBound(0) != 0) throw new ArgumentException("Use a zero-based one-dimensional array.", nameof(array));
        if (index < 0 || index > array.Length - Count) throw new ArgumentOutOfRangeException(nameof(index));
        foreach (var row in this) array.SetValue(row, index++);
    }
}
