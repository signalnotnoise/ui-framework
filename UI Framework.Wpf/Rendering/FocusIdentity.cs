using System.Windows;

namespace UI_Framework.Wpf;

// Identity is attached only to renderer-owned frames; ordinary Border styling
// and application-owned native metadata remain unchanged.
internal static class FocusIdentity
{
    internal static readonly DependencyProperty KeyProperty = DependencyProperty.RegisterAttached(
        "Key", typeof(string), typeof(FocusIdentity), new PropertyMetadata(null));

    internal static string? GetKey(DependencyObject target) => (string?)target.GetValue(KeyProperty);
}
