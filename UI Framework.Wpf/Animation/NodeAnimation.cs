using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using UI_Framework;

namespace UI_Framework.Wpf;

/// <summary>One mounted frame's native clocks. Never captured in logical snapshots.</summary>
internal sealed class NodeAnimation : IDisposable
{
    private static readonly DependencyProperty ControllerProperty = DependencyProperty.RegisterAttached(
        "Controller", typeof(NodeAnimation), typeof(NodeAnimation));
    private static readonly QuadraticEase easeOut = CreateEase();
    private readonly Border frame;
    private readonly EventHandler completed;
    private TranslateTransform? translation;
    private AnimationClock? opacityClock, xClock, yClock;
    private bool subscribed, disposed;

    internal NodeAnimation(Border frame)
    {
        this.frame = frame;
        completed = Completed;
        frame.SetValue(ControllerProperty, this);
        frame.Unloaded += Unloaded;
    }

    internal static NodeAnimation? GetController(DependencyObject element) => (NodeAnimation?)element.GetValue(ControllerProperty);
    // Keep clock/policy setup out of the ordinary reconciliation method's JIT path.
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void Patch(Border frame, ViewMotion? previous, ViewMotion? next, bool created)
    {
        var controller = GetController(frame) ?? new NodeAnimation(frame);
        controller.Update(previous, next, !created && frame.IsLoaded
            && AnimationSettings.GetIsEnabled(frame) && SystemParameters.ClientAreaAnimation);
    }
    internal bool HasClocks => opacityClock is not null || xClock is not null || yClock is not null;
    internal AnimationClock? OpacityClock => opacityClock;
    internal AnimationClock? XClock => xClock;
    internal AnimationClock? YClock => yClock;

    private static QuadraticEase CreateEase()
    {
        var ease = new QuadraticEase { EasingMode = EasingMode.EaseOut };
        ease.Freeze();
        return ease;
    }

    internal void Update(ViewMotion? previous, ViewMotion? next, bool animate)
    {
        var old = previous ?? ViewMotion.Default;
        var target = next ?? ViewMotion.Default;
        var spec = target.Animation;
        var properties = animate && spec is { Duration.Ticks: > 0 } ? target.Properties : AnimationProperties.None;
        Change(frame, UIElement.OpacityProperty, old.Opacity, target.Opacity,
            (properties & AnimationProperties.Opacity) != 0, spec, ref opacityClock);
        if (translation is null && (target.X != 0 || target.Y != 0))
            frame.RenderTransform = translation = new TranslateTransform();
        if (translation is not null)
        {
            var move = (properties & AnimationProperties.Translation) != 0;
            Change(translation, TranslateTransform.XProperty, old.X, target.X, move, spec, ref xClock);
            Change(translation, TranslateTransform.YProperty, old.Y, target.Y, move, spec, ref yClock);
        }
        ObserveSystemPreference();
    }

    private void Change(DependencyObject owner, DependencyProperty property, double previous, double target,
        bool animate, AnimationSpec? spec, ref AnimationClock? clock)
    {
        if (!animate)
        {
            Stop(owner, property, ref clock);
            if ((double)owner.GetValue(property) != target) owner.SetValue(property, target);
            return;
        }
        if (previous == target) return; // Refreshes and timing-only edits do not restart a clock.
        var from = (double)owner.GetValue(property);
        Stop(owner, property, ref clock);
        owner.SetValue(property, target); // The destination survives clock removal and FillBehavior.Stop.
        if (from == target) return;
        var timeline = new DoubleAnimation(from, target, new Duration(spec!.Duration))
        {
            FillBehavior = FillBehavior.Stop,
            EasingFunction = spec.Easing == AnimationEasing.EaseOut ? easeOut : null
        };
        timeline.Freeze();
        clock = (AnimationClock)timeline.CreateClock(true);
        clock.Completed += completed;
        Apply(owner, property, clock);
    }

    private static void Apply(DependencyObject owner, DependencyProperty property, AnimationClock? clock)
    {
        if (owner is UIElement element) element.ApplyAnimationClock(property, clock, HandoffBehavior.SnapshotAndReplace);
        else ((Animatable)owner).ApplyAnimationClock(property, clock, HandoffBehavior.SnapshotAndReplace);
    }

    private void Stop(DependencyObject owner, DependencyProperty property, ref AnimationClock? clock)
    {
        if (clock is null) return;
        var old = clock;
        clock = null;
        old.Completed -= completed;
        Apply(owner, property, null);
        old.Controller?.Remove();
    }

    private void Completed(object? sender, EventArgs args)
    {
        if (ReferenceEquals(sender, opacityClock)) Stop(frame, UIElement.OpacityProperty, ref opacityClock);
        if (ReferenceEquals(sender, xClock)) Stop(translation!, TranslateTransform.XProperty, ref xClock);
        if (ReferenceEquals(sender, yClock)) Stop(translation!, TranslateTransform.YProperty, ref yClock);
        ObserveSystemPreference();
    }

    internal void Settle()
    {
        Stop(frame, UIElement.OpacityProperty, ref opacityClock);
        if (translation is not null)
        {
            Stop(translation, TranslateTransform.XProperty, ref xClock);
            Stop(translation, TranslateTransform.YProperty, ref yClock);
        }
        ObserveSystemPreference();
    }

    private void ObserveSystemPreference()
    {
        if (subscribed == HasClocks) return;
        subscribed = HasClocks;
        if (subscribed) SystemParameters.StaticPropertyChanged += SystemPreferenceChanged;
        else SystemParameters.StaticPropertyChanged -= SystemPreferenceChanged;
    }

    private void SystemPreferenceChanged(object? sender, PropertyChangedEventArgs args)
    {
        if (args.PropertyName != nameof(SystemParameters.ClientAreaAnimation)) return;
        if (!frame.Dispatcher.CheckAccess())
        {
            frame.Dispatcher.BeginInvoke(new Action(() => SystemPreferenceChanged(sender, args)));
            return;
        }
        if (!disposed && !SystemParameters.ClientAreaAnimation) Settle();
    }

    private void Unloaded(object sender, RoutedEventArgs args) => Settle();

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        Settle();
        frame.Unloaded -= Unloaded;
        frame.ClearValue(ControllerProperty);
    }
}
