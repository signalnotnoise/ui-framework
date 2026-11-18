using UI_Framework;

namespace LogViewer;

public static class StudioTheme
{
    public const string Panel = "#191E26";
    public const string Raised = "#202731";
    public const string Selection = "#343126";
    public static ThemeTokens Tokens { get; } = new() {
        Canvas = "#12161D", Surface = Panel, Ink = "#E7ECF2", Muted = "#A8B3C3",
        Accent = "#E6B566", AccentHover = "#F1C987", AccentPressed = "#D39C45", OnAccent = "#171B22",
        Hover = "#2B3441", Pressed = "#364251", Border = "#394553", Focus = "#95C9F6",
        ControlRadius = 3, ControlPadding = 6
    };
    public static string LevelColor(string level) => level switch {
        "Error" => "#FF9D97", "Warning" => "#E6B566", "Debug" => "#ADAFE9", _ => "#8BCDBB"
    };
}
