using UI_Framework;

namespace StressLab;

public sealed class LaunchModel
{
    private int nextId = 108;
    public NavigationStack<LaunchRoute> Navigation { get; } = new(new(LaunchScreen.Overview));
    public State<bool> ReduceMotion { get; } = new(false);

    public void OpenBoard()
    {
        while (Navigation.Current.Screen == LaunchScreen.Details && Navigation.CanGoBack) Navigation.Back();
        if (Navigation.Current.Screen != LaunchScreen.Board) Navigation.Push(new(LaunchScreen.Board));
    }

    public void OpenDetails(LaunchItem item)
    {
        Selected.Value = item;
        Navigation.Push(new(LaunchScreen.Details, item.Id));
    }
    public StateList<LaunchItem> Items { get; } = new([
        new(101, "A warmer first impression", "Maya", "DESIGN", LaunchStage.Building, true, "Make the welcome screen feel personal. Keep the first action obvious."),
        new(102, "Invite your team", "Noah", "PRODUCT", LaunchStage.Planned, true, "Let people bring a teammate into their workspace in one step."),
        new(103, "Search that feels instant", "Alex", "ENGINEERING", LaunchStage.Building, false, "Filter results while typing and keep the current context visible."),
        new(104, "A home for every project", "Maya", "DESIGN", LaunchStage.Shipped, false, "Give each project a clear identity and a useful overview."),
        new(105, "Keyboard-first workflows", "Alex", "ENGINEERING", LaunchStage.Planned, false, "Make everyday actions comfortable without reaching for the mouse."),
        new(106, "Share the small wins", "Noah", "PRODUCT", LaunchStage.Shipped, false, "Show completed work alongside the people who made it happen."),
        new(107, "Polish the empty states", "Maya", "DESIGN", LaunchStage.Planned, false, "Every empty view should explain what to do next.")
    ]);
    public State<string> Query { get; } = new("");
    public State<string> Draft { get; } = new("");
    public State<bool> PriorityOnly { get; } = new(false);
    public State<LaunchItem?> Selected { get; } = new(null);
    public State<string> Activity { get; } = new("Your next release starts here.");

    public LaunchModel() => Selected.Value = Items[0];

    public IEnumerable<LaunchItem> VisibleItems() => Items.Where(item =>
        (!PriorityOnly.Value || item.Priority.Value) &&
        (item.Title.Value.Contains(Query.Value.Trim(), StringComparison.OrdinalIgnoreCase) ||
         item.Owner.Value.Contains(Query.Value.Trim(), StringComparison.OrdinalIgnoreCase) ||
         item.Area.Value.Contains(Query.Value.Trim(), StringComparison.OrdinalIgnoreCase)));

    public void Add()
    {
        var title = Draft.Value.Trim();
        if (title.Length == 0) { Activity.Value = "Give your idea a title first."; return; }
        var item = new LaunchItem(nextId++, title, "You", "PRODUCT", LaunchStage.Planned, false, "");
        Items.Add(item);
        Selected.Value = item;
        Draft.Value = "";
        Query.Value = "";
        PriorityOnly.Value = false;
        Activity.Value = $"Added LP-{item.Id} to Planned.";
    }

    public void Move(LaunchItem item, LaunchStage stage)
    {
        item.Stage.Value = stage;
        Activity.Value = $"LP-{item.Id} moved to {stage}.";
    }
}
