namespace UI_Framework;

/// <summary>Icon display sizes and the native icon-button label gap in DIPs, independent of path coordinates.</summary>
public sealed record IconTokens
{
    internal const double DefaultSmall = 16;
    public static IconTokens Default { get; } = new();
    public double Small { get; init; } = DefaultSmall;
    public double Medium { get; init; } = 24;
    public double Large { get; init; } = 32;
    public double LabelGap { get; init; } = 6;
}
