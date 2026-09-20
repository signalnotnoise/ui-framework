using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace UI_Framework.Wpf;

internal sealed class NativeControlHost : Border, IDisposable
{
    private readonly NativeViewDescriptor owner;
    private bool disposed;
    internal FrameworkElement Element { get; }

    internal NativeControlHost(NativeViewDescriptor description)
    {
        owner = description;
        Element = description.Create() ?? throw new InvalidOperationException("Native factory returned null.");
        Element.Dispatcher.VerifyAccess();
        if (Element.Parent is not null || VisualTreeHelper.GetParent(Element) is not null
            || PresentationSource.FromVisual(Element) is not null || Element is Window)
            throw new InvalidOperationException("Native factory must return an unparented element, not a window or presentation root.");
        Child = Element;
    }

    internal bool Matches(object? description) => description is NativeViewDescriptor next && next.ControlType == owner.ControlType;

    internal void Update(NativeViewDescriptor description)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        description.Update?.Invoke(Element);
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        Child = null;
        owner.Release?.Invoke(Element);
    }
}
