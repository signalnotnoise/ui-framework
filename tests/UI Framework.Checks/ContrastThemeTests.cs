using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using UI_Framework;
using UI_Framework.Wpf;

[TestClass]
public sealed class ContrastThemeTests
{
    [TestMethod]
    public void TextBoxContrastFlagRespectsNestedThemeScopes() => StaTestRunner.Run(() =>
    {
        var outer = new StackPanel();
        var inner = new StackPanel();
        var outerEditor = new TextBox();
        var innerEditor = new TextBox();
        outer.Children.Add(outerEditor); outer.Children.Add(inner); inner.Children.Add(innerEditor);
        ThemeStyles.Apply(outer, new ThemeTokens());
        ThemeStyles.Apply(inner, new ThemeTokens { Surface = "#123456" });
        var outerPalette = (ThemePalette)outer.Resources[ThemePalette.ResourceKey];
        var innerPalette = (ThemePalette)inner.Resources[ThemePalette.ResourceKey];
        innerPalette.Refresh(false);
        outerPalette.Refresh(true);
        TestVisualTree.Layout(outer);
        Assert.AreEqual(SystemColors.WindowColor, ((SolidColorBrush)outerEditor.Background).Color);
        Assert.AreEqual(Color.FromRgb(18, 52, 86), ((SolidColorBrush)innerEditor.Background).Color);
        innerPalette.Refresh(true); TestVisualTree.Flush();
        Assert.AreEqual(SystemColors.WindowColor, ((SolidColorBrush)innerEditor.Background).Color);
    });

    [TestMethod]
    public void TextBoxesShareFrozenNormalBrushesAndRetainIdentityAcrossRepeatedContrastChanges() => StaTestRunner.Run(() =>
    {
        var panel = new StackPanel();
        var editors = Enumerable.Range(0, 100).Select(i => new TextBox { Text = $"Editor {i}" }).ToArray();
        foreach (var editor in editors) panel.Children.Add(editor);
        ThemeStyles.Apply(panel, new ThemeTokens());
        var palette = (ThemePalette)panel.Resources[ThemePalette.ResourceKey];
        palette.Refresh(false);
        TestVisualTree.Layout(panel);
        var background = editors[0].Background;
        Assert.IsTrue(background.IsFrozen);
        Assert.IsTrue(editors.All(editor => ReferenceEquals(editor.Background, background)));
        var template = editors[0].Template;
        var content = template.FindName("PART_ContentHost", editors[0]);
        editors[0].Select(1, 3);
        for (var i = 0; i < 3; i++)
        {
            palette.Refresh(true); TestVisualTree.Flush();
            Assert.AreEqual(SystemColors.WindowColor, ((SolidColorBrush)editors[0].Background).Color);
            Assert.AreEqual(SystemColors.HighlightColor, ((SolidColorBrush)editors[0].SelectionBrush).Color);
            Assert.AreEqual(1.0, editors[0].SelectionOpacity);
            panel.Resources[SystemColors.WindowBrushKey] = Brushes.Lime;
            TestVisualTree.Flush();
            Assert.AreSame(Brushes.Lime, editors[0].Background, "Active contrast resources must remain live.");
            panel.Resources.Remove(SystemColors.WindowBrushKey);
            TestVisualTree.Flush();
            Assert.AreSame(content, template.FindName("PART_ContentHost", editors[0]));
            Assert.AreEqual(1, editors[0].SelectionStart); Assert.AreEqual(3, editors[0].SelectionLength);
            palette.Refresh(false); TestVisualTree.Flush();
            Assert.AreSame(background, editors[0].Background);
        }
        editors[0].Foreground = Brushes.Magenta;
        editors[1].IsEnabled = false;
        palette.Refresh(true); TestVisualTree.Flush();
        Assert.AreSame(Brushes.Magenta, editors[0].Foreground);
        Assert.AreEqual(SystemColors.GrayTextColor, ((SolidColorBrush)editors[1].Foreground).Color);
    });

    [TestMethod]
    public void SystemEventRegistrationDoesNotRetainUnusedPalette() => StaTestRunner.Run(() =>
    {
        var weak = CreateReleasedPalette();
        for (var i = 0; i < 3 && weak.IsAlive; i++)
        {
            GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
        }
        Assert.IsFalse(weak.IsAlive);
    });

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    private static WeakReference CreateReleasedPalette()
    {
        var root = new StackPanel();
        ThemeStyles.Apply(root, new ThemeTokens());
        return new WeakReference(root.Resources[ThemePalette.ResourceKey]);
    }

    [TestMethod]
    public void LivePaletteChangesPreserveKeyboardFocusAndPopupSelection() => StaTestRunner.Run(() =>
    {
        var editor = new TextBox { Text = "composition owner" };
        var picker = new ComboBox { ItemsSource = new[] { "One", "Two" }, SelectedIndex = 1 };
        var panel = new StackPanel(); panel.Children.Add(editor); panel.Children.Add(picker);
        ThemeStyles.Apply(panel, new ThemeTokens());
        var window = new Window { Content = panel, Width = 400, Height = 300, ShowInTaskbar = false };
        try
        {
            window.Show(); window.Activate(); TestVisualTree.Layout(panel);
            editor.Focus(); editor.Select(1, 3);
            Assert.IsTrue(editor.IsKeyboardFocused);
            var palette = (ThemePalette)panel.Resources[ThemePalette.ResourceKey];
            palette.Refresh(true); TestVisualTree.Flush();
            Assert.IsTrue(editor.IsKeyboardFocused);
            Assert.AreEqual(1, editor.SelectionStart); Assert.AreEqual(3, editor.SelectionLength);
            picker.IsDropDownOpen = true; TestVisualTree.Flush();
            var popup = (Border)picker.Template.FindName("DropDownChrome", picker);
            Assert.AreEqual(SystemColors.WindowColor, ((SolidColorBrush)popup.Background).Color);
            var item = (ComboBoxItem)picker.ItemContainerGenerator.ContainerFromIndex(1);
            Assert.AreEqual(SystemColors.HighlightColor, ((SolidColorBrush)item.Background).Color);
            Assert.AreEqual(SystemColors.HighlightTextColor, ((SolidColorBrush)item.Foreground).Color);
            palette.Refresh(false); TestVisualTree.Flush();
            Assert.IsTrue(picker.IsDropDownOpen); Assert.AreEqual(1, picker.SelectedIndex);
        }
        finally { window.Close(); }
    });

    [TestMethod]
    public void PaletteTransitionRetainsTemplatesTextSelectionAndLocalOverrides() => StaTestRunner.Run(() =>
    {
        var panel = new StackPanel();
        var editor = new TextBox { Text = "retained text" };
        var button = new Button { Content = "Action" };
        var disabled = new Button { Content = "Disabled", IsEnabled = false };
        var toggle = new CheckBox { Content = "Checked", IsChecked = true };
        var picker = new ComboBox { ItemsSource = new[] { "One", "Two" }, SelectedIndex = 1 };
        panel.Children.Add(editor); panel.Children.Add(button); panel.Children.Add(disabled);
        panel.Children.Add(toggle); panel.Children.Add(picker);
        var theme = new ThemeTokens();
        ThemeStyles.Apply(panel, theme);
        TestVisualTree.Layout(panel);
        editor.Select(2, 4);
        var template = editor.Template;
        var content = editor.Template.FindName("PART_ContentHost", editor);
        var palette = (ThemePalette)panel.Resources[ThemePalette.ResourceKey];
        palette.Refresh(true);
        TestVisualTree.Flush();
        Assert.AreEqual(SystemColors.WindowColor, ((SolidColorBrush)editor.Background).Color);
        Assert.AreEqual(SystemColors.WindowTextColor, ((SolidColorBrush)editor.Foreground).Color);
        Assert.AreEqual(SystemColors.GrayTextColor, ((SolidColorBrush)disabled.Foreground).Color);
        Assert.AreEqual(1d, disabled.Opacity);
        Assert.AreSame(template, editor.Template);
        Assert.AreSame(content, editor.Template.FindName("PART_ContentHost", editor));
        Assert.AreEqual(2, editor.SelectionStart); Assert.AreEqual(4, editor.SelectionLength);
        Assert.AreEqual("retained text", editor.Text);
        Assert.AreEqual(1, picker.SelectedIndex); Assert.IsTrue(toggle.IsChecked);
        button.Foreground = Brushes.Magenta;
        palette.Refresh(false);
        TestVisualTree.Flush();
        Assert.AreEqual((Color)ColorConverter.ConvertFromString(theme.Surface), ((SolidColorBrush)editor.Background).Color);
        Assert.AreSame(Brushes.Magenta, button.Foreground);
        ThemeStyles.Apply(panel, theme with { Surface = "#123456" });
        palette.Refresh(true); // A queued notification for the replaced scope must be inert.
        TestVisualTree.Flush();
        Assert.AreEqual(Color.FromRgb(18, 52, 86), ((SolidColorBrush)editor.Background).Color);
    });
}
