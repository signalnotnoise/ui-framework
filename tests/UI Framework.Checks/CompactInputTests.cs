using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using UI_Framework;
using UI_Framework.Wpf;
using static UI_Framework.UI;

[TestClass]
public sealed class CompactInputTests
{
    [TestMethod]
    public void CompactSearchFieldKeepsTheEntireQueryLineVisible() => StaTestRunner.Run(() =>
    {
        var query = new State<string>("Find this query");
        using var host = new ViewHost(() => HStack(TextField(query).Id("query").FontSize(16).Height(36)).Height(60).Padding(10));
        ThemeStyles.Apply(host, new ThemeTokens());
        using var source = new HwndSource(new HwndSourceParameters("Compact input check")
        { Width = 420, Height = 80, PositionX = -10000, PositionY = -10000 });
        source.RootVisual = host;
        host.Measure(new Size(420, 80));
        host.Arrange(new Rect(0, 0, 420, 80));
        host.UpdateLayout();
        TestVisualTree.Flush();
        var input = TestVisualTree.Find<TextBox>(host).Single();
        var content = (ScrollViewer)input.Template.FindName("PART_ContentHost", input);
        var character = input.GetRectFromCharacterIndex(0);
        Assert.IsFalse(character.IsEmpty);
        Assert.IsTrue(content.ViewportHeight >= character.Height,
            $"Text line {character.Height:F2} exceeds the editing viewport {content.ViewportHeight:F2}.");
        Assert.IsTrue(character.Top >= 0 && character.Bottom <= input.ActualHeight);
        var image = new RenderTargetBitmap(420, 80, 96, 96, PixelFormats.Pbgra32);
        image.Render(host);
        var folder = System.IO.Path.Combine(AppContext.BaseDirectory, "compact-input-render");
        System.IO.Directory.CreateDirectory(folder);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(image));
        using (var capture = System.IO.File.Create(System.IO.Path.Combine(folder, "search.png"))) encoder.Save(capture);
        input.Select(2, 4);
        query.Value = "Find this query again";
        TestVisualTree.Flush();
        Assert.AreSame(input, TestVisualTree.Find<TextBox>(host).Single());
        Assert.AreEqual(2, input.SelectionStart);
        input.Text = "User query";
        Assert.AreEqual("User query", query.Value);
        source.RootVisual = null;
    });

    [TestMethod]
    public void ThemedEditorRetainsItsScrollableViewportAndNativePadding() => StaTestRunner.Run(() =>
    {
        var text = new State<string>(string.Join('\n', Enumerable.Range(0, 50).Select(i => $"Line {i}")));
        using var host = new ViewHost(() => TextEditor(text).FontSize(16).Height(120));
        ThemeStyles.Apply(host, new ThemeTokens());
        TestVisualTree.Layout(host);
        var input = TestVisualTree.Find<TextBox>(host).Single();
        Assert.AreEqual(new Thickness(6), input.Padding);
        Assert.IsTrue(input.AcceptsReturn);
        var scroll = (ScrollViewer)input.Template.FindName("PART_ContentHost", input);
        Assert.IsTrue(scroll.ScrollableHeight > 0);
        input.ScrollToEnd();
        TestVisualTree.Layout(host);
        Assert.IsTrue(input.VerticalOffset > 0);
        var last = input.GetRectFromCharacterIndex(input.Text.Length - 1);
        Assert.IsFalse(last.IsEmpty);
        Assert.IsTrue(last.Top >= 0 && last.Bottom <= input.ActualHeight);
    });
}
