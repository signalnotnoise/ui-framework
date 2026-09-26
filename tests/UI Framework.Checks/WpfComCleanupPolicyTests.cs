using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using UI_Framework.Wpf;

namespace UI_Framework.Checks;

[TestClass]
public sealed class WpfComCleanupPolicyTests
{
    [TestMethod]
    public void FailedCleanupIsReportedAndCanRecover() => StaTestRunner.Run(() =>
    {
        var attempts = 0;
        var reported = new List<Exception>();
        using var policy = new WpfComCleanupPolicy(Dispatcher.CurrentDispatcher,
            () => { if (++attempts == 1) throw new InvalidOperationException("injected failure"); }, reported.Add);
        policy.RequestCleanup();
        Flush();
        Assert.AreEqual(1, reported.Count);
        Assert.AreEqual(1, policy.FailureCount);
        policy.RequestCleanup();
        Flush();
        Assert.IsNull(policy.LastError);
        Assert.AreEqual(1, policy.FailureCount);
    });

    [TestMethod]
    public void UpdateBoundaryPreservesEditorStateAndInputSupport() => StaTestRunner.Run(() =>
    {
        using var policy = new WpfComCleanupPolicy(Dispatcher.CurrentDispatcher, () => { });
        var editor = new TextBox { Text = "original text", IsUndoEnabled = true };
        using var source = new HwndSource(new HwndSourceParameters("Cleanup editor regression")
        {
            Width = 400,
            Height = 200,
            PositionX = -10000,
            PositionY = -10000,
            WindowStyle = unchecked((int)0x80000000)
        });
        source.RootVisual = editor;
        editor.Measure(new Size(400, 200));
        editor.Arrange(new Rect(0, 0, 400, 200));
        editor.UpdateLayout();
        editor.Select(0, 8);
        policy.RunUpdate(() =>
        {
            editor.BeginChange();
            try { editor.SelectedText = "edited"; }
            finally { editor.EndChange(); }
        });
        editor.Select(1, 3);
        Flush();
        Assert.AreEqual("edited text", editor.Text);
        Assert.AreEqual(1, editor.SelectionStart);
        Assert.AreEqual(3, editor.SelectionLength);
        Assert.IsTrue(InputMethod.GetIsInputMethodEnabled(editor));
        Assert.IsTrue(editor.CanUndo);
        policy.RunUpdate(() => editor.Undo());
        Assert.AreEqual("original text", editor.Text);
        source.RootVisual = null;
    });

    [TestMethod]
    public void RejectsSecondOwnerOnTheSameSta() => StaTestRunner.Run(() =>
    {
        using var policy = new WpfComCleanupPolicy(Dispatcher.CurrentDispatcher, () => { });
        Assert.ThrowsException<InvalidOperationException>(
            () => new WpfComCleanupPolicy(Dispatcher.CurrentDispatcher, () => { }));
    });

    private static void Flush() =>
        Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
}
