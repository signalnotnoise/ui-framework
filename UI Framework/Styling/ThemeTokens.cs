namespace UI_Framework;

/// <summary>Immutable, platform-independent design tokens. Apply at a renderer's theme boundary.</summary>
public sealed record ThemeTokens
{
    public SpacingTokens Spacing { get; init; } = SpacingTokens.Default;
    public TypographyTokens Typography { get; init; } = TypographyTokens.Default;
    public RadiusTokens Radii { get; init; } = RadiusTokens.Default;
    public IconTokens Icons { get; init; } = IconTokens.Default;
    public string Canvas { get; init; } = "#F4F6F8";
    public string Surface { get; init; } = "#FFFFFF";
    public string Ink { get; init; } = "#172D32";
    public string Muted { get; init; } = "#667B7D";
    public string Accent { get; init; } = "#176B57";
    public string AccentHover { get; init; } = "#125642";
    public string AccentPressed { get; init; } = "#0B4032";
    public string OnAccent { get; init; } = "#FFFFFF";
    public string Hover { get; init; } = "#EAF2EE";
    public string Pressed { get; init; } = "#D8E8DF";
    public string Border { get; init; } = "#D3DFD9";
    public string Focus { get; init; } = "#387AE0";
    public double ControlRadius { get; init; } = RadiusTokens.Default.Medium;
    public double ControlPadding { get; init; } = 10;

    internal void Validate()
    {
        ArgumentNullException.ThrowIfNull(Spacing);
        ArgumentNullException.ThrowIfNull(Typography);
        ArgumentNullException.ThrowIfNull(Radii);
        ArgumentNullException.ThrowIfNull(Icons);
        static void Dimension(double value, string name, bool positive = false)
        {
            if (!double.IsFinite(value) || (positive ? value <= 0 : value < 0))
                throw new ArgumentOutOfRangeException(name, "Design tokens must be finite and have a valid sign.");
        }
        Dimension(ControlRadius, nameof(ControlRadius));
        Dimension(ControlPadding, nameof(ControlPadding));
        Dimension(Spacing.ExtraSmall, "Spacing.ExtraSmall");
        Dimension(Spacing.Small, "Spacing.Small");
        Dimension(Spacing.Medium, "Spacing.Medium");
        Dimension(Spacing.Large, "Spacing.Large");
        Dimension(Spacing.ExtraLarge, "Spacing.ExtraLarge");
        Dimension(Spacing.Section, "Spacing.Section");
        Dimension(Typography.Caption, "Typography.Caption", true);
        Dimension(Typography.Body, "Typography.Body", true);
        Dimension(Typography.Subheading, "Typography.Subheading", true);
        Dimension(Typography.Heading, "Typography.Heading", true);
        Dimension(Typography.Title, "Typography.Title", true);
        Dimension(Radii.Small, "Radii.Small");
        Dimension(Radii.Medium, "Radii.Medium");
        Dimension(Radii.Large, "Radii.Large");
        Dimension(Icons.Small, "Icons.Small", true);
        Dimension(Icons.Medium, "Icons.Medium", true);
        Dimension(Icons.Large, "Icons.Large", true);
        Dimension(Icons.LabelGap, "Icons.LabelGap");
    }
}
