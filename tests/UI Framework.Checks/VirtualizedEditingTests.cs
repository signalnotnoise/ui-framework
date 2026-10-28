using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using UI_Framework;
using UI_Framework.Wpf;
using static UI_Framework.UI;

[TestClass]
public sealed class VirtualizedEditingTests
{
    [DataTestMethod]
    [DataRow("regenerate")]
    [DataRow("remove")]
    [DataRow("dispose")]
    public void CompositionFinishesOnOriginalBindingBeforeDetachment(string operation) => StaTestRunner.Run(() =>
    {
        var states = Enumerable.Range(0, 1000).Select(i => new State<string>($"row {i}")).ToArray();
        var ids = new StateList<int>(Enumerable.Range(0, 1000));
        using var host = new ViewHost(() => VirtualList(ids.Select(id => TextField(states[id]).Height(40).Id(id.ToString())), 180));
        var window = new Window { Content = host, Width = 500, Height = 300, ShowInTaskbar = false };
        try
        {
            window.Show();
            TestVisualTree.Layout(host);
            var original = TestVisualTree.Find<TextBox>(host).First();
            Assert.IsTrue(original.Focus());
            original.SelectAll();
            var composition = new TrackedTextComposition(original);
            TextCompositionManager.StartComposition(composition);
            composition.Update("確定");
            composition.BeforeComplete = () => Assert.IsTrue(original.IsLoaded, "Complete while the original editor is attached.");
            if (operation == "regenerate") TestVisualTree.Find<ItemsControl>(host).Single().Items.Refresh();
            else if (operation == "remove") { ids.Remove(0); host.Refresh(); }
            else host.Dispose();
            TestVisualTree.Layout(host);
            Assert.AreEqual(1, composition.Completions, "Use the provider's virtual completion method exactly once.");
            Assert.AreEqual("確定", states[0].Value);
            Assert.AreEqual("row 1", states[1].Value);
            original.Text = "late detached write";
            original.RaiseEvent(new TextCompositionEventArgs(Keyboard.PrimaryDevice,
                new TextComposition(InputManager.Current, original, "late")) { RoutedEvent = TextCompositionManager.TextInputEvent });
            Assert.AreEqual("確定", states[0].Value);
            Assert.AreEqual("row 1", states[1].Value);
            if (operation == "regenerate")
            {
                var replacement = TestVisualTree.Find<TextBox>(host).First();
                Assert.AreNotSame(original, replacement);
                replacement.Text = "new editor";
                Assert.AreEqual("new editor", states[0].Value);
            }
        }
        finally { window.Close(); }
    });

    [DataTestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void CompletedOrCanceledCompositionIsNotCompletedAgain(bool cancel) => StaTestRunner.Run(() =>
    {
        var state = new State<string>("original");
        using var host = new ViewHost(() => VirtualList([TextField(state).Id("row")], 180));
        var window = new Window { Content = host, Width = 500, Height = 300, ShowInTaskbar = false };
        try
        {
            window.Show();
            TestVisualTree.Layout(host);
            var input = TestVisualTree.Find<TextBox>(host).Single();
            Assert.IsTrue(input.Focus());
            input.SelectAll();
            var composition = new TrackedTextComposition(input);
            TextCompositionManager.StartComposition(composition);
            composition.Update("中間");
            composition.Update(cancel ? "" : "確定");
            composition.Complete();
            var accepted = state.Value;
            TestVisualTree.Find<ItemsControl>(host).Single().Items.Refresh();
            TestVisualTree.Layout(host);
            Assert.AreEqual(1, composition.Completions);
            Assert.AreEqual(cancel ? "original" : "確定", accepted);
            Assert.AreEqual(accepted, state.Value);
        }
        finally { window.Close(); }
    });

    [TestMethod]
    public void ActualScrollingRecyclesEditorsWithoutRedirectingLateInput() => StaTestRunner.Run(() =>
    {
        var states = Enumerable.Range(0, 1000).Select(i => new State<string>($"row {i}")).ToArray();
        using var host = new ViewHost(() => VStack(TextField(new State<string>("outside")),
            VirtualList(states.Select((state, id) => TextField(state).Height(40).Id(id.ToString())), 180)));
        var window = new Window { Content = host, Width = 500, Height = 300, ShowInTaskbar = false };
        try
        {
            window.Show();
            TestVisualTree.Layout(host);
            var list = TestVisualTree.Find<VirtualListControl>(host).Single();
            var original = TestVisualTree.Find<TextBox>(list).First();
            Assert.IsTrue(original.Focus());
            original.Select(1, 2);
            var scroll = TestVisualTree.Find<ScrollViewer>(list).First();
            scroll.ScrollToEnd();
            TestVisualTree.Layout(host);
            // WPF may retain a focused container. Moving focus permits normal cleanup.
            var outside = TestVisualTree.Find<TextBox>(host).First();
            Assert.IsTrue(outside.Focus());
            scroll.ScrollToHome();
            TestVisualTree.Layout(host);
            scroll.ScrollToEnd();
            TestVisualTree.Layout(host);
            Assert.IsFalse(TestVisualTree.Find<TextBox>(list).Contains(original), "Require actual viewport recycling.");
            Assert.IsTrue(TestVisualTree.Find<TextBox>(list).Count() < 40);
            original.Text = "stale";
            Assert.AreEqual("row 0", states[0].Value);
            Assert.AreEqual("row 999", states[999].Value);
            var last = TestVisualTree.Find<TextBox>(list).Single(input => input.Text == "row 999");
            last.Text = "last edited";
            Assert.AreEqual("last edited", states[999].Value);
            scroll.ScrollToHome();
            TestVisualTree.Layout(host);
            Assert.AreSame(outside, Keyboard.FocusedElement, "Returning a previously focused row must not steal newer focus.");
            Assert.AreNotSame(original, TestVisualTree.Find<TextBox>(list).First());
        }
        finally { window.Close(); }
    });

    [TestMethod]
    public void FocusedCompositionSurvivesScrollingAndReorderOrFinishesBeforeRecycling() => StaTestRunner.Run(() =>
    {
        var states = Enumerable.Range(0, 1000).Select(i => new State<string>($"row {i}")).ToArray();
        var ids = new StateList<int>(Enumerable.Range(0, 1000));
        using var host = new ViewHost(() => VirtualList(ids.Select(id => TextField(states[id]).Height(40).Id(id.ToString())), 180));
        var window = new Window { Content = host, Width = 500, Height = 300, ShowInTaskbar = false };
        try
        {
            window.Show();
            TestVisualTree.Layout(host);
            var input = TestVisualTree.Find<TextBox>(host).First();
            Assert.IsTrue(input.Focus());
            input.SelectAll();
            var composition = new TrackedTextComposition(input);
            TextCompositionManager.StartComposition(composition);
            composition.Update("確定");
            composition.BeforeComplete = () => Assert.IsNotNull(PresentationSource.FromVisual(input));
            TestVisualTree.Find<ScrollViewer>(host).First().ScrollToEnd();
            TestVisualTree.Layout(host);
            ids.Move(0, 999);
            TestVisualTree.Layout(host);
            if (composition.Completions == 0)
            {
                Assert.IsNotNull(PresentationSource.FromVisual(input), "An unfinished composition must retain its original editor.");
                composition.Complete();
            }
            Assert.AreEqual(1, composition.Completions);
            Assert.AreEqual("確定", states[0].Value);
            Assert.AreEqual("row 999", states[999].Value);
            host.Dispose();
            input.Text = "late after disposal";
            Assert.AreEqual("確定", states[0].Value);
        }
        finally { window.Close(); }
    });

    [TestMethod]
    public void RemovedRowCannotRestoreFocusIntoReinsertedKey() => StaTestRunner.Run(() =>
    {
        var ids = new StateList<int>([0, 1]);
        var states = new[] { new State<string>("zero"), new State<string>("one") };
        using var host = new ViewHost(() => VStack(TextField(new State<string>("outside")),
            VirtualList(ids.Select(id => TextField(states[id]).Id(id.ToString())), 180)));
        var window = new Window { Content = host, Width = 500, Height = 300, ShowInTaskbar = false };
        try
        {
            window.Show();
            TestVisualTree.Layout(host);
            Assert.IsTrue(TestVisualTree.Find<TextBox>(host).Single(input => input.Text == "zero").Focus());
            TestVisualTree.Find<ItemsControl>(host).Single().Items.Refresh();
            host.UpdateLayout(); // Queue restoration, then remove its owner before it runs.
            ids.Remove(0);
            host.Refresh();
            var external = TestVisualTree.Find<TextBox>(host).First();
            Assert.IsTrue(external.Focus());
            ids.ReplaceAll([0, 1]);
            host.Refresh();
            TestVisualTree.Layout(host);
            Assert.AreSame(external, Keyboard.FocusedElement);
        }
        finally { window.Close(); }
    });

    [TestMethod]
    public void CompositionProviderFailureStillDetachesEveryRow() => StaTestRunner.Run(() =>
    {
        var state = new State<string>("original");
        var released = 0;
        using var host = new ViewHost(() => VirtualList([
            TextField(state).Id("editor"),
            WpfUI.Native(() => new TextBox { Text = "native" }, release: _ => released++).Id("native")], 180));
        var window = new Window { Content = host, Width = 500, Height = 300, ShowInTaskbar = false };
        try
        {
            window.Show();
            TestVisualTree.Layout(host);
            var input = TestVisualTree.Find<TextBox>(host).First();
            Assert.IsTrue(input.Focus());
            var composition = new TrackedTextComposition(input);
            TextCompositionManager.StartComposition(composition);
            composition.BeforeComplete = () => throw new InvalidOperationException("provider failure");
            Assert.ThrowsException<AggregateException>(host.Dispose);
            Assert.AreEqual(1, released);
            Assert.AreEqual(1, composition.Completions);
            input.Text = "stale";
            Assert.AreEqual("original", state.Value);
            host.Dispose();
        }
        finally { window.Close(); }
    });

    [TestMethod]
    public void NestedRowsHaveOneCompositionOwnerEvenWhenProviderFails() => StaTestRunner.Run(() =>
    {
        var state = new State<string>("nested");
        using var host = new ViewHost(() => VirtualList([
            VirtualList([TextField(state).Id("inner")], 100).Id("outer")], 180));
        var window = new Window { Content = host, Width = 500, Height = 300, ShowInTaskbar = false };
        try
        {
            window.Show();
            TestVisualTree.Layout(host);
            var input = TestVisualTree.Find<TextBox>(host).Single();
            Assert.IsTrue(input.Focus());
            var composition = new TrackedTextComposition(input);
            TextCompositionManager.StartComposition(composition);
            composition.BeforeComplete = () => throw new InvalidOperationException("nested provider failure");
            Assert.ThrowsException<AggregateException>(host.Dispose);
            Assert.AreEqual(1, composition.Completions, "Only the innermost row owns the input provider.");
            input.Text = "late";
            Assert.AreEqual("nested", state.Value);
        }
        finally { window.Close(); }
    });

    [TestMethod]
    public void InnerRowRemovalCompletesCompositionWhileOuterRowRemains() => StaTestRunner.Run(() =>
    {
        var ids = new StateList<int>([0, 1]);
        var states = new[] { new State<string>("zero"), new State<string>("one") };
        using var host = new ViewHost(() => VirtualList([
            VirtualList(ids.Select(id => TextField(states[id]).Id(id.ToString())), 100).Id("outer")], 180));
        var window = new Window { Content = host, Width = 500, Height = 300, ShowInTaskbar = false };
        try
        {
            window.Show();
            TestVisualTree.Layout(host);
            var input = TestVisualTree.Find<TextBox>(host).First();
            Assert.IsTrue(input.Focus());
            input.SelectAll();
            var composition = new TrackedTextComposition(input);
            TextCompositionManager.StartComposition(composition);
            composition.Update("確定");
            ids.Remove(0);
            host.Refresh();
            TestVisualTree.Layout(host);
            Assert.AreEqual(1, composition.Completions);
            Assert.AreEqual("確定", states[0].Value);
            Assert.AreEqual("one", states[1].Value);
            Assert.AreEqual(2, TestVisualTree.Find<ItemsControl>(host).Count());
            host.Dispose();
            Assert.AreEqual(1, composition.Completions);
        }
        finally { window.Close(); }
    });

    [TestMethod]
    public void EndEventWithANewCompositionObjectDoesNotCompleteTheOldSnapshot() => StaTestRunner.Run(() =>
    {
        using var host = new ViewHost(() => VirtualList([TextField(new State<string>("text")).Id("row")], 180));
        var window = new Window { Content = host, Width = 500, Height = 300, ShowInTaskbar = false };
        try
        {
            window.Show();
            TestVisualTree.Layout(host);
            var input = TestVisualTree.Find<TextBox>(host).Single();
            Assert.IsTrue(input.Focus());
            var start = new TrackedTextComposition(input);
            TextCompositionManager.StartComposition(start);
            var update = new TrackedTextComposition(input);
            input.RaiseEvent(new TextCompositionEventArgs(Keyboard.PrimaryDevice, update)
                { RoutedEvent = TextCompositionManager.PreviewTextInputUpdateEvent });
            input.RaiseEvent(new TextCompositionEventArgs(Keyboard.PrimaryDevice, new TrackedTextComposition(input))
                { RoutedEvent = TextCompositionManager.PreviewTextInputEvent });
            TestVisualTree.Find<ItemsControl>(host).Single().Items.Refresh();
            TestVisualTree.Layout(host);
            Assert.AreEqual(0, start.Completions);
            Assert.AreEqual(0, update.Completions, "An end event belongs to the editor, not a stored event object's identity.");
        }
        finally { window.Close(); }
    });

    [DataTestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void FocusLossFinishesAttachedEditorBeforeCapturingSelection(bool nested) => StaTestRunner.Run(() =>
    {
        var state = new State<string>("original");
        using var host = new ViewHost(() => VirtualList([
            (nested ? VirtualList([TextField(state).Id("inner")], 100) : TextField(state)).Id("outer")], 180));
        var window = new Window { Content = host, Width = 500, Height = 300, ShowInTaskbar = false };
        try
        {
            window.Show();
            TestVisualTree.Layout(host);
            var input = TestVisualTree.Find<TextBox>(host).Single();
            Assert.IsTrue(input.Focus());
            input.SelectAll();
            var composition = new TrackedTextComposition(input);
            TextCompositionManager.StartComposition(composition);
            composition.Update("確定");
            var losingFocus = false;
            composition.BeforeComplete = () =>
            {
                Assert.IsTrue(losingFocus, "Native-shaped providers finish from focus loss, not a delayed snapshot call.");
                Assert.IsNotNull(PresentationSource.FromVisual(input));
            };
            KeyboardFocusChangedEventHandler? finish = null;
            finish = (_, _) =>
            {
                input.LostKeyboardFocus -= finish;
                losingFocus = true;
                composition.Complete();
                losingFocus = false;
            };
            input.LostKeyboardFocus += finish;
            TestVisualTree.Find<ItemsControl>(host).First().Items.Refresh();
            TestVisualTree.Layout(host);
            Assert.AreEqual(1, composition.Completions);
            Assert.AreEqual("確定", state.Value);
            var restored = TestVisualTree.Find<TextBox>(host).Single();
            Assert.AreNotSame(input, restored);
            Assert.AreSame(restored, Keyboard.FocusedElement);
            Assert.AreEqual(2, restored.SelectionStart);
            Assert.AreEqual(0, restored.SelectionLength);
        }
        finally { window.Close(); }
    });
}
