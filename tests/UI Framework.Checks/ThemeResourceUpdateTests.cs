using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using UI_Framework;
using UI_Framework.Wpf;

[TestClass]
public sealed class ThemeResourceUpdateTests
{
    [TestMethod]
    public void ApplyJoinsCallerInitializationAndPreservesOtherResources() => StaTestRunner.Run(() =>
    {
        var editor = new TextBox { Text = "retained" };
        var root = new StackPanel(); root.Children.Add(editor);
        var marker = new object();
        var merged = new ResourceDictionary { ["merged-marker"] = marker };
        root.Resources["local-marker"] = marker;
        root.Resources.MergedDictionaries.Add(merged);
        root.Resources.BeginInit();
        ThemeStyles.Apply(root, new ThemeTokens { Surface = "#123456" });
        // Apply must neither reject nor close an initialization owned by its caller.
        root.Resources.EndInit();
        TestVisualTree.Layout(root);
        Assert.AreEqual(Color.FromRgb(18, 52, 86), ((SolidColorBrush)editor.Background).Color);
        Assert.AreSame(marker, root.Resources["local-marker"]);
        Assert.AreSame(marker, root.Resources["merged-marker"]);
        Assert.AreSame(merged, root.Resources.MergedDictionaries.Single());
        Assert.AreEqual("retained", editor.Text);
    });

    [TestMethod]
    public void FailedStyleConstructionDoesNotLeaveOwnedInitializationOpen() => StaTestRunner.Run(() =>
    {
        var root = new StackPanel();
        Assert.ThrowsException<FormatException>(() => ThemeStyles.Apply(root, new ThemeTokens { Ink = "invalid-color" }));
        root.Resources.BeginInit();
        root.Resources.EndInit();
        ThemeStyles.Apply(root, new ThemeTokens());
        var editor = new TextBox(); root.Children.Add(editor);
        TestVisualTree.Layout(root);
        Assert.IsNotNull(editor.Template.FindName("PART_ContentHost", editor));
    });
}
