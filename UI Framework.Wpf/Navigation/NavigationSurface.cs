using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using UI_Framework;

namespace UI_Framework.Wpf;

internal sealed class NavigationSurface : ContentControl, IDisposable
{
    private readonly Dictionary<string, NodeSnapshot> saved = [];
    private ViewHost? active;
    private View? current;
    private bool disposed;

    internal NavigationSurface(NavigationSnapshot? snapshot = null)
    {
        if (snapshot is not null)
            foreach (var (key, screen) in snapshot.Screens) saved.Add(key, screen);
        Focusable = false;
        HorizontalContentAlignment = HorizontalAlignment.Stretch;
        VerticalContentAlignment = VerticalAlignment.Stretch;
    }

    internal void Update(IReadOnlyList<View> screens, NavigationTransition transition)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        var next = screens[^1];
        var keys = screens.Select(screen => screen.Key!).ToHashSet();
        foreach (var key in saved.Keys.Where(key => !keys.Contains(key)).ToArray()) saved.Remove(key);
        if (current?.Key == next.Key && active is not null)
        {
            current = next;
            if (transition == NavigationTransition.None || !SystemParameters.ClientAreaAnimation) StopTransition();
            active.Refresh();
            return;
        }
        if (active is not null)
        {
            if (current is not null && keys.Contains(current.Key!) && active.Capture() is { } snapshot)
                saved[current.Key!] = snapshot;
            StopTransition();
            active.Dispose();
        }
        Content = null;
        active = null;
        current = next;
        saved.Remove(next.Key!, out var restored);
        active = new ViewHost(() => current!, restored);
        Content = active;
        if (IsLoaded && transition == NavigationTransition.FadeSlide && SystemParameters.ClientAreaAnimation)
        {
            var duration = TimeSpan.FromMilliseconds(160);
            var transform = new TranslateTransform();
            active.RenderTransform = transform;
            active.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, duration) { FillBehavior = FillBehavior.Stop });
            transform.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(10, 0, duration)
                { FillBehavior = FillBehavior.Stop, EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut } });
        }
    }

    internal void Deactivate() => active?.Deactivate();

    private void StopTransition()
    {
        active?.BeginAnimation(OpacityProperty, null);
        if (active?.RenderTransform is TranslateTransform transform) transform.BeginAnimation(TranslateTransform.YProperty, null);
    }

    internal NavigationSnapshot Capture()
    {
        var screens = new Dictionary<string, NodeSnapshot>(saved);
        if (current is not null && active?.Capture() is { } snapshot) screens[current.Key!] = snapshot;
        return new(screens);
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        if (active is not null)
        {
            StopTransition();
            active.Dispose();
        }
        active = null;
        current = null;
        Content = null;
        saved.Clear();
    }
}
