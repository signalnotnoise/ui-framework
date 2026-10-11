using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace UI_Framework.Wpf;

internal static class NativeStyleResources
{
    // Empty implicit styles stop ancestor lookup but leave WPF theme styles,
    // explicit local styles and dictionaries inside the native island intact.
    internal static readonly IReadOnlyDictionary<Type, Style> Styles = Create();

    private static Dictionary<Type, Style> Create()
    {
        var result = new Dictionary<Type, Style>();
        foreach (var type in new[] { typeof(Button), typeof(ToggleButton), typeof(RepeatButton),
            typeof(TextBox), typeof(PasswordBox), typeof(ComboBox), typeof(ComboBoxItem),
            typeof(CheckBox), typeof(RadioButton), typeof(ListBox), typeof(ListBoxItem),
            typeof(ListView), typeof(ListViewItem), typeof(TreeView), typeof(TreeViewItem),
            typeof(ScrollViewer), typeof(Menu), typeof(MenuItem), typeof(ContextMenu),
            typeof(TextBlock), typeof(Label), typeof(Slider), typeof(ProgressBar),
            typeof(TabControl), typeof(TabItem) })
        {
            var style = new Style(type);
            style.Seal();
            result.Add(type, style);
        }
        return result;
    }
}
