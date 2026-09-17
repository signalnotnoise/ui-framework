using System.Diagnostics;
using System.Windows.Threading;
using UI_Framework;

namespace StressLab;

public sealed class WorkItem
{
    public State<bool> StressExpanded { get; } = new(false);
    public int Id { get; }
    public State<WorkData> Data { get; }
    public Binding<string> Title { get; }
    public Binding<string> Owner { get; }
    public Binding<bool> Done { get; }
    public Binding<string> Notes { get; }
    public Binding<string> Estimate { get; }
    public StateList<ChecklistItem> Checklist { get; }

    public WorkItem(int id)
    {
        Id = id;
        string[] areas = ["Design review", "API contract", "Binding audit", "Render pipeline", "Keyboard flow", "Release checklist"];
        Data = new(new($"{areas[id % areas.Length]} · {id:0000}", id % 2 == 0 ? "Alex" : "Sam", id % 5 == 0,
            new("Edit here or in the inspector. Both views share the same record.", "3")));
        Title = Data.Binding(s => s.Title, (s, value) => s with { Title = value });
        Owner = Data.Binding(s => s.Owner, (s, value) => s with { Owner = value });
        Done = Data.Binding(s => s.Done, (s, value) => s with { Done = value });
        var details = Data.Binding(s => s.Details, (s, value) => s with { Details = value });
        Notes = details.Select(s => s.Notes, (s, value) => s with { Notes = value });
        Estimate = details.Select(s => s.Estimate, (s, value) => s with { Estimate = value });
        Checklist = new([new("Define acceptance criteria"), new("Exercise edge cases"), new("Review the result")]);
    }
}
