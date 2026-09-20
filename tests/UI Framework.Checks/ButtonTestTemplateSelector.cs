using System.Windows;
using System.Windows.Controls;

internal sealed class ButtonTestTemplateSelector(DataTemplate template) : DataTemplateSelector
{
    public override DataTemplate SelectTemplate(object item, DependencyObject container) => template;
}
