namespace UI_Framework.Wpf;

internal sealed record NavigationSnapshot(IReadOnlyDictionary<string, NodeSnapshot> Screens);
