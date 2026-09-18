using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using UI_Framework.Wpf;
using static UI_Framework.UI;

namespace StressLab;

internal static class LaunchNavigationChecks
{
    internal static int Run(Application app)
    {
        var model = new LaunchModel();
        using var host = new ViewHost(() => Component<Launchpad>(screen => screen.Model = model));
        ThemeStyles.Apply(host, LaunchTheme.Tokens);
        var scroll = new ScrollViewer { Content = host, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled };
        var window = new Window { Title = "Launchpad navigation stress", Content = scroll, Width = 1280, Height = 950 };
        var step = 0;
        var checks = 0;
        Exception? failure = null;
        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(80) };
        void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); checks++; }
        void Flush() { host.Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle); window.UpdateLayout(); }
        void Click(string label)
        {
            Find<Button>(host).Single(button => Equals(button.Content, label)).RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Button.ClickEvent));
            Flush();
        }
        timer.Tick += (_, _) =>
        {
            try
            {
                var item = model.Items[0];
                switch (step % 8)
                {
                    case 0:
                        Click("Board");
                        Check(model.Navigation.Current.Screen == LaunchScreen.Board, "Board navigation failed.");
                        window.Width = step % 16 == 0 ? 800 : 1380;
                        break;
                    case 1:
                        Find<CheckBox>(host).Single(box => Equals(box.Content, "Show shipped")).IsChecked = false;
                        Find<TextBox>(host).First().Text = "Maya";
                        Find<CheckBox>(host).Single(box => Equals(box.Content, "Reduce motion")).IsChecked = step % 16 == 9;
                        Flush();
                        Check(model.Query.Value == "Maya", "Board filter did not bind.");
                        break;
                    case 2:
                        Click(item.Title.Value);
                        Check(model.Navigation.Current.ItemId == item.Id, "Card opened wrong details.");
                        Check(model.ReduceMotion.Value || !SystemParameters.ClientAreaAnimation || Find<ViewHost>(host).Any(view => view.HasAnimatedProperties), "Expected entry animation.");
                        break;
                    case 3:
                        Find<TextBox>(host).First().Text = $"Navigation edit {step}";
                        Flush();
                        Check(item.Title.Value == $"Navigation edit {step}", "Details edit did not reach shared model.");
                        if (step == 11) Capture(scroll, "details");
                        break;
                    case 4:
                        Click("← Back");
                        Check(Find<CheckBox>(host).Single(box => Equals(box.Content, "Show shipped")).IsChecked == false, "Back lost component-local preference.");
                        Check(Find<TextBox>(host).First().Text == "Maya", "Back lost filter.");
                        Check(Find<Button>(host).Any(button => Equals(button.Content, item.Title.Value)), "Updated card missing on return.");
                        break;
                    case 5:
                        Click(item.Title.Value);
                        Click("Board"); // Interrupt any running transition immediately.
                        Check(model.Navigation.Current.Screen == LaunchScreen.Board, "Rapid transition left the wrong screen.");
                        Find<CheckBox>(host).Single(box => Equals(box.Content, "Reduce motion")).IsChecked = true;
                        Flush();
                        Check(Find<ViewHost>(host).All(view => !view.HasAnimatedProperties), "Reduced motion left an opacity animation running.");
                        if (step == 13) Capture(scroll, "board");
                        break;
                    case 6:
                        Click("Overview");
                        Check(!model.Navigation.CanGoBack, "Overview did not return to root.");
                        Check(!Find<Button>(host).Single(button => Equals(button.Content, "← Back")).IsEnabled, "Root Back button remains enabled.");
                        if (step == 14) Capture(scroll, "overview");
                        break;
                    case 7:
                        Click("Board");
                        Check(Find<CheckBox>(host).Single(box => Equals(box.Content, "Show shipped")).IsChecked == true, "Popped board reused discarded local state.");
                        break;
                }
                step++;
                window.Title = $"Launchpad navigation stress — {step}/64";
                if (step == 64) { timer.Stop(); window.Close(); }
            }
            catch (Exception error) { failure = error; timer.Stop(); window.Close(); }
        };
        window.Loaded += (_, _) => timer.Start();
        try { app.Run(window); }
        finally { timer.Stop(); }
        Console.WriteLine($"Navigation visual stress: {step}/64 steps, {checks} assertions. {failure}");
        return failure is null && step == 64 ? 0 : 1;
    }

    private static void Capture(FrameworkElement element, string name)
    {
        var bitmap = new RenderTargetBitmap((int)Math.Ceiling(element.ActualWidth), (int)Math.Ceiling(element.ActualHeight), 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(element);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        Directory.CreateDirectory("artifacts/navigation-stress");
        using var stream = File.Create($"artifacts/navigation-stress/{name}.png");
        encoder.Save(stream);
    }

    private static IEnumerable<T> Find<T>(DependencyObject root) where T : DependencyObject
    {
        if (root is T match) yield return match;
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
            foreach (var child in Find<T>(VisualTreeHelper.GetChild(root, i))) yield return child;
    }
}
