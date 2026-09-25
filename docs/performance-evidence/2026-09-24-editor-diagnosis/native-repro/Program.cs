using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Interop;
using System.Windows.Threading;

internal static class Program
{
    [STAThread]
    private static int Main()
    {
        var app = new Application();
        app.Dispatcher.InvokeAsync(async () =>
        {
            var watch = Stopwatch.StartNew();
            var editors = Enumerable.Range(0, 1000).Select(i => new TextBox
            {
                Text = $"Editor {i}: revision 0", Height = 60, AcceptsReturn = true,
                MaxLength = 2000, UndoLimit = 20, TextWrapping = TextWrapping.Wrap
            }).ToArray();
            var grid = new UniformGrid { Columns = 5 };
            foreach (var editor in editors) grid.Children.Add(editor);
            var host = new ScrollViewer { Content = grid, Height = 1080 };
            using var source = new HwndSource(new HwndSourceParameters("Native editor reproduction")
            { Width = 1320, Height = 1080, PositionX = -10000, PositionY = -10000, WindowStyle = unchecked((int)0x80000000) });
            source.RootVisual = host;
            for (var step = 0; step <= 50; step++)
            {
                Console.WriteLine($"Step {step}: start at {watch.ElapsedMilliseconds} ms");
                for (var i = 0; i < editors.Length; i++)
                {
                    var editor = editors[i];
                    editor.IsReadOnly = i % 5 == 0 && step % 2 == 1;
                    var limit = editor.IsReadOnly ? 0 : 20;
                    if (editor.UndoLimit != limit) editor.UndoLimit = limit;
                    if (editor.IsUndoEnabled != (limit > 0)) editor.IsUndoEnabled = limit > 0;
                    var text = $"Editor {i}: revision {step / 5}";
                    if (editor.Text != text)
                    {
                        var caret = editor.SelectionStart;
                        editor.Text = text;
                        var desiredCaret = Math.Min(caret, editor.Text.Length);
                        if (editor.SelectionStart != desiredCaret) editor.SelectionStart = desiredCaret;
                    }
                }
                await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
                var width = step % 2 == 0 ? 1320 : 1040;
                grid.Columns = step % 2 == 0 ? 5 : 4;
                host.Measure(new Size(width, 1080));
                host.Arrange(new Rect(0, 0, width, 1080));
                host.UpdateLayout();
                Console.WriteLine($"Step {step}: complete at {watch.ElapsedMilliseconds} ms");
            }
            app.Shutdown();
        });
        return app.Run();
    }
}
