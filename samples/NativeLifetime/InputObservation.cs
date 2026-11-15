using System.Windows;
using System.Windows.Input;

namespace NativeLifetime;

// Bounded, passive observation of this test window only. Does not suppress input.
internal sealed class InputObservation : IDisposable
{
    private readonly Window window;
    private bool pointerEverInside;
    private long moves, buttons, keys, activations;

    internal InputObservation(Window window)
    {
        this.window = window;
        pointerEverInside = window.IsMouseOver;
        window.MouseEnter += OnEnter;
        window.PreviewMouseMove += OnMove;
        window.PreviewMouseDown += OnButton;
        window.PreviewKeyDown += OnKey;
        window.Activated += OnActivated;
    }

    private void OnEnter(object sender, MouseEventArgs args) => pointerEverInside = true;
    private void OnMove(object sender, MouseEventArgs args) { pointerEverInside = true; moves++; }
    private void OnButton(object sender, MouseButtonEventArgs args) => buttons++;
    private void OnKey(object sender, KeyEventArgs args) => keys++;
    private void OnActivated(object? sender, EventArgs args) => activations++;

    internal long ActivationCount => activations;

    internal object Snapshot() => new { Protocol = "window-routed-input-v1", PointerEverInside = pointerEverInside,
        PointerInsideNow = window.IsMouseOver, MouseMoves = moves, MouseButtons = buttons, KeyDowns = keys,
        Activations = activations, IsActive = window.IsActive, HasKeyboardFocus = window.IsKeyboardFocusWithin,
        Scope = "Passive WPF routed input on this native test window; not a global input log or native-HWND input monitor." };

    public void Dispose()
    {
        window.MouseEnter -= OnEnter;
        window.PreviewMouseMove -= OnMove;
        window.PreviewMouseDown -= OnButton;
        window.PreviewKeyDown -= OnKey;
        window.Activated -= OnActivated;
    }
}
