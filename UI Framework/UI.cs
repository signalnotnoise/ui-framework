namespace UI_Framework;

public static class UI
{
    // Factories are invoked only when a component is first mounted or its identity changes.
    public static View Component<T>(Action<T>? configure = null) where T : Component, new() =>
        new(ViewKind.Component)
        {
            ComponentType = typeof(T),
            CreateComponent = static () => new T(),
            ConfigureComponent = configure is null ? null : component => configure((T)component)
        };
    public static View Text(string text) => new(ViewKind.Text) { Content = text };
    public static View Button(string text, Action click) => new(ViewKind.Button) { Content = text, Click = click };
    public static View TextField(State<string> value) => TextField(value.Binding());
    public static View TextField(Binding<string> value) => new(ViewKind.TextField)
        { Content = value.Value, Edit = text => value.Value = text, ReadText = () => value.Value };
    public static View Toggle(string label, State<bool> value) => Toggle(label, value.Binding());
    public static View Toggle(string label, Binding<bool> value) => new(ViewKind.Toggle)
        { Content = label, Checked = value.Value, ToggleChanged = next => value.Value = next, ReadChecked = () => value.Value };
    public static View Scroll(View content) => new(ViewKind.Scroll) { Children = [content] };
    /// <summary>Keyed rows with a finite viewport. Only visible rows and a scroll buffer mount.</summary>
    public static View VirtualList(IEnumerable<View> rows, double height) =>
        new View(ViewKind.VirtualList) { Children = rows.ToArray() }.Height(height);
    public static View VStack(params View[] children) => new(ViewKind.VStack) { Children = children };
    public static View HStack(params View[] children) => new(ViewKind.HStack) { Children = children };
}
