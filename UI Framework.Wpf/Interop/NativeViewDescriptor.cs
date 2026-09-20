using System.Windows;

namespace UI_Framework.Wpf;

internal sealed record NativeViewDescriptor(Type ControlType, Func<FrameworkElement> Create,
    Action<FrameworkElement>? Update, Action<FrameworkElement>? Release);
