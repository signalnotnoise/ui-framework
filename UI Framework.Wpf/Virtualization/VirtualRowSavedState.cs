namespace UI_Framework.Wpf;

// Allocated only while a recycled/restored row has component or focus state.
internal sealed class VirtualRowSavedState
{
    internal NodeSnapshot? Component;
    internal RowFocusSnapshot? Focus;
}
