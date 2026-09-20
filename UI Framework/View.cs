namespace UI_Framework;

public sealed record View(ViewKind Kind)
{
    public string? Key { get; init; }
    public string Content { get; init; } = "";
    public string? AccessibleName { get; init; }
    /// <summary>Opaque renderer-specific description; use the platform's factory API.</summary>
    public object? PlatformContent { get; init; }
    public IReadOnlyList<View> Children { get; init; } = [];
    public Action? Click { get; init; }
    public Action<string>? Edit { get; init; }
    public Func<string>? ReadText { get; init; }
    public bool ReadOnly { get; init; }
    public int MaximumLength { get; init; }
    public int UndoHistoryLimit { get; init; } = 100;
    public IReadOnlyList<string> Options { get; init; } = [];
    public int SelectedIndex { get; init; } = -1;
    public Action<int>? SelectionChanged { get; init; }
    public Func<int>? ReadSelectedIndex { get; init; }
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
    public NavigationTransition Transition { get; init; } = NavigationTransition.FadeSlide;
    public View Flex(double weight = 1) => this with { FlexWeight = Dimension(weight) };
    public View Align(ViewAlignment horizontal, ViewAlignment vertical = ViewAlignment.Stretch) => this with { Horizontal = horizontal, Vertical = vertical };
    public View ButtonStyle(ButtonStyleKind style) => this with { ButtonAppearance = style };
    public View IsEnabled(bool enabled) => this with { Enabled = enabled };
    public View IsReadOnly(bool readOnly) => Kind is ViewKind.TextField or ViewKind.TextEditor
        ? this with { ReadOnly = readOnly } : throw new InvalidOperationException("IsReadOnly applies only to text fields and editors.");
    /// <summary>Native user-input length limit; 0 is unlimited. Bound values are not truncated.</summary>
    public View MaxLength(int length) => Kind is ViewKind.TextField or ViewKind.TextEditor or ViewKind.PasswordField
        ? this with { MaximumLength = NonNegative(length) } : throw new InvalidOperationException("MaxLength applies only to text inputs.");
    /// <summary>Maximum undo actions; 0 disables undo. Read-only text always disables undo.</summary>
    public View UndoLimit(int limit) => Kind is ViewKind.TextField or ViewKind.TextEditor
        ? this with { UndoHistoryLimit = NonNegative(limit) } : throw new InvalidOperationException("UndoLimit applies only to text fields and editors.");
    public View Id(string key) => this with { Key = key };
    /// <summary>Sets the native automation name without changing visible content. Null restores the native name.</summary>
    public View AccessibilityLabel(string? label) => this with { AccessibleName = label };
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
    private static int NonNegative(int value) => value >= 0 ? value : throw new ArgumentOutOfRangeException(nameof(value));
}
