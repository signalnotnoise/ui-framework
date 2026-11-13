using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using UI_Framework;
using UI_Framework.Wpf;

namespace WebViewAcceptance;

internal static class InteractionChecks
{
    internal static async Task<object> Run(string mode, string profile, string output)
    {
        var owner = new BrowserSession(activate: true) { ExpectedPage = "interaction.html" };
        var workspace = new InteractionWorkspace(owner);
        try
        {
            await Probe.Await(owner.Start(profile));
            owner.Browser.CoreWebView2.Navigate(new Uri(Path.Combine(AppContext.BaseDirectory, "Pages", "interaction.html")).AbsoluteUri);
            await Probe.Await(owner.PageLoaded.Task);
            owner.Window.Activate(); await Probe.Flush();
            Console.WriteLine("Waiting up to 60 seconds for test-window activation. Do not type while checks run.");
            var activationDeadline = DateTime.UtcNow.AddSeconds(60);
            while (!KeyboardProbe.IsForeground(owner.Window) && DateTime.UtcNow < activationDeadline) await Task.Delay(100);
            KeyboardProbe.RequireForeground(owner.Window);
            var result = mode == "focus-keyboard" ? await Keyboard(workspace, output)
                : mode == "pointer-bounds" ? await PointerBounds(workspace, output) : await Airspace(workspace, output);
            await Probe.Await(owner.RequestClose()); owner.AssertClosed();
            return new { Mode = mode, Passed = true, Runtime = owner.Environment!.BrowserVersionString,
                Profile = profile, owner.Releases, Details = result };
        }
        catch
        {
            try { ScreenProbe.Capture(owner.Window, workspace.Root, Path.Combine(output, "failure.png")); }
            catch (Exception captureError) { Console.Error.WriteLine("Failure screenshot unavailable: " + captureError.Message); }
            await Probe.Await(owner.RequestClose());
            throw;
        }
    }

    private static async Task Until(Func<Task<bool>> condition, string message)
    {
        var stop = DateTime.UtcNow.AddSeconds(5);
        do { if (await condition()) return; await Task.Delay(30); } while (DateTime.UtcNow < stop);
        throw new InvalidOperationException(message);
    }
    private static async Task<bool> Is(BrowserSession owner, string expression)
        => await owner.Browser.ExecuteScriptAsync(expression) == "true";
    private static Task Active(BrowserSession owner, string id) => Until(
        () => Is(owner, $"document.hasFocus() && document.activeElement.id === {JsonSerializer.Serialize(id)}"), "Browser focus did not reach " + id);

    private static async Task<object> Keyboard(InteractionWorkspace workspace, string output)
    {
        var owner = workspace.Owner;
        Probe.Require(workspace.Before.Focus(), "Cannot focus native WPF predecessor.");
        KeyboardProbe.Press(owner.Window, 9); await Active(owner, "first");
        KeyboardProbe.Press(owner.Window, 65, 17); // Ctrl+A
        KeyboardProbe.Text(owner.Window, "framework-keyboard");
        await Until(() => Is(owner, "document.getElementById('first').value === 'framework-keyboard'"), "Real keyboard text was not received.");
        KeyboardProbe.Press(owner.Window, 9); await Active(owner, "second");
        KeyboardProbe.Press(owner.Window, 9); await Active(owner, "last");
        KeyboardProbe.Press(owner.Window, 13);
        await Until(() => Is(owner, "window.activations === 1"), "Enter did not activate the HTML button.");
        KeyboardProbe.Press(owner.Window, 9);
        await Until(() => Task.FromResult(workspace.After.IsKeyboardFocused), "Tab did not leave browser for WPF successor.");
        KeyboardProbe.Press(owner.Window, 9, 16); await Active(owner, "last");
        KeyboardProbe.Press(owner.Window, 9, 16); await Active(owner, "second");
        KeyboardProbe.Press(owner.Window, 9, 16); await Active(owner, "first");
        KeyboardProbe.Press(owner.Window, 9, 16);
        await Until(() => Task.FromResult(workspace.Before.IsKeyboardFocused), "Shift+Tab did not leave browser for WPF predecessor.");
        KeyboardProbe.Press(owner.Window, 9); await Active(owner, "first");
        KeyboardProbe.Press(owner.Window, 65, 17);
        var selection = await owner.Browser.ExecuteScriptAsync("JSON.stringify([first.value,first.selectionStart,first.selectionEnd])");
        var core = owner.Browser.CoreWebView2;
        foreach (var surface in new[] { "#202830", "#FFFFFF", "#303030" })
        {
            owner.Host.Refresh();
            ThemeStyles.Apply(owner.Window, new ThemeTokens { Surface = surface, Ink = surface == "#FFFFFF" ? "#101010" : "#FFFFFF" });
            await Probe.Flush(); await Active(owner, "first");
            Probe.Require(ReferenceEquals(core, owner.Browser.CoreWebView2), "Refresh/theme replaced the browser.");
            Probe.Require(selection == await owner.Browser.ExecuteScriptAsync("JSON.stringify([first.value,first.selectionStart,first.selectionEnd])"), "Refresh/theme lost text or selection.");
        }
        var keys = await owner.Browser.ExecuteScriptAsync("JSON.stringify(window.keys)");
        Probe.Require(await Is(owner, "window.keys.some(e=>e.key==='Tab') && window.keys.some(e=>e.key==='Enter') && window.keys.every(e=>e.trusted)"), "Missing trusted browser keyboard events.");
        ScreenProbe.Capture(owner.Window, workspace.Root, Path.Combine(output, "keyboard.png"));
        return new { ForwardAndReverseTab = true, TextInput = true, EnterActivation = true,
            RefreshThemeIdentityFocusSelection = true, BrowserKeys = keys, InputMethod = "Win32 SendInput guarded by foreground ownership" };
    }

    private static async Task<object> PointerBounds(InteractionWorkspace workspace, string output)
    {
        var owner = workspace.Owner;
        workspace.BrowserArea.Margin = new Thickness(40);
        owner.Window.UpdateLayout(); await Probe.Flush();
        await owner.Browser.ExecuteScriptAsync("window.pointerEvents=[]; document.addEventListener('pointerdown',e=>pointerEvents.push({id:e.target.id,trusted:e.isTrusted})); document.body.style.minHeight='2000px'");
        var nativeClicks = 0;
        workspace.Before.Click += (_, _) => nativeClicks++;
        async Task Wait(Func<Task<bool>> condition, string phase)
        {
            owner.Window.Title = "Framework WebView2 acceptance — " + phase;
            File.WriteAllText(Path.Combine(output, "interaction-phase.txt"), phase);
            var end = DateTime.UtcNow.AddSeconds(60);
            while (DateTime.UtcNow < end)
            {
                KeyboardProbe.RequireForeground(owner.Window);
                if (await condition()) return;
                await Task.Delay(100);
            }
            throw new InvalidOperationException("Interactive step not completed: " + phase);
        }
        await Wait(() => Task.FromResult(nativeClicks == 1), "click Before browser");
        await Wait(() => Is(owner, "activations===1 && pointerEvents.some(e=>e.id==='last' && e.trusted)"), "click Activate in local page");
        await Active(owner, "last");
        await Wait(() => Is(owner, "scrollY>100"), "scroll local blue page down");
        var inside = owner.Browser.PointToScreen(new Point(10, 10));
        var outside = owner.Browser.PointToScreen(new Point(-20, 10));
        await Until(() => Task.FromResult(ScreenProbe.Pixel(owner.Window, inside) == 0xC06000
            && ScreenProbe.Pixel(owner.Window, outside) == 0xFFFFFF), "Browser pixels escaped the allocated rectangular bounds.");
        ScreenProbe.Capture(owner.Window, workspace.Root, Path.Combine(output, "pointer-bounds.png"));
        return new { NativeClick = nativeClicks, TrustedHtmlPointer = true, HtmlFocus = true, Scrolling = true,
            RectangularBounds = true, ArbitraryWpfClipSupported = false,
            PointerEvents = await owner.Browser.ExecuteScriptAsync("JSON.stringify(pointerEvents)"),
            ScrollY = await owner.Browser.ExecuteScriptAsync("scrollY") };
    }

    private static async Task<object> Airspace(InteractionWorkspace workspace, string output)
    {
        var owner = workspace.Owner;
        var before = owner.Browser.ActualWidth;
        owner.Window.Width += 100; owner.Window.Height += 60; owner.Window.UpdateLayout(); await Probe.Flush();
        await Until(() => Is(owner, $"Math.abs(innerWidth - {owner.Browser.ActualWidth.ToString(System.Globalization.CultureInfo.InvariantCulture)}) < 3"), "Browser viewport did not follow resize.");
        Probe.Require(owner.Browser.ActualWidth > before + 90, "Native browser width did not grow.");
        workspace.Overlay.Visibility = Visibility.Visible; owner.Window.UpdateLayout();
        await Probe.Flush();
        var point = workspace.Overlay.PointToScreen(new Point(90, 50));
        // COLORREF is 0x00BBGGRR. Browser body is #0060C0; WPF overlay is #FF00FF.
        await Until(() => Task.FromResult(ScreenProbe.Pixel(owner.Window, point) == 0xC06000), "Expected native browser airspace pixel was not visible.");
        var browserPixel = ScreenProbe.Pixel(owner.Window, point);
        ScreenProbe.Capture(owner.Window, workspace.Root, Path.Combine(output, "native-covers-wpf.png"));
        owner.Browser.Visibility = Visibility.Hidden;
        await Until(() => Task.FromResult(ScreenProbe.Pixel(owner.Window, point) == 0xFF00FF), "Control check: WPF overlay did not appear when browser was hidden.");
        var hiddenPixel = ScreenProbe.Pixel(owner.Window, point);
        ScreenProbe.Capture(owner.Window, workspace.Root, Path.Combine(output, "hidden-browser-reveals-wpf.png"));
        owner.Browser.Visibility = Visibility.Visible;
        await Until(() => Task.FromResult(ScreenProbe.Pixel(owner.Window, point) == 0xC06000), "Restored browser did not regain native airspace.");
        var popupContent = new System.Windows.Controls.Border { Background = Brushes.Magenta, Width = 180, Height = 100 };
        var popup = new Popup { PlacementTarget = workspace.Overlay, Placement = PlacementMode.Relative,
            Child = popupContent, StaysOpen = true, AllowsTransparency = false };
        try
        {
            popup.IsOpen = true; await Probe.Flush();
            var popupPoint = popupContent.PointToScreen(new Point(90, 50));
            await Until(() => Task.FromResult(ScreenProbe.Pixel(owner.Window, popupPoint) == 0xFF00FF), "Separate popup HWND did not appear above browser.");
            ScreenProbe.Capture(owner.Window, workspace.Root, Path.Combine(output, "popup-above-browser.png"));
        }
        finally { popup.IsOpen = false; popup.Child = null; popup.PlacementTarget = null; }
        return new { Resize = true, WpfSiblingOverlaySupported = false, SeparatePopupVisible = true,
            BrowserPixel = browserPixel, HiddenBrowserPixel = hiddenPixel,
            Boundary = "Standard HwndHost WebView2 covers WPF sibling overlays; separate popup HWND can appear above it" };
    }
}
