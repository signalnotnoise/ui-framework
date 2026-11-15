using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace FocusComparison;

internal static class Program
{
    private static string output = "";
    private static bool focus;
    private static int sequence;
    private static int verifiedFocus;
    private static int activations;
    private static int survivors;
    private static Exception? failure;

    [STAThread]
    private static int Main(string[] args)
    {
        output = Path.GetFullPath(args[0]);
        focus = args[1] == "focus";
        if (Directory.Exists(output)) throw new IOException("Use a fresh output directory.");
        Directory.CreateDirectory(output);
        Save("process-start");
        var thread = new Thread(Run) { Name = "Focus comparison STA" };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start(); thread.Join();
        Save("thread-exit");
        var assemblies = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetName().Name).Order().ToArray();
        if (ContentFactory.Kind == "plain-wpf" && assemblies.Any(a => a!.StartsWith("UI Framework")))
            failure = new InvalidOperationException("Plain control loaded framework assemblies.");
        File.WriteAllText(Path.Combine(output, "result.json"), JsonSerializer.Serialize(new {
            Kind = ContentFactory.Kind, Focus = focus, Windows = 30, FocusRoundsPerWindow = 3,
            VerifiedFocus = verifiedFocus, Activations = activations, Survivors = survivors,
            Passed = failure is null && survivors == 0 && (focus ? verifiedFocus == 90 : activations == 0),
            Error = failure?.ToString(), Runtime = Environment.Version.ToString(), Assemblies = assemblies,
            ReleaseGate = false }, new JsonSerializerOptions { WriteIndented = true }));
        return failure is null && survivors == 0 && (focus ? verifiedFocus == 90 : activations == 0) ? 0 : 1;
    }

    private static void Run()
    {
        try
        {
            var app = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
            app.Dispatcher.InvokeAsync(async () =>
            {
                try
                {
                    var anchor = new Window { Title = $"{ContentFactory.Kind}: focus comparison anchor", Width = 320, Height = 100, Left = 100, Top = 100, ShowActivated = false };
                    anchor.Activated += (_, _) => activations++;
                    anchor.Show(); await Flush(); Save("anchor-unfocused");
                    if (focus)
                    {
                        anchor.Activate();
                        Console.WriteLine("Waiting for initial activation: " + anchor.Title);
                        for (var wait = 0; !anchor.IsActive && wait < 180; wait++) await Task.Delay(500);
                        Require(anchor.IsActive, "Initial activation unavailable; manual title-bar activation required.");
                    }
                    Save("anchor-ready");
                    for (var cycle = 0; cycle < 30; cycle++)
                    {
                        var weak = await Cycle(anchor, cycle);
                        await Flush();
                        GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect(); await Flush();
                        var alive = weak.Count(w => w.IsAlive); survivors += alive;
                        Save("cycle-closed", cycle, alive);
                    }
                    anchor.Close(); await Flush();
                    GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
                    Save("windows-closed");
                }
                catch (Exception error) { failure = error; }
                finally { app.Shutdown(); }
            });
            app.Run(); Save("dispatcher-exit");
        }
        catch (Exception error) { failure = error; }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static async Task<WeakReference[]> Cycle(Window anchor, int cycle)
    {
        var content = ContentFactory.Create();
        var window = new Window { Title = $"{ContentFactory.Kind}: window {cycle + 1}/30", Width = 640, Height = 420,
            Left = 460, Top = 100, ShowActivated = false, Content = content.Root };
        window.Activated += (_, _) => activations++;
        var weak = content.Editors.Select(e => new WeakReference(e)).Append(new(window)).Append(new(content.Root)).ToArray();
        try
        {
            window.Show(); await Flush(); Save("window-open", cycle);
            for (var round = 0; round < 3; round++)
            {
                if (focus)
                {
                    if (!anchor.IsActive) anchor.Activate();
                    await Flush();
                    Require(anchor.IsActive, "Anchor did not become active.");
                    window.Activate();
                    Keyboard.Focus(content.Editors[round]); await Flush();
                    Require(window.IsActive && content.Editors[round].IsKeyboardFocused, "Editor focus not acquired.");
                    verifiedFocus++;
                }
                content.Editors[round].Text += " updated";
                await Flush(); Save("focus-round", cycle);
            }
        }
        finally
        {
            if (focus) { anchor.Activate(); Keyboard.ClearFocus(); }
            window.Content = null; content.Release(); window.Close();
        }
        return weak;
    }

    private static async Task Flush()
    {
        await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
        await Task.Delay(30);
        await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
    }
    private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Save(string phase, int cycle = -1, int alive = 0)
    {
        File.WriteAllText(Path.Combine(output, $"{sequence++:D4}-{phase}.json"), JsonSerializer.Serialize(new {
            Phase = phase, Cycle = cycle, Alive = alive, VerifiedFocus = verifiedFocus,
            Activations = activations, Resources = ResourceSnapshot.Read() }));
    }
}
