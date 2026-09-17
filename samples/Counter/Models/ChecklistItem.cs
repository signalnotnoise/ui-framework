using System.Diagnostics;
using System.Windows.Threading;
using UI_Framework;

namespace StressLab;

public sealed class ChecklistItem
{
    public string Id { get; } = Guid.NewGuid().ToString("N");
    public State<StepData> Data { get; }
    public Binding<string> Title { get; }
    public Binding<bool> Done { get; }
    public ChecklistItem(string title)
    {
        Data = new(new(title, false));
        Title = Data.Binding(s => s.Title, (s, value) => s with { Title = value });
        Done = Data.Binding(s => s.Done, (s, value) => s with { Done = value });
    }
}
