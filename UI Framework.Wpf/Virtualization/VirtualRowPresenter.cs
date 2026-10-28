using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Threading;

namespace UI_Framework.Wpf;

internal sealed class VirtualRowPresenter : ContentControl
{
    private VirtualRow? row;
    private ViewHost? host;
    private DispatcherOperation? focusOperation;
    private TextComposition? composition;
    private DependencyObject? compositionSource;
    private Exception? compositionFailure;
    private bool completingComposition;

    static VirtualRowPresenter()
    {
        // This is a lifetime wrapper, not an actionable keyboard destination.
        FocusableProperty.OverrideMetadata(typeof(VirtualRowPresenter), new FrameworkPropertyMetadata(false));
        IsTabStopProperty.OverrideMetadata(typeof(VirtualRowPresenter), new FrameworkPropertyMetadata(false));
        EventManager.RegisterClassHandler(typeof(VirtualRowPresenter), TextCompositionManager.PreviewTextInputStartEvent,
            new TextCompositionEventHandler(CompositionStarted), true);
        EventManager.RegisterClassHandler(typeof(VirtualRowPresenter), TextCompositionManager.PreviewTextInputUpdateEvent,
            new TextCompositionEventHandler(CompositionStarted), true);
        EventManager.RegisterClassHandler(typeof(VirtualRowPresenter), TextCompositionManager.PreviewTextInputEvent,
            new TextCompositionEventHandler(CompositionFinished), true);
    }

    private static void CompositionStarted(object sender, TextCompositionEventArgs args)
    {
        // Preview events tunnel through every containing list. Only the nearest
        // row owns this editor, including when provider completion throws.
        var current = args.OriginalSource as DependencyObject;
        while (current is not null && current is not VirtualRowPresenter)
            current = current switch
            {
                Visual or System.Windows.Media.Media3D.Visual3D => VisualTreeHelper.GetParent(current),
                ContentElement content => ContentOperations.GetParent(content) ?? (content as FrameworkContentElement)?.Parent,
                _ => LogicalTreeHelper.GetParent(current)
            };
        if (ReferenceEquals(current, sender))
        {
            var presenter = (VirtualRowPresenter)sender;
            if (presenter.row is null || presenter.completingComposition) return;
            presenter.composition = args.TextComposition;
            presenter.compositionSource = args.OriginalSource as DependencyObject;
        }
    }

    private static void CompositionFinished(object sender, TextCompositionEventArgs args)
    {
        var presenter = (VirtualRowPresenter)sender;
        // Native TSF creates a new FrameworkTextComposition for each phase.
        if (ReferenceEquals(presenter.compositionSource, args.OriginalSource))
        {
            presenter.composition = null;
            presenter.compositionSource = null;
        }
    }

    private void CompleteComposition()
    {
        var active = composition;
        if (active is null || completingComposition) return;
        completingComposition = true;
        try
        {
            // TextEditor finalizes TSF/IMM on LostKeyboardFocus, while the editor
            // is still attached. FrameworkTextComposition.Complete merely sets
            // a flag consumed during its event; storing and calling it later
            // does not complete the native session.
            if (compositionSource is UIElement { IsKeyboardFocusWithin: true })
            {
                Keyboard.ClearFocus();
                if (compositionSource is UIElement { IsKeyboardFocusWithin: true })
                    throw new InvalidOperationException("Active composition prevented focus departure before row release.");
            }
            if (ReferenceEquals(composition, active) && active is not FrameworkTextComposition)
                active.Complete();
        }
        catch (Exception error) { compositionFailure = error; }
        finally
        {
            composition = null;
            compositionSource = null;
            completingComposition = false;
        }
    }

    internal void Attach(VirtualRow value, double gap)
    {
        Release();
        row = value;
        row.Presenter = this;
        Margin = new Thickness(0, 0, 0, gap);
        HorizontalContentAlignment = HorizontalAlignment.Stretch;
        try { host = new ViewHost(() => value.View, value.Saved); }
        catch { row.Presenter = null; row = null; throw; }
        value.Saved = null;
        Content = host;
        if (value.Focus is { } focus)
        {
            value.Focus = null;
            var attachedHost = host;
            focusOperation = Dispatcher.BeginInvoke(DispatcherPriority.Input, new Action(() =>
            {
                focusOperation = null;
                if (ReferenceEquals(row, value) && ReferenceEquals(host, attachedHost)
                    && !IsKeyboardFocusWithin && (Keyboard.FocusedElement is null
                        || focus.Owns(Keyboard.FocusedElement)
                        || ReferenceEquals(Keyboard.FocusedElement, Window.GetWindow(this))))
                    focus.Restore(this);
            }));
        }
    }

    internal void Refresh(double gap)
    {
        Margin = new Thickness(0, 0, 0, gap);
        host?.Refresh();
    }

    internal void Deactivate()
    {
        CompleteComposition();
        host?.Deactivate();
    }
    internal NodeSnapshot? Capture() => host?.Capture();

    internal void Release()
    {
        focusOperation?.Abort();
        focusOperation = null;
        if (row is null) return;
        var previous = row;
        row = null;
        var old = host;
        host = null;
        previous.Presenter = null;
        Exception? failure = null;
        try
        {
            var focused = IsKeyboardFocusWithin ? Keyboard.FocusedElement : null;
            CompleteComposition();
            // Nested editors must finish before snapshots capture their final state.
            old?.Deactivate();
            var completionError = compositionFailure;
            compositionFailure = null;
            if (completionError is not null)
                System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(completionError).Throw();
            previous.Focus = focused is not null ? RowFocusSnapshot.Capture(this, focused) : null;
            previous.Saved = old?.Capture();
        }
        catch (Exception error) { previous.Saved = null; previous.Focus = null; failure = error; }
        Content = null;
        try { old?.Dispose(); }
        catch (Exception error)
        {
            if (failure is not null) throw new AggregateException("Row capture and cleanup failed.", failure, error);
            throw;
        }
        if (failure is not null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(failure).Throw();
    }
}
