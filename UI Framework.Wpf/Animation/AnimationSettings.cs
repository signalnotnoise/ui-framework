using System.Windows;

namespace UI_Framework.Wpf;

/// <summary>Inherited application motion preference; Windows motion preferences also apply.</summary>
public static class AnimationSettings
{
    public static readonly DependencyProperty IsEnabledProperty = DependencyProperty.RegisterAttached(
        "IsEnabled", typeof(bool), typeof(AnimationSettings),
        new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.Inherits, Changed));

    public static bool GetIsEnabled(DependencyObject element) => (bool)element.GetValue(IsEnabledProperty);
    public static void SetIsEnabled(DependencyObject element, bool value) => element.SetValue(IsEnabledProperty, value);

    private static void Changed(DependencyObject element, DependencyPropertyChangedEventArgs args)
    {
        if (!(bool)args.NewValue) NodeAnimation.GetController(element)?.Settle();
    }
}
