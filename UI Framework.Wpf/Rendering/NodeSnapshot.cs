using UI_Framework;

namespace UI_Framework.Wpf;
// A saved logical tree owns component instances, never native elements or view sessions.
internal sealed record NodeSnapshot(ViewKind Kind, string? Key, Type? ComponentType,
    Component? Instance, NodeSnapshot? Body, IReadOnlyList<NodeSnapshot> Children)
{
    internal bool Matches(View view) => Kind == view.Kind && Key == view.Key && ComponentType == view.ComponentType;
}
