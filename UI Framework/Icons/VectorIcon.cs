namespace UI_Framework;

/// <summary>Reusable monochrome path in a zero-origin viewport. A zero stroke width fills the path;
/// a positive width draws a rounded outline. The renderer validates path syntax.</summary>
public sealed class VectorIcon
{
    public string Path { get; }
    public double ViewportWidth { get; }
    public double ViewportHeight { get; }
    public double StrokeWidth { get; }

    public VectorIcon(string path, double viewportWidth = IconDefaults.ViewportSize, double viewportHeight = IconDefaults.ViewportSize, double strokeWidth = 0)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        if (!double.IsFinite(viewportWidth) || viewportWidth <= 0) throw new ArgumentOutOfRangeException(nameof(viewportWidth));
        if (!double.IsFinite(viewportHeight) || viewportHeight <= 0) throw new ArgumentOutOfRangeException(nameof(viewportHeight));
        if (!double.IsFinite(strokeWidth) || strokeWidth < 0) throw new ArgumentOutOfRangeException(nameof(strokeWidth));
        Path = path;
        ViewportWidth = viewportWidth;
        ViewportHeight = viewportHeight;
        StrokeWidth = strokeWidth;
    }
}
