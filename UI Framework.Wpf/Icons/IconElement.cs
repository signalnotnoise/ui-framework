using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Documents;
using System.Windows.Media;
using UI_Framework;

namespace UI_Framework.Wpf;

internal sealed class IconElement : FrameworkElement
{
    static IconElement() => TextElement.ForegroundProperty.AddOwner(typeof(IconElement),
        new FrameworkPropertyMetadata(Brushes.Black, FrameworkPropertyMetadataOptions.Inherits | FrameworkPropertyMetadataOptions.AffectsRender));

    private VectorIcon? definition;
    private Geometry? geometry;
    private Pen? pen;
    private double size;
    internal bool Decorative { get; init; }
    internal Geometry? Geometry => geometry;

    internal void Update(VectorIcon icon, double iconSize)
    {
        if (!ReferenceEquals(definition, icon))
        {
            geometry = IconGeometry.Get(icon);
            definition = icon;
            pen = null;
            InvalidateVisual();
        }
        if (size != iconSize)
        {
            size = iconSize;
            InvalidateMeasure();
            InvalidateVisual();
        }
    }

    protected override Size MeasureOverride(Size availableSize) => new(size, size);

    protected override void OnRender(DrawingContext drawingContext)
    {
        if (definition is null || geometry is null) return;
        var brush = TextElement.GetForeground(this);
        var scale = Math.Min(size / definition.ViewportWidth, size / definition.ViewportHeight);
        var x = (RenderSize.Width - definition.ViewportWidth * scale) / 2;
        var y = (RenderSize.Height - definition.ViewportHeight * scale) / 2;
        drawingContext.PushTransform(new MatrixTransform(scale, 0, 0, scale, x, y));
        drawingContext.PushClip(new RectangleGeometry(new Rect(0, 0, definition.ViewportWidth, definition.ViewportHeight)));
        if (definition.StrokeWidth > 0 && (pen is null || !ReferenceEquals(pen.Brush, brush)))
        {
            pen = new Pen(brush, definition.StrokeWidth) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round, LineJoin = PenLineJoin.Round };
            if (pen.CanFreeze) pen.Freeze();
        }
        drawingContext.DrawGeometry(definition.StrokeWidth == 0 ? brush : null, definition.StrokeWidth > 0 ? pen : null, geometry);
        drawingContext.Pop();
        drawingContext.Pop();
    }

    protected override AutomationPeer? OnCreateAutomationPeer() => Decorative ? null : new IconAutomationPeer(this);
}
