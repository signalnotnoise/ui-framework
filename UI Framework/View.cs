namespace UI_Framework;

public sealed record View(ViewKind Kind)
{
    public string? Key { get; init; }
    public string Content { get; init; } = "";
    public IReadOnlyList<View> Children { get; init; } = [];
    public Action? Click { get; init; }
    public Action<string>? Edit { get; init; }
    public Func<string>? ReadText { get; init; }
    public bool Checked { get; init; }
    public Action<bool>? ToggleChanged { get; init; }
    public Func<bool>? ReadChecked { get; init; }
    public Type? ComponentType { get; init; }
    public Func<Component>? CreateComponent { get; init; }
    public Action<Component>? ConfigureComponent { get; init; }
    public bool IsMemoized { get; init; }
    public object? MemoInputs { get; init; }
    public double Gap { get; init; }
    public double Inset { get; init; }
    public double TextSize { get; init; } = 16;
    public double DesiredWidth { get; init; } = double.NaN;
    public double DesiredHeight { get; init; } = double.NaN;
    public string? BackgroundColor { get; init; }
    public string? ForegroundColor { get; init; }
    public double Radius { get; init; }
    public double FlexWeight { get; init; } = 1;
    public double MinimumColumnWidth { get; init; } = 240;
    public ViewAlignment Horizontal { get; init; } = ViewAlignment.Stretch;
    public ViewAlignment Vertical { get; init; } = ViewAlignment.Stretch;
    public ButtonStyleKind ButtonAppearance { get; init; }
    public bool Enabled { get; init; } = true;
    public View Flex(double weight = 1) => this with { FlexWeight = Dimension(weight) };
    public View Align(ViewAlignment horizontal, ViewAlignment vertical = ViewAlignment.Stretch) => this with { Horizontal = horizontal, Vertical = vertical };
    public View ButtonStyle(ButtonStyleKind style) => this with { ButtonAppearance = style };
    public View IsEnabled(bool enabled) => this with { Enabled = enabled };
    public View Id(string key) => this with { Key = key };
    /// <summary>Skip parent-driven component rebuilds when these immutable input values compare equal.</summary>
    public View Memo(object? inputs) => Kind == ViewKind.Component
        ? this with { IsMemoized = true, MemoInputs = inputs }
        : throw new InvalidOperationException("Memo applies only to component descriptions.");
    public View Spacing(double value) => this with { Gap = Dimension(value) };
    public View Padding(double value) => this with { Inset = value };
    public View FontSize(double value) => this with { TextSize = value };
    public View Width(double value) => this with { DesiredWidth = Dimension(value) };
    public View Height(double value) => this with { DesiredHeight = Dimension(value) };
    public View Background(string color) => this with { BackgroundColor = color };
    public View Foreground(string color) => this with { ForegroundColor = color };
    public View CornerRadius(double value) => this with { Radius = Dimension(value) };
    private static double Dimension(double value) => double.IsFinite(value) && value >= 0
        ? value : throw new ArgumentOutOfRangeException(nameof(value), "Use a finite, non-negative dimension.");
}
