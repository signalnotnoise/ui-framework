namespace UI_Framework;

/// <summary>Immutable, platform-independent design tokens. Apply at a renderer's theme boundary.</summary>
public sealed record ThemeTokens
{
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
    public double ControlRadius { get; init; } = 8;
    public double ControlPadding { get; init; } = 10;
}
