using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using UI_Framework.Wpf;
using static UI_Framework.UI;

namespace StressLab;

internal static class LaunchpadWindow
{
    internal static int Run(Application app, string[] args)
    {
        var model = new LaunchModel();
        using var host = new ViewHost(() => Component<Launchpad>(screen => screen.Model = model));
        ThemeStyles.Apply(host, LaunchTheme.Tokens);
        if (args.Contains("--capture"))
        {
            var width = args.Contains("--compact") ? 800 : 1280;
            host.Measure(new Size(width, double.PositiveInfinity));
            var height = (int)Math.Ceiling(host.DesiredSize.Height);
            host.Arrange(new Rect(0, 0, width, height));
            host.UpdateLayout();
            Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
            host.UpdateLayout();
            var bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
            bitmap.Render(host);
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));
            Directory.CreateDirectory("artifacts/launchpad");
            using var stream = File.Create(args.Contains("--compact") ? "artifacts/launchpad/compact.png" : "artifacts/launchpad/board.png");
            encoder.Save(stream);
            return 0;
        }
        return app.Run(new Window
        {
            Title = "Launchpad — UI Framework Showcase", Width = 1320, Height = 1000,
            MinWidth = 760, MinHeight = 600, FontFamily = new FontFamily("Segoe UI"),
            Background = new SolidColorBrush(Color.FromRgb(246, 247, 242)),
            Content = new ScrollViewer { Content = host, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto }
        });
    }
}
