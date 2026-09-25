using System.Windows;
using System.Windows.Controls;

internal sealed class InheritedValueProbe : Border
{
    internal static readonly DependencyProperty MarkerProperty = DependencyProperty.RegisterAttached(
        "Marker", typeof(int), typeof(InheritedValueProbe),
        new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.Inherits));

    internal int Changes { get; set; }

    protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        if (e.Property == MarkerProperty) Changes++;
    }
}
