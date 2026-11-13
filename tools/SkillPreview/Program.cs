using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using UI_Framework.Wpf;
using static UI_Framework.UI;

namespace SkillPreview;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        var output = Path.GetFullPath(args.Length > 0 ? args[0] : "artifacts/skill-preview");
        Directory.CreateDirectory(output);
        var app = new Application();
        using var cleanup = new WpfComCleanupPolicy(app.Dispatcher, error => Console.Error.WriteLine(error));
        foreach (var width in new[] { 1040, 640 })
        foreach (var polished in new[] { false, true })
        {
            using var host = new ViewHost(() => Component<ReleaseScreen>(screen => screen.Polished = polished));
            host.FontFamily = new FontFamily("Segoe UI");
            if (polished) ThemeStyles.Apply(host, ReleaseScreen.Tokens);
            host.Measure(new Size(width, double.PositiveInfinity));
            var height = Math.Max(760, (int)Math.Ceiling(host.DesiredSize.Height));
            host.Arrange(new Rect(0, 0, width, height));
            host.UpdateLayout();
            app.Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
            host.UpdateLayout();
            var bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
            bitmap.Render(host);
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));
            var path = Path.Combine(output, $"{(polished ? "after" : "before")}-{width}.png");
            using var stream = File.Create(path);
            encoder.Save(stream);
            Console.WriteLine($"Captured {path} ({width} x {height}, 96 DPI)");
        }
    }
}
