using Microsoft.VisualStudio.TestTools.UnitTesting;
using UI_Framework;
using UI_Framework.Wpf;

[TestClass]
public sealed class ViewValidationTests
{
    [TestMethod]
    public void MalformedDescriptionsFailBeforeMutatingRetainedTree() => StaTestRunner.Run(() =>
    {
        var view = UI.Text("stable");
        using var host = new ViewHost(() => view);
        var original = host.Content;
        View[] invalid =
        [
            view with { Gap = -1 }, view with { Inset = double.NaN },
            view with { DesiredWidth = double.PositiveInfinity }, view with { TextSize = 0 },
            view with { Kind = (ViewKind)999 }, view with { Children = null! },
            view with { Children = [null!] }, view with { ComponentType = typeof(ThrowOnMounted) },
            view with { PlatformContent = new object() },
            new(ViewKind.AdaptiveGrid) { MinimumColumnWidth = 0 },
            new(ViewKind.AdaptiveGrid) { MinimumColumnWidth = double.NaN },
            new(ViewKind.TextEditor) { MaximumLength = -1 },
            new(ViewKind.TextEditor) { UndoHistoryLimit = -1 }
        ];
        foreach (var malformed in invalid)
        {
            view = malformed;
            Assert.ThrowsException<InvalidOperationException>(host.Refresh);
            Assert.AreSame(original, host.Content);
        }
        view = UI.Text("recovered");
        host.Refresh();
        TestVisualTree.Layout(host);
        Assert.AreEqual("recovered", TestVisualTree.Find<System.Windows.Controls.TextBlock>(host).Single().Text);
    });
}
