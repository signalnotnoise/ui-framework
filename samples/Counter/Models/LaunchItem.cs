using UI_Framework;

namespace StressLab;

public sealed class LaunchItem(int id, string title, string owner, string area, LaunchStage stage, bool priority, string notes)
{
    public int Id { get; } = id;
    public State<string> Title { get; } = new(title);
    public State<string> Owner { get; } = new(owner);
    public State<string> Area { get; } = new(area);
    public State<LaunchStage> Stage { get; } = new(stage);
    public State<bool> Priority { get; } = new(priority);
    public State<string> Notes { get; } = new(notes);
}
