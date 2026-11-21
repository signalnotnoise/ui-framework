namespace UI_Framework;

/// <summary>Reusable font sizes in DIPs. Select a role with View.FontSize.</summary>
public sealed record TypographyTokens
{
    public static TypographyTokens Default { get; } = new();
    public double Caption { get; init; } = 12;
    public double Body { get; init; } = 16;
    public double Subheading { get; init; } = 20;
    public double Heading { get; init; } = 24;
    public double Title { get; init; } = 32;
}
