using System.Runtime.CompilerServices;
using System.Windows.Media;
using UI_Framework;

namespace UI_Framework.Wpf;

internal static class IconGeometry
{
    // Definitions are application-owned: released definitions do not accumulate in a global string cache.
    private static readonly ConditionalWeakTable<VectorIcon, Geometry> cache = new();

    internal static Geometry Get(VectorIcon icon) => cache.GetValue(icon, static definition =>
    {
        var geometry = Geometry.Parse(definition.Path);
        var bounds = geometry.Bounds;
        if (!bounds.IsEmpty && (!double.IsFinite(bounds.X) || !double.IsFinite(bounds.Y)
            || !double.IsFinite(bounds.Width) || !double.IsFinite(bounds.Height)))
            throw new ArgumentException("Icon geometry must have finite bounds.", nameof(icon));
        geometry.Freeze();
        return geometry;
    });
}
