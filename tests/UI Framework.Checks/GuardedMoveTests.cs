using System.Collections;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using UI_Framework;
using UI_Framework.Wpf;
using static UI_Framework.UI;

[TestClass]
public sealed class GuardedMoveTests
{
    [DataTestMethod]
    [DataRow("inactive")]
    [DataRow("focused")]
    [DataRow("composition")]
    [DataRow("nested-composition")]
    [DataRow("inside-viewport")]
    public void ForwardMoveOnlyShortcutsInactiveRowsLeavingViewport(string mode) => StaTestRunner.Run(() =>
    {
        var ids = new StateList<int>(Enumerable.Range(0, 1000));
        var states = Enumerable.Range(0, 1000).Select(i => new State<string>($"row {i}")).ToArray();
        using var host = new ViewHost(() => VirtualList(ids.Select(id =>
            mode == "nested-composition" && id == 0
                ? VirtualList([TextField(states[id]).Height(40).Id("inner")], 80).Id(id.ToString())
                : TextField(states[id]).Height(40).Id(id.ToString())), 300));
        var window = new Window { Content = host, Width = 500, Height = 420, ShowInTaskbar = false };
        TrackedTextComposition? composition = null;
        try
        {
            window.Show();
            TestVisualTree.Layout(host);
            Keyboard.ClearFocus();
            var control = (ItemsControl)((Border)host.Content).Child;
            var source = (IList)control.ItemsSource;
            var original = source[0];
            var inputs = TestVisualTree.Find<TextBox>(host).ToArray();
            var input = inputs[0];
            var neighbor = inputs[1];
            neighbor.Select(1, 3);
            if (mode == "focused") Assert.IsTrue(input.Focus());
            if (mode.Contains("composition"))
            {
                composition = new TrackedTextComposition(input);
                TextCompositionManager.StartComposition(composition);
                composition.Update("確定");
                composition.BeforeComplete = () => Assert.IsNotNull(PresentationSource.FromVisual(input));
                Assert.IsFalse(input.IsKeyboardFocusWithin, "Exercise the composition guard independently of focus.");
            }
            var moves = 0;
            NotifyCollectionChangedEventHandler onMove = (_, e) =>
            {
                Assert.AreEqual(NotifyCollectionChangedAction.Move, e.Action);
                moves++;
            };
            ((INotifyCollectionChanged)source).CollectionChanged += onMove;
            var destination = mode == "inside-viewport" ? 2 : 999;
            try { ids.Move(0, destination); TestVisualTree.Layout(host); }
            finally { ((INotifyCollectionChanged)source).CollectionChanged -= onMove; }
            Assert.AreSame(source, control.ItemsSource);
            Assert.AreSame(original, source[destination]);
            if (mode == "inactive") Assert.AreEqual(1, moves);
            else Assert.IsTrue(moves > 1, "An active editor or realized destination must retain the conservative path.");
            if (mode == "focused")
            {
                // WPF brings the moved focused row into view; its old neighbor
                // need not remain realized at the other end of the list.
                var focused = Keyboard.FocusedElement as TextBox;
                Assert.IsNotNull(focused);
                Assert.AreEqual("row 0", focused.Text);
                focused.Text = "focused edit";
                Assert.AreEqual("focused edit", states[0].Value);
                Assert.AreEqual("row 1", states[1].Value);
                return;
            }
            if (mode == "inactive")
            {
                Assert.IsTrue(TestVisualTree.Find<TextBox>(host).Contains(neighbor));
                Assert.AreEqual(1, neighbor.SelectionStart);
                Assert.AreEqual(3, neighbor.SelectionLength);
            }
            else neighbor = TestVisualTree.Find<TextBox>(host).Single(box => box.Text == "row 1");
            neighbor.Text = "neighbor changed";
            Assert.AreEqual("neighbor changed", states[1].Value);
            if (composition is not null && composition.Completions == 0) composition.Complete();
            if (composition is not null) Assert.AreEqual(1, composition.Completions);
            if (mode == "inactive")
            {
                input.Text = "late detached edit";
                Assert.AreEqual("row 0", states[0].Value);
                TestVisualTree.Find<ScrollViewer>(host).First().ScrollToEnd();
                TestVisualTree.Layout(host);
                var returned = TestVisualTree.Find<TextBox>(host).Last();
                Assert.AreNotSame(input, returned);
                Assert.AreEqual("row 0", returned.Text);
                returned.Text = "returned edit";
                Assert.AreEqual("returned edit", states[0].Value);
                Assert.AreEqual("neighbor changed", states[1].Value);
            }
        }
        finally
        {
            try { if (composition is { Completions: 0 }) composition.Complete(); }
            finally { window.Close(); }
        }
    });
}
