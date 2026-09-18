using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using UI_Framework;
using UI_Framework.Wpf;
using static UI_Framework.UI;

[TestClass]
public sealed class PickerStyleTests
{
    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void ClosedAndPopupChromeUseThemeColorsAndNativeSelection(bool dark) => StaTestRunner.Run(() =>
    {
        var theme = dark ? new ThemeTokens
        {
            Surface = "#20252E", Ink = "#EDF1F7", Muted = "#A8B3C4", Accent = "#B9D9AD",
            AccentHover = "#C8E5BE", OnAccent = "#17231A", Hover = "#2C3440", Pressed = "#394453",
            Border = "#414C5C", Focus = "#AFCBFA"
        } : new ThemeTokens();
        var index = new State<int>(0);
        using var host = new ViewHost(() => VStack(Picker(["3 rounds", "Azure OpenAI"], index)).Foreground(theme.Ink));
        ThemeStyles.Apply(host, theme);
        using var source = new HwndSource(new HwndSourceParameters("Picker theme regression")
        {
            Width = 400, Height = 200, PositionX = -10000, PositionY = -10000,
            WindowStyle = unchecked((int)0x80000000) // Hidden popup-style source: no visible test window.
        });
        source.RootVisual = host;
        TestVisualTree.Layout(host);
        var picker = TestVisualTree.Find<ComboBox>(host).Single();
        var toggle = (ToggleButton)picker.Template.FindName("DropDownToggle", picker);
        toggle.ApplyTemplate();
        var chrome = (Border)toggle.Template.FindName("Chrome", toggle);
        AssertColor(theme.Surface, chrome.Background);
        AssertColor(theme.Ink, picker.Foreground);
        var selectedText = TestVisualTree.Find<TextBlock>(picker).Single(text => text.Text == "3 rounds");
        AssertColor(theme.Ink, selectedText.Foreground);
        Assert.IsFalse(toggle.Focusable);

        // Exercise ComboBox's real keyboard handler, not a replacement selection handler.
        picker.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, source, 0, Key.Down) { RoutedEvent = Keyboard.KeyDownEvent });
        Assert.AreEqual(1, index.Value);
        Assert.AreEqual(1, picker.SelectedIndex);
        toggle.IsChecked = true;
        TestVisualTree.Flush();
        Assert.IsTrue(picker.IsDropDownOpen);
        var popup = (Popup)picker.Template.FindName("PART_Popup", picker);
        try
        {
            Assert.IsTrue(popup.IsOpen);
            Assert.AreSame(picker, popup.PlacementTarget);
            var popupChrome = (Border)popup.Child;
            TestVisualTree.Layout(popupChrome);
            AssertColor(theme.Surface, popupChrome.Background);
            AssertColor(theme.Pressed, chrome.Background);
            var first = (ComboBoxItem)picker.ItemContainerGenerator.ContainerFromIndex(0);
            var second = (ComboBoxItem)picker.ItemContainerGenerator.ContainerFromIndex(1);
            Assert.IsNotNull(first);
            Assert.IsNotNull(second);
            first.ApplyTemplate(); second.ApplyTemplate();
            AssertColor(first.IsHighlighted ? theme.Hover : theme.Surface, first.Background);
            AssertColor(theme.Ink, first.Foreground);
            AssertColor(second.IsHighlighted ? theme.AccentHover : theme.Accent, second.Background);
            AssertColor(theme.OnAccent, second.Foreground);
            AssertColor(second.IsHighlighted ? theme.AccentHover : theme.Accent,
                ((Border)second.Template.FindName("ItemChrome", second)).Background);
            // Native item selection still reaches the binding.
            first.IsSelected = true;
            TestVisualTree.Flush();
            Assert.AreEqual(0, index.Value);
            AssertColor(theme.OnAccent, first.Foreground);
            AssertColor(theme.Ink, second.Foreground);
            picker.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, source, 0, Key.Escape) { RoutedEvent = Keyboard.KeyDownEvent });
            TestVisualTree.Flush();
            Assert.IsFalse(picker.IsDropDownOpen);
            Assert.IsFalse(popup.IsOpen);
            Assert.IsFalse(toggle.IsChecked == true);
        }
        finally { picker.IsDropDownOpen = false; }
        picker.IsEnabled = false;
        Assert.AreEqual(0.45, picker.Opacity, 0.001);
        AssertColor(theme.Ink, picker.Foreground);
        using var other = new ViewHost(() => Picker(["unscoped"], new State<int>(0)));
        Assert.IsFalse(other.Resources.Contains(typeof(ComboBox)));
    });

    private static void AssertColor(string expected, Brush actual) =>
        Assert.AreEqual((Color)ColorConverter.ConvertFromString(expected), ((SolidColorBrush)actual).Color);
}
