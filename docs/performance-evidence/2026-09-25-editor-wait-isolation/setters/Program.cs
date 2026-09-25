using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        if (args.Length != 2 || args[0] is not ("normal" or "no-caret" or "fixed-options" or "ime-disabled"))
            throw new ArgumentException("Usage: EditorWaitDiagnostics <normal|no-caret|fixed-options|ime-disabled> <report.json>");
        var mode = args[0];
        var app = new Application();
        app.Dispatcher.InvokeAsync(async () =>
        {
            try
            {
                var editors = Enumerable.Range(0, 1000).Select(i => new TextBox
                {
                    Text = $"Editor {i}: revision 0", Height = 60, AcceptsReturn = true,
                    MaxLength = 2000, UndoLimit = 20, TextWrapping = TextWrapping.Wrap
                }).ToArray();
                var grid = new UniformGrid { Columns = 5 };
                foreach (var editor in editors)
                {
                    // Diagnostic subtraction only: never proposed as production behavior.
                    if (mode == "ime-disabled") InputMethod.SetIsInputMethodEnabled(editor, false);
                    grid.Children.Add(editor);
                }
                var host = new ScrollViewer { Content = grid, Height = 1080 };
                using var source = new HwndSource(new HwndSourceParameters("Native editor wait diagnosis")
                {
                    Width = 1320, Height = 1080, PositionX = -10000, PositionY = -10000,
                    WindowStyle = unchecked((int)0x80000000)
                });
                source.RootVisual = host;
                await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
                host.Measure(new Size(1320, 1080));
                host.Arrange(new Rect(0, 0, 1320, 1080));
                host.UpdateLayout();
                var durations = new List<double>();
                var watch = Stopwatch.StartNew();
                var allocated = GC.GetAllocatedBytesForCurrentThread();
                var textWrites = 0;
                for (var step = 1; step <= 50; step++)
                {
                    var stepStart = Stopwatch.GetTimestamp();
                    double textMilliseconds = 0, caretMilliseconds = 0, optionMilliseconds = 0;
                    Console.WriteLine($"Step {step} start: {watch.ElapsedMilliseconds} ms");
                    for (var i = 0; i < editors.Length; i++)
                    {
                        var editor = editors[i];
                        var optionsStart = Stopwatch.GetTimestamp();
                        if (mode != "fixed-options")
                        {
                            editor.IsReadOnly = i % 5 == 0 && step % 2 == 1;
                            var limit = editor.IsReadOnly ? 0 : 20;
                            if (editor.UndoLimit != limit) editor.UndoLimit = limit;
                            if (editor.IsUndoEnabled != (limit > 0)) editor.IsUndoEnabled = limit > 0;
                        }
                        optionMilliseconds += Stopwatch.GetElapsedTime(optionsStart).TotalMilliseconds;
                        var text = $"Editor {i}: revision {step / 5}";
                        if (editor.Text != text)
                        {
                            var caretStart = Stopwatch.GetTimestamp();
                            var caret = mode == "no-caret" ? 0 : editor.SelectionStart;
                            caretMilliseconds += Stopwatch.GetElapsedTime(caretStart).TotalMilliseconds;
                            var started = Stopwatch.GetTimestamp();
                            editor.Text = text;
                            var elapsed = Stopwatch.GetElapsedTime(started).TotalMilliseconds;
                            textMilliseconds += elapsed;
                            if (elapsed > 100) Console.WriteLine($"Slow text setter: step {step}, editor {i}, {elapsed:F1} ms");
                            if (mode != "no-caret")
                            {
                                caretStart = Stopwatch.GetTimestamp();
                                var desired = Math.Min(caret, editor.Text.Length);
                                if (editor.SelectionStart != desired) editor.SelectionStart = desired;
                                caretMilliseconds += Stopwatch.GetElapsedTime(caretStart).TotalMilliseconds;
                            }
                            textWrites++;
                        }
                    }
                    var settersEnd = Stopwatch.GetTimestamp();
                    Console.WriteLine($"Step {step} setters done: {watch.ElapsedMilliseconds} ms; text {textMilliseconds:F1}; caret {caretMilliseconds:F1}; options {optionMilliseconds:F1}");
                    await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
                    var dispatcherEnd = Stopwatch.GetTimestamp();
                    Console.WriteLine($"Step {step} dispatcher resumed: {watch.ElapsedMilliseconds} ms");
                    var width = step % 2 == 0 ? 1320 : 1040;
                    grid.Columns = step % 2 == 0 ? 5 : 4;
                    host.Measure(new Size(width, 1080));
                    host.Arrange(new Rect(0, 0, width, 1080));
                    host.UpdateLayout();
                    durations.Add(Stopwatch.GetElapsedTime(stepStart).TotalMilliseconds);
                    Console.WriteLine($"Step {step} done: {watch.ElapsedMilliseconds} ms; setters {Stopwatch.GetElapsedTime(stepStart, settersEnd).TotalMilliseconds:F1}; dispatcher {Stopwatch.GetElapsedTime(settersEnd, dispatcherEnd).TotalMilliseconds:F1}; layout {Stopwatch.GetElapsedTime(dispatcherEnd).TotalMilliseconds:F1}");
                }
                watch.Stop();
                var bytes = GC.GetAllocatedBytesForCurrentThread() - allocated;
                if (textWrites != 10000 || editors.Where((editor, index) => editor.Text != $"Editor {index}: revision 10").Any())
                    throw new InvalidOperationException("Diagnostic text workload did not complete.");
                File.WriteAllText(args[1], JsonSerializer.Serialize(new
                {
                    Mode = mode, Rows = 1000, Operations = 50, TextWrites = textWrites,
                    Milliseconds = watch.Elapsed.TotalMilliseconds, AllocatedBytes = bytes,
                    StepMilliseconds = durations, Runtime = Environment.Version.ToString(),
                    DiagnosticOnly = true
                }, new JsonSerializerOptions { WriteIndented = true }));
                app.Shutdown();
            }
            catch (Exception error) { Console.Error.WriteLine(error); app.Shutdown(1); }
        });
        return app.Run();
    }
}
