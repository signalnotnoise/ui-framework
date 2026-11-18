using UI_Framework;
using static UI_Framework.UI;

namespace LogViewer;

public sealed class LogStream : Component
{
    public LogWorkspace Model { get; set; } = null!;
    public bool FullList { get; set; }
    public override View Body()
    {
        var entries = Model.Visible.Value;
        var rows = entries.Select(entry => Component<LogRow>(row => { row.Model = Model; row.Entry = entry; })
            .Id(entry.Id.ToString(System.Globalization.CultureInfo.InvariantCulture))).ToArray();
        var list = FullList ? Scroll(VStack(rows)).Height(Model.ListHeight) : VirtualList(rows, Model.ListHeight);
        return Dock(entries.Count == 0
                ? VStack(Text("No matching events").FontSize(22), Text("Try another search or clear the filters.").FontSize(13),
                    Button("Show all events", Model.ClearFilters).Height(36).Width(160)).Spacing(16).Padding(24)
                : list.Id("log-rows"),
            top: FlexRow(VStack(Text("Event stream").FontSize(18),
                    Text($"{entries.Count:N0} matching / {Model.Entries.Count:N0} buffered").FontSize(11).Foreground(StudioTheme.Tokens.Muted)).Spacing(4),
                Button(Model.NewestFirst.Value ? "Newest first ↓" : "Oldest first ↑", () => Model.NewestFirst.Value = !Model.NewestFirst.Value)
                    .Width(116).Height(32).FontSize(12).ButtonStyle(ButtonStyleKind.Quiet)).Padding(12).Height(72),
            bottom: Text("Select View to inspect an event  ·  timestamps in UTC").FontSize(11)
                .Foreground(StudioTheme.Tokens.Muted).Padding(10).Height(36));
    }
}
