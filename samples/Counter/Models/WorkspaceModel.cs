using System.Diagnostics;
using System.Windows.Threading;
using UI_Framework;

namespace StressLab;

public sealed class WorkspaceModel : IDisposable
{
    public State<ProjectSettings> Settings { get; } = new(new("Framework stress lab", "UI team", new(false, true)));
    public Binding<string> ProjectName { get; }
    public Binding<string> ProjectOwner { get; }
    public Binding<bool> CompactRows { get; }
    public Binding<bool> ShowChecklist { get; }
    public StateList<WorkItem> Items { get; } = new();
    public State<WorkItem?> Selected { get; } = new(null);
    public State<string> Query { get; } = new("");
    public State<bool> OnlyOpen { get; } = new(false);
    public State<bool> BoardMounted { get; } = new(true);
    public State<bool> Virtualized { get; } = new(false);
    public State<int> Depth { get; } = new(4);
    public StateList<string> Activity { get; } = new();
    public State<string> RunStatus { get; } = new("Ready. Load a size, edit a row, or start a mixed run.");
    public State<VisualStressState> VisualStress { get; } = new(new(false, 0, 60, "Press Visual stress to watch live edits, reordering, and layout changes."));
    public Counters Counters { get; } = new();
    public bool TelemetryEnabled { get; init; } = true;
    public int StepsCompleted { get; private set; }
    private readonly Random random = new(1729);
    private readonly DispatcherTimer timer;
    private readonly Stopwatch campaign = new();
    private int nextId = 1;
    private int stepsRemaining;
    private bool disposed;
    private bool visualMode;
    private WorkItem? visualFocus;

    public WorkspaceModel(int initialCount = 50)
    {
        ProjectName = Settings.Binding(s => s.Name, (s, value) => s with { Name = value });
        ProjectOwner = Settings.Binding(s => s.Owner, (s, value) => s with { Owner = value });
        var preferences = Settings.Binding(s => s.Preferences, (s, value) => s with { Preferences = value });
        CompactRows = preferences.Select(s => s.CompactRows, (s, value) => s with { CompactRows = value });
        ShowChecklist = preferences.Select(s => s.ShowChecklist, (s, value) => s with { ShowChecklist = value });
        timer = new DispatcherTimer(DispatcherPriority.Background) { Interval = TimeSpan.FromMilliseconds(80) };
        timer.Tick += Tick;
        Load(initialCount);
    }

    public void Load(int count)
    {
        if (count < 0 || count > 1000) throw new ArgumentOutOfRangeException(nameof(count));
        Stop();
        Query.Value = "";
        OnlyOpen.Value = false;
        Items.ReplaceAll(Enumerable.Range(0, count).Select(_ => new WorkItem(nextId++)));
        Selected.Value = Items.FirstOrDefault();
        Log($"Loaded {count:N0} work items; each owns three checklist items.");
    }

    public void Add()
    {
        if (Items.Count >= 1000) { Log("At the demo limit of 1,000 rows. Remove a row before adding."); return; }
        var item = new WorkItem(nextId++);
        Items.Add(item);
        Selected.Value = item;
    }

    public void Remove(WorkItem item)
    {
        Items.Remove(item);
        if (ReferenceEquals(Selected.Value, item)) Selected.Value = Items.FirstOrDefault();
    }

    public void Shuffle()
    {
        var shuffled = Items.ToArray();
        for (var i = shuffled.Length - 1; i > 0; i--)
        {
            var j = random.Next(i + 1);
            (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
        }
        Items.ReplaceAll(shuffled);
    }

    public void MoveFirstToLast() { if (Items.Count > 1) Items.Move(0, Items.Count - 1); }

    public void Burst(int mutations = 10000)
    {
        if (Items.Count == 0) Add();
        var target = Items[0];
        for (var i = 0; i < mutations; i++) target.Notes.Value = $"Burst mutation {i + 1:N0}";
        Log($"{mutations:N0} writes queued in one event. Watch builds after the event completes.");
    }

    public void Start()
    {
        if (disposed) return;
        Stop();
        timer.Interval = TimeSpan.FromMilliseconds(80);
        StepsCompleted = 0;
        stepsRemaining = 200;
        campaign.Restart();
        RunStatus.Value = "Running 200 mixed operations… Stop remains available between steps.";
        timer.Start();
    }

    public void StartVisualStress()
    {
        if (disposed) return;
        Stop();
        if (Items.Count == 0) Add();
        BoardMounted.Value = true;
        Query.Value = "";
        OnlyOpen.Value = false;
        CompactRows.Value = false;
        ShowChecklist.Value = true;
        StepsCompleted = 0;
        stepsRemaining = 60;
        visualMode = true;
        VisualStress.Value = new(true, 0, 60, "Starting visible stress…");
        timer.Interval = TimeSpan.FromMilliseconds(350);
        campaign.Restart();
        AdvanceRun(); // The first click produces a visible change immediately.
        timer.Start();
    }

    public void Stop()
    {
        timer.Stop();
        if (stepsRemaining > 0) RunStatus.Value = $"Stopped after {StepsCompleted:N0} operations.";
        if (visualMode)
        {
            VisualStress.Value = VisualStress.Value with { Running = false, Action = $"Stopped at {StepsCompleted}/60. Press Visual stress to run again." };
            EndVisualFocus();
        }
        visualMode = false;
        stepsRemaining = 0;
        campaign.Stop();
    }

    private void Tick(object? sender, EventArgs args) => AdvanceRun();

    internal void AdvanceRun()
    {
        if (disposed || stepsRemaining <= 0) return;
        if (visualMode)
        {
            var action = VisualStep(StepsCompleted);
            StepsCompleted++;
            stepsRemaining--;
            VisualStress.Value = new(stepsRemaining > 0, StepsCompleted, 60, action);
            RunStatus.Value = $"Visual stress {StepsCompleted}/60 · {action} · {Items.Count:N0} rows";
            if (stepsRemaining == 0)
            {
                timer.Stop();
                campaign.Stop();
                visualMode = false;
                EndVisualFocus();
                OnlyOpen.Value = false;
                VisualStress.Value = new(false, 60, 60, $"Complete in {campaign.Elapsed.TotalSeconds:F1}s. All 60 visible steps finished.");
                RunStatus.Value = VisualStress.Value.Action;
                Log("Visual stress complete: live edits, selection, reorder, layouts, and filters.");
            }
            return;
        }
        MixedStep(StepsCompleted);
        StepsCompleted++;
        stepsRemaining--;
        if (stepsRemaining == 0)
        {
            timer.Stop();
            campaign.Stop();
            Query.Value = "";
            OnlyOpen.Value = false;
            RunStatus.Value = $"Completed 200 operations in {campaign.Elapsed.TotalSeconds:F1}s (includes timer pacing).";
            Log("Mixed run complete. Inspect rows and their shared editor.");
        }
        else RunStatus.Value = $"Mixed operation {StepsCompleted}/200 · {Items.Count:N0} rows";
    }

    private string VisualStep(int step)
    {
        if (Items.Count == 0) Add();
        var item = Items[(step / 8) % Math.Min(Items.Count, 3)];
        if (!ReferenceEquals(visualFocus, item))
        {
            EndVisualFocus();
            visualFocus = item;
            item.StressExpanded.Value = true;
        }
        Selected.Value = item;
        switch (step % 8)
        {
            case 0:
                OnlyOpen.Value = false;
                item.Title.Value = $"Live edit {step + 1:00} — watch this row and its inspector";
                return "Editing a row title and its shared inspector";
            case 1:
                item.Done.Value = !item.Done.Value;
                if (item.Checklist.Count > 0) item.Checklist[0].Done.Value = !item.Checklist[0].Done.Value;
                return "Toggling completion and a nested checklist";
            case 2:
                item.Owner.Value = step % 16 < 8 ? "Blue team" : "Green team";
                return "Changing selection and the bound owner field";
            case 3:
                var position = Array.IndexOf(Items.ToArray(), item);
                Items.Move(position, position == 0 ? Math.Min(2, Items.Count - 1) : 0);
                return "Moving a keyed row while preserving its controls";
            case 4:
                CompactRows.Value = !CompactRows.Value;
                return "Switching the entire board between compact and expanded layout";
            case 5:
                item.Notes.Value = $"Live nested record update #{step + 1}";
                if (item.Checklist.Count > 0) item.Checklist[0].Title.Value = $"Live checklist edit #{step + 1}";
                return "Editing nested record and checklist bindings";
            case 6:
                foreach (var row in Items.Take(8))
                {
                    row.Title.Value = $"Visible pulse {step + 1:00} / work item {row.Id}";
                    row.Done.Value = !row.Done.Value;
                }
                return "Updating eight rows together in one batched event";
            default:
                OnlyOpen.Value = true;
                return "Filtering completed rows out of the mounted board";
        }
    }

    private void EndVisualFocus()
    {
        if (visualFocus is not null) visualFocus.StressExpanded.Value = false;
        visualFocus = null;
    }

    // Deterministic operation kinds; the random generator has a fixed initial seed.
    public void MixedStep(int step)
    {
        if (Items.Count == 0) Add();
        var item = Items[random.Next(Items.Count)];
        switch (step % 10)
        {
            case 0: item.Title.Value = $"Edited work item {item.Id} · step {step}"; break;
            case 1: item.Done.Value = !item.Done.Value; break;
            case 2: MoveFirstToLast(); break;
            case 3: item.Notes.Value = $"Nested binding edit {step}"; break;
            case 4: Selected.Value = item; break;
            case 5: if (Items.Count > 1) Remove(item); else Add(); break;
            case 6: Add(); break;
            case 7: CompactRows.Value = !CompactRows.Value; break;
            case 8: item.Checklist[0].Done.Value = !item.Checklist[0].Done.Value; break;
            case 9: OnlyOpen.Value = !OnlyOpen.Value; break;
        }
    }

    public void Log(string text)
    {
        Activity.Add(text);
        while (Activity.Count > 8) Activity.RemoveAt(0);
    }

    public void Dispose()
    {
        if (disposed) return;
        Stop();
        disposed = true;
        timer.Tick -= Tick;
    }
}
