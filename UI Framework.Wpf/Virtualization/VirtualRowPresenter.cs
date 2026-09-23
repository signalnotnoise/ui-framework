using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace UI_Framework.Wpf;

internal sealed class VirtualRowPresenter : ContentControl
{
    private VirtualRow? row;
    private ViewHost? host;
    private DispatcherOperation? focusOperation;

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
            var previousFocus = Keyboard.FocusedElement;
            focusOperation = Dispatcher.BeginInvoke(DispatcherPriority.Input, new Action(() =>
            {
                focusOperation = null;
                if (ReferenceEquals(row, value) && ReferenceEquals(host, attachedHost)
                    && !IsKeyboardFocusWithin && (Keyboard.FocusedElement is null
                        || ReferenceEquals(Keyboard.FocusedElement, previousFocus)
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

    internal void Deactivate() => host?.Deactivate();
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
            previous.Focus = IsKeyboardFocusWithin ? RowFocusSnapshot.Capture(this) : null;
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
