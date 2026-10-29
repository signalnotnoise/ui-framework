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
