namespace UI_Framework;

/// <summary>Reusable layout spacing in DIPs. Values are selected explicitly by view composition.</summary>
public sealed record SpacingTokens
{
    public static SpacingTokens Default { get; } = new();
    public double ExtraSmall { get; init; } = 4;
    public double Small { get; init; } = 8;
    public double Medium { get; init; } = 12;
    public double Large { get; init; } = 16;
    public double ExtraLarge { get; init; } = 24;
    public double Section { get; init; } = 32;
}
