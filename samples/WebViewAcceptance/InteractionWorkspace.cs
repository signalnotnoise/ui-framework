using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace WebViewAcceptance;

internal sealed class InteractionWorkspace
{
    internal readonly BrowserSession Owner;
    internal readonly Grid Root = new() { Background = Brushes.White };
    internal readonly Grid BrowserArea = new();
    internal readonly Button Before = new() { Content = "Before browser", Height = 40 };
    internal readonly Button After = new() { Content = "After browser", Height = 40 };
    internal readonly Border Overlay = new() { Background = Brushes.Magenta, Width = 180, Height = 100,
        HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Bottom,
        Margin = new Thickness(0, 0, 0, 30), Visibility = Visibility.Collapsed };

    internal InteractionWorkspace(BrowserSession owner)
    {
        Owner = owner;
        owner.Window.Width = 820; owner.Window.Height = 600; owner.Window.Left = 80; owner.Window.Top = 80;
        owner.Window.Content = null;
        Root.RowDefinitions.Add(new() { Height = GridLength.Auto });
        Root.RowDefinitions.Add(new() { Height = new GridLength(1, GridUnitType.Star) });
        Root.RowDefinitions.Add(new() { Height = GridLength.Auto });
        KeyboardNavigation.SetTabNavigation(Root, KeyboardNavigationMode.Cycle);
        Root.Children.Add(Before);
        Grid.SetRow(BrowserArea, 1); Root.Children.Add(BrowserArea);
        Grid.SetRow(After, 2); Root.Children.Add(After);
        BrowserArea.Children.Add(owner.Host);
        Panel.SetZIndex(Overlay, 100); BrowserArea.Children.Add(Overlay);
        owner.Window.Content = Root;
        owner.Window.UpdateLayout();
    }
}
