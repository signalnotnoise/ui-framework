using System.Windows;
using UI_Framework;

namespace UI_Framework.Wpf;

public static class WpfUI
{
    /// <summary>Retains a native element until its key, position, or declared type changes.
    /// Release runs once after detaching; no implicit IDisposable call is made.</summary>
    public static View Native<T>(Func<T> create, Action<T>? update = null, Action<T>? release = null)
        where T : FrameworkElement
    {
        ArgumentNullException.ThrowIfNull(create);
        return new(ViewKind.Platform)
        {
            PlatformContent = new NativeViewDescriptor(typeof(T), () => create(),
                update is null ? null : element => update((T)element),
                release is null ? null : element => release((T)element))
        };
    }
}
