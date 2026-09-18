using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using UI_Framework;
using UI_Framework.Wpf;
using static UI_Framework.UI;

[TestClass]
public sealed class ToggleStyleTests
{
    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void LabelIndicatorAndDisabledStateUseScopedTheme(bool dark) => StaTestRunner.Run(() =>
    {
        var theme = dark ? new ThemeTokens
        {
            Canvas = "#14171D", Surface = "#20252E", Ink = "#EDF1F7", Muted = "#A8B3C4",
            Accent = "#B9D9AD", OnAccent = "#17231A", Border = "#414C5C", Focus = "#AFCBFA"
        } : new ThemeTokens();
        var value = new State<bool>(false);
        var enabled = new State<bool>(true);
        var labelText = new State<string>("Show original model responses");
        using var host = new ViewHost(() => VStack(Toggle(labelText.Value, value).IsEnabled(enabled.Value)).Foreground(theme.Ink));
        ThemeStyles.Apply(host, theme);
        TestVisualTree.Layout(host);
        var toggle = TestVisualTree.Find<CheckBox>(host).Single();
        var label = (TextBlock)toggle.Template.FindName("Label", toggle);
        var indicator = (Border)toggle.Template.FindName("Indicator", toggle);
        var mark = (System.Windows.Shapes.Path)toggle.Template.FindName("CheckMark", toggle);
        AssertColor(theme.Ink, label.Foreground);
        AssertColor(theme.Surface, indicator.Background);
        Assert.AreEqual(Visibility.Hidden, mark.Visibility);
        // Exercise the native checkbox toggle provider, preserving UI Automation behavior.
        var peer = new CheckBoxAutomationPeer(toggle);
        var provider = (IToggleProvider)peer.GetPattern(PatternInterface.Toggle);
        provider.Toggle();
        TestVisualTree.Flush();
        Assert.IsTrue(value.Value);
        Assert.AreEqual(Visibility.Visible, mark.Visibility);
        AssertColor(theme.Accent, indicator.Background);
        AssertColor(theme.OnAccent, mark.Stroke);
        labelText.Value = "Updated label";
        TestVisualTree.Flush();
        Assert.AreEqual("Updated label", label.Text);
        Assert.AreSame(toggle, TestVisualTree.Find<CheckBox>(host).Single());
        enabled.Value = false;
        TestVisualTree.Flush();
        Assert.IsFalse(toggle.IsEnabled);
        AssertColor(theme.Muted, label.Foreground);
        AssertColor(theme.Surface, indicator.Background);
        AssertColor(theme.Muted, mark.Stroke);
        Assert.AreEqual(1.0, toggle.Opacity);
        enabled.Value = true;
        value.Value = false;
        TestVisualTree.Flush();
        AssertColor(theme.Ink, label.Foreground);
        Assert.AreEqual(Visibility.Hidden, mark.Visibility);
        using var other = new ViewHost(() => Toggle("Unscoped", new State<bool>(false)));
        Assert.IsFalse(other.Resources.Contains(typeof(CheckBox)));
    });

    [TestMethod]
    public void ExplicitForegroundAndThemeReplacementUpdateRetainedLabel() => StaTestRunner.Run(() =>
    {
        var color = new State<string?>("#FFCC00");
        var value = new State<bool>(false);
        var enabled = new State<bool>(true);
        using var host = new ViewHost(() => Toggle("Custom", value).IsEnabled(enabled.Value) with { ForegroundColor = color.Value });
        ThemeStyles.Apply(host, new ThemeTokens { Ink = "#EDF1F7" });
        TestVisualTree.Layout(host);
        var toggle = TestVisualTree.Find<CheckBox>(host).Single();
        TextBlock Label() => (TextBlock)toggle.Template.FindName("Label", toggle);
        AssertColor("#FFCC00", Label().Foreground);
        enabled.Value = false;
        TestVisualTree.Flush();
        AssertColor("#FFCC00", Label().Foreground);
        color.Value = null;
        TestVisualTree.Flush();
        AssertColor(new ThemeTokens().Muted, Label().Foreground);
        enabled.Value = true;
        TestVisualTree.Flush();
        AssertColor("#EDF1F7", Label().Foreground);
        ThemeStyles.Apply(host, new ThemeTokens { Ink = "#123456" });
        TestVisualTree.Layout(host);
        Assert.AreSame(toggle, TestVisualTree.Find<CheckBox>(host).Single());
        AssertColor("#123456", Label().Foreground);
    });

    [TestMethod]
    public void KeyboardFocusUsesThemeOutlineAndKeepsLabelReadable() => StaTestRunner.Run(() =>
    {
        var theme = new ThemeTokens { Ink = "#EDF1F7", Focus = "#AFCBFA" };
        var value = new State<bool>(false);
        using var host = new ViewHost(() => Toggle("Focusable", value));
        ThemeStyles.Apply(host, theme);
        using var source = new HwndSource(new HwndSourceParameters("Toggle focus regression")
        {
            Width = 400, Height = 200, PositionX = -10000, PositionY = -10000,
            WindowStyle = unchecked((int)0x80000000)
        });
        source.RootVisual = host;
        TestVisualTree.Layout(host);
        var toggle = TestVisualTree.Find<CheckBox>(host).Single();
        // A hidden source cannot take desktop keyboard focus. Drive the read-only
        // focus state through its WPF property key to verify the actual template trigger.
        var key = (DependencyPropertyKey)typeof(UIElement).GetField("IsKeyboardFocusWithinPropertyKey",
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)!.GetValue(null)!;
        toggle.SetValue(key, true);
        AssertColor(theme.Focus, ((Border)toggle.Template.FindName("FocusChrome", toggle)).BorderBrush);
        AssertColor(theme.Ink, ((TextBlock)toggle.Template.FindName("Label", toggle)).Foreground);
        toggle.SetValue(key, false);
        Assert.AreEqual(Colors.Transparent, ((SolidColorBrush)((Border)toggle.Template.FindName("FocusChrome", toggle)).BorderBrush).Color);
    });

    private static void AssertColor(string expected, Brush actual) =>
        Assert.AreEqual((Color)ColorConverter.ConvertFromString(expected), ((SolidColorBrush)actual).Color);
}
