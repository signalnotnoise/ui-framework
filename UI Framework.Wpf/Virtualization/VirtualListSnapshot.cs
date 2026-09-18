namespace UI_Framework.Wpf;

internal sealed record VirtualListSnapshot(IReadOnlyDictionary<string, NodeSnapshot> Rows);
