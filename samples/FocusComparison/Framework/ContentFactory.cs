using System.Windows.Controls;
using UI_Framework;
using UI_Framework.Wpf;

namespace FocusComparison;

internal static class ContentFactory
{
    internal const string Kind = "framework-native";
    internal static ContentLease Create()
    {
        var editors = Enumerable.Range(0, 10).Select(i => new TextBox { Text = $"Editor {i}", Height = 26 }).ToArray();
        var host = new ViewHost(() => UI.VStack(editors.Select(editor => WpfUI.Native(() => editor)).ToArray()));
        return new(host, editors, host.Dispose);
    }
}
