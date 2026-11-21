namespace UI_Framework;

/// <summary>Reusable corner radii in DIPs. ControlRadius remains a separate semantic override.</summary>
public sealed record RadiusTokens
{
    public static RadiusTokens Default { get; } = new();
    public double Small { get; init; } = 4;
    public double Medium { get; init; } = 8;
    public double Large { get; init; } = 12;
}
