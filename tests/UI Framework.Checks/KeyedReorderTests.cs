using System.Collections;
using System.Collections.Specialized;
using System.Windows.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using UI_Framework;
using UI_Framework.Wpf;
using static UI_Framework.UI;

[TestClass]
public sealed class KeyedReorderTests
{
    [TestMethod]
    public void MembershipRejectsForeignRemovedAndDisposedRows() => StaTestRunner.Run(() =>
    {
        var ids = new[] { 0, 1, 2 };
        using var first = new ViewHost(() => VirtualList(ids.Select(id => Text(id.ToString()).Id(id.ToString())), 300));
        using var second = new ViewHost(() => VirtualList([Text("foreign").Id("0")], 300));
        var source = (IList)((ItemsControl)((Border)first.Content).Child).ItemsSource;
        var other = (IList)((ItemsControl)((Border)second.Content).Child).ItemsSource;
        var retained = source.Cast<object>().ToArray();
        Assert.AreEqual(-1, source.IndexOf(other[0]));
        Assert.IsFalse(source.Contains(other[0]));
        Assert.AreEqual(-1, other.IndexOf(retained[0]));
        ids = [2, 1];
        first.Refresh();
        Assert.AreEqual(-1, source.IndexOf(retained[0]));
        Assert.IsFalse(source.Contains(retained[0]));
        Assert.AreEqual(1, source.IndexOf(retained[1]));
        first.Dispose();
        foreach (var row in retained)
        {
            Assert.AreEqual(-1, source.IndexOf(row));
            Assert.IsFalse(source.Contains(row));
        }
        Assert.AreEqual(0, other.IndexOf(other[0]));
    });

    [DataTestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void DisposedCollectionDoesNotRetainOtherRowsThroughStaleIterator(bool removeFirst) => StaTestRunner.Run(() =>
    {
        var (rows, iterator) = CreateDisposedCollectionWithIterator(removeFirst);
        try
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            // Current may keep its own row alive, as List<T>'s enumerator does;
            // it must not retain all other rows through tree parent/sibling links.
            Assert.IsTrue(rows.Skip(1).All(row => !row.TryGetTarget(out _)));
            GC.KeepAlive(iterator);
        }
        finally { (iterator as IDisposable)?.Dispose(); }
    });

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    private static (WeakReference<object>[] Rows, IEnumerator Iterator) CreateDisposedCollectionWithIterator(bool removeFirst)
    {
        var ids = Enumerable.Range(0, 64).ToList();
        using var host = new ViewHost(() => VirtualList(ids.Select(id => Text(id.ToString()).Id(id.ToString())), 300));
        var source = (IList)((ItemsControl)((Border)host.Content).Child).ItemsSource;
        var rows = source.Cast<object>().Select(row => new WeakReference<object>(row)).ToArray();
        var iterator = source.GetEnumerator();
        Assert.IsTrue(iterator.MoveNext());
        if (removeFirst) { ids.RemoveAt(0); host.Refresh(); }
        return (rows, iterator);
    }

    [DataTestMethod]
    [DataRow(1)]
    [DataRow(257)]
    [DataRow(2048)]
    public void RepeatedPermutationsExposeConsistentIndicesEnumerationAndNotifications(int count) => StaTestRunner.Run(() =>
    {
        var ids = Enumerable.Range(0, count).ToArray();
        using var host = new ViewHost(() => VirtualList(ids.Select(id => Text(id.ToString()).Id(id.ToString())), 300));
        var control = (ItemsControl)((Border)host.Content).Child;
        var source = (IList)control.ItemsSource;
        var original = source.Cast<object>().ToArray();
        var replay = original.ToList();
        NotifyCollectionChangedEventHandler onMoved = (_, e) =>
        {
            Assert.AreEqual(NotifyCollectionChangedAction.Move, e.Action, "A permutation must not reset or replace rows.");
            var moved = replay[e.OldStartingIndex];
            Assert.AreSame(moved, e.NewItems![0]);
            replay.RemoveAt(e.OldStartingIndex);
            replay.Insert(e.NewStartingIndex, moved);
            Assert.AreEqual(replay.Count, source.Count);
            Assert.AreSame(moved, source[e.NewStartingIndex]);
        };
        ((INotifyCollectionChanged)source).CollectionChanged += onMoved;
        try
        {
            var random = new Random(7002);
            for (var step = 0; step < 60; step++)
            {
                if (step % 3 == 0) Array.Reverse(ids);
                else if (step % 3 == 1) random.Shuffle(ids);
                else ids = ids.Where((_, index) => index % 2 == 1).Concat(ids.Where((_, index) => index % 2 == 0)).ToArray();
                host.Refresh();
                var copied = new object[count];
                source.CopyTo(copied, 0);
                var enumerated = source.Cast<object>().ToArray();
                for (var i = 0; i < count; i++)
                {
                    Assert.AreSame(original[ids[i]], source[i]);
                    Assert.AreSame(source[i], copied[i]);
                    Assert.AreSame(source[i], enumerated[i]);
                    Assert.AreSame(source[i], replay[i]);
                    Assert.AreEqual(i, source.IndexOf(source[i]));
                }
            }
        }
        finally { ((INotifyCollectionChanged)source).CollectionChanged -= onMoved; }
    });

    [TestMethod]
    public void OffscreenForwardMoveRetainsCollectionVisibleInputAndSelection() => StaTestRunner.Run(() =>
    {
        var ids = new StateList<int>(Enumerable.Range(0, 1000));
        var probes = new Dictionary<int, VirtualizationProbe>();
        using var host = new ViewHost(() => VirtualList(ids.Select(id =>
            Component<VirtualizationProbe>(p => { p.Id = id; probes[id] = p; }).Id(id.ToString()).Memo(id)), 300));
        TestVisualTree.Layout(host);
        var control = (ItemsControl)((Border)host.Content).Child;
        var source = control.ItemsSource;
        var neighbor = TestVisualTree.Find<TextBox>(host).First();
        neighbor.Text = "neighbor selection";
        neighbor.Select(2, 5);
        var moves = 0; var resets = 0;
        ((INotifyCollectionChanged)source).CollectionChanged += (_, e) =>
        {
            if (e.Action == NotifyCollectionChangedAction.Move) moves++;
            if (e.Action == NotifyCollectionChangedAction.Reset) resets++;
        };
        ids.Move(900, 999);
        TestVisualTree.Layout(host);
        Assert.AreSame(source, control.ItemsSource);
        Assert.AreEqual(1, moves);
        Assert.AreEqual(0, resets);
        Assert.AreSame(neighbor, TestVisualTree.Find<TextBox>(host).First());
        Assert.AreEqual(2, neighbor.SelectionStart);
        Assert.AreEqual(5, neighbor.SelectionLength);
        Assert.AreEqual("neighbor selection", probes[0].Text.Value);
        Assert.AreEqual(1, probes[0].Mounts);
        Assert.AreEqual(0, probes[0].Unmounts);
    });

    [TestMethod]
    public void MixedInsertRemoveAndReorderPreservesRetainedRowIdentity() => StaTestRunner.Run(() =>
    {
        var ids = Enumerable.Range(0, 60).ToList();
        using var host = new ViewHost(() => VirtualList(ids.Select(id => Text(id.ToString()).Id(id.ToString())), 300));
        var control = (ItemsControl)((Border)host.Content).Child;
        var source = (IList)control.ItemsSource;
        var identities = ids.Select((id, index) => (id, row: source[index])).ToDictionary(x => x.id, x => x.row);
        var random = new Random(701);
        var nextId = 60;
        for (var step = 0; step < 200; step++)
        {
            var removeCount = random.Next(1, Math.Min(5, ids.Count) + 1);
            for (var removedIndex = 0; removedIndex < removeCount; removedIndex++)
            {
                var removed = ids[random.Next(ids.Count)];
                ids.Remove(removed);
                identities.Remove(removed);
            }
            var addCount = random.Next(1, 6);
            for (var addedIndex = 0; addedIndex < addCount; addedIndex++)
                ids.Insert(random.Next(ids.Count + 1), nextId++);
            var reordered = ids.ToArray();
            random.Shuffle(reordered);
            ids = reordered.ToList();
            host.Refresh();
            Assert.AreSame(source, control.ItemsSource);
            Assert.AreEqual(ids.Count, source.Count);
            for (var i = 0; i < ids.Count; i++)
            {
                if (identities.TryGetValue(ids[i], out var retained)) Assert.AreSame(retained, source[i]);
                else identities.Add(ids[i], source[i]);
            }
        }
        ids.Clear();
        host.Refresh();
        Assert.AreEqual(0, source.Count);
    });
}
