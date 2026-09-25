using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Windows;
using UI_Framework;
using UI_Framework.Wpf;
using static UI_Framework.UI;

[TestClass]
public sealed class PanelReorderTests
{
    [TestMethod]
    public void ForwardMoveDoesNotDetachInterveningNativeSubtrees() => StaTestRunner.Run(() =>
    {
        var order = new State<int[]>([0, 1, 2, 3]);
        var probes = Enumerable.Range(0, 6).Select(_ => new InheritedValueProbe()).ToArray();
        using var host = new ViewHost(() => VStack(order.Value.Select(index =>
            WpfUI.Native(() => probes[index]).Id(index.ToString())).ToArray()));
        host.SetValue(InheritedValueProbe.MarkerProperty, 42);
        TestVisualTree.Layout(host);
        foreach (var probe in probes) probe.Changes = 0;
        order.Value = [1, 2, 3, 0];
        TestVisualTree.Flush();
        Assert.AreEqual(0, probes[1].Changes, "The adjacent retained row must not be detached.");
        Assert.AreEqual(0, probes[2].Changes);
        Assert.AreEqual(0, probes[3].Changes);
        foreach (var expected in new[] { new[] { 1, 2, 3, 0 }, [3, 2, 1, 0], [2, 4, 0, 3, 1], [4, 0], [0, 4, 5] })
        {
            order.Value = expected;
            TestVisualTree.Flush();
            var actual = TestVisualTree.Find<InheritedValueProbe>(host).ToArray();
            CollectionAssert.AreEqual(expected.Select(index => probes[index]).ToArray(), actual);
            foreach (var probe in actual) Assert.AreEqual(42, (int)probe.GetValue(InheritedValueProbe.MarkerProperty));
        }
    });
}
