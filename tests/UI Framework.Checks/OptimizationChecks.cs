using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using UI_Framework;
using UI_Framework.Wpf;
using static UI_Framework.UI;

internal static class OptimizationChecks
{

    private static int checks;
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
        checks++;
    }
    private static void Flush() => Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);

    public static int Run()
    {
        var record = new State<Pair>(new(1, 2));
        var leftBinding = record.Binding(p => p.Left, (p, value) => p with { Left = value });
        var invalidated = 0;
        using (var session = new ViewSession(() => Text(leftBinding.Value.ToString())))
        {
            session.Invalidated += () => invalidated++;
            session.Build();
            record.Value = record.Value with { Right = 99 };
            Check(invalidated == 0, "Unrelated record fields must not invalidate a projected reader");
            leftBinding.Value = 5;
            Check(invalidated == 1 && record.Value.Right == 99, "Changed projection must notify and preserve unrelated fields");
        }

        // Reading another projection while processing a notification must not erase
        // that projection's own pending change notification (a dependency diamond).
        var pair = new State<Pair>(new(1, 2));
        var left = pair.Select(p => p.Left);
        var right = pair.Select(p => p.Right);
        var sum = new Computed<int>(() => left.Value + right.Value);
        var sums = 0;
        var rights = 0;
        Action sumChanged = () => sums++;
        Action rightChanged = () => rights++;
        sum.Changed += sumChanged;
        right.Changed += rightChanged;
        pair.Value = new(3, 4);
        Check(sum.Value == 7 && sums == 1, "A derived diamond must publish the final combined value once");
        Check(rights == 1, "A read by another derived value must not swallow a sibling notification");
        sum.Changed -= sumChanged;
        right.Changed -= rightChanged;

        var branch = new State<bool>(true);
        var a = new State<int>(1);
        var b = new State<int>(1);
        var evaluations = 0;
        var chosen = new Computed<int>(() => { evaluations++; return branch.Value ? a.Value : b.Value; });
        Check(chosen.Value == 1, "Unobserved derived reads must return a current value");
        var before = evaluations;
        a.Value = 2;
        Check(evaluations == before, "An unobserved computed value must not retain source subscriptions");
        a.Value = 1;
        var chosenChanges = 0;
        using (var session = new ViewSession(() => Text(chosen.Value.ToString())))
        {
            session.Invalidated += () => chosenChanges++;
            session.Build();
            branch.Value = false;
            Check(chosenChanges == 0, "Equal derived results must not invalidate readers");
            before = evaluations;
            a.Value = 9;
            Check(evaluations == before, "Derived conditional dependencies must detach the old branch");
            b.Value = 2;
            Check(chosenChanges == 1, "Derived conditional dependencies must attach the new branch even after an equal result");
        }
        before = evaluations;
        b.Value = 3;
        Check(evaluations == before, "Disposing the final observer must detach the computed value from sources");

        var source = new State<int>(0);
        var parity = source.Select(value => value % 2);
        var builds = 0;
        using (var host = new ViewHost(() => { builds++; return Text(parity.Value.ToString()); }))
        {
            source.Value = 2;
            Flush();
            Check(builds == 1, "Equal selector results must skip body builds");
            source.Value = 3;
            Flush();
            Check(builds == 2, "Changed selector results must still rebuild a reader");
        }

        var selected = new State<int>(0);
        var selectors = Enumerable.Range(0, 1000).Select(id => selected.Select(value => value == id)).ToArray();
        var selectedChanges = 0;
        Action change = () => selectedChanges++;
        foreach (var selector in selectors) selector.Changed += change;
        selected.Value = 500;
        Check(selectedChanges == 2, "Changing selection among 1000 rows must notify only the old and new selected rows");
        foreach (var selector in selectors) selector.Changed -= change;

        var parent = new State<int>(0);
        var props = new State<int>(1);
        MemoProbe? probe = null;
        using (var host = new ViewHost(() =>
        {
            _ = parent.Value;
            var snapshot = props.Value;
            return Component<MemoProbe>(value => { probe = value; value.Prop = snapshot; }).Memo(snapshot);
        }))
        {
            parent.Value++;
            Flush();
            Check(probe!.Builds == 1, "Unchanged memo inputs must skip a parent-driven child build");
            probe.Local.Value++;
            Flush();
            Check(probe.Builds == 2, "Memoization must not block local state updates");
            props.Value = 2;
            Flush();
            Check(probe.Prop == 2 && probe.Builds == 3, "Changed memo inputs must deliver new props");
            parent.Value++;
            probe.Local.Value++;
            Flush();
            Check(probe.Builds == 4, "A dirty memoized child must render once when its parent also updates");
        }
        before = probe!.Builds;
        probe.Local.Value++;
        Flush();
        Check(probe.Builds == before, "Memoized components must still detach on disposal");

        var callbackState = new State<int>(1);
        var clicked = 0;
        using (var host = new ViewHost(() =>
        {
            var snapshot = callbackState.Value;
            return Component<CallbackProbe>(value => value.Click = () => clicked = snapshot).Memo(snapshot);
        }))
        {
            callbackState.Value = 2;
            Flush();
            var componentHost = (ViewHost)((Border)host.Content).Child;
            var button = (Button)((Border)componentHost.Content).Child;
            button.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Button.ClickEvent));
            Check(clicked == 2, "Changing a callback input must refresh the retained component's handler");
        }

        var ids = new StateList<int>(Enumerable.Range(0, 1000));
        var rows = new Dictionary<int, MemoProbe>();
        using (var host = new ViewHost(() => VStack(ids.Select(id => Component<MemoProbe>(value => { rows[id] = value; value.Prop = id; }).Id(id.ToString()).Memo(id)).ToArray())))
        {
            var original = rows[0];
            ids.ReplaceAll(ids.Reverse());
            Flush();
            Check(rows.Values.Sum(row => row.Builds) == 1000, "Reordering 1000 unchanged keyed components must not rebuild their bodies");
            Check(ReferenceEquals(original, rows[0]), "Memoization must preserve keyed component identity");
            rows[42].Local.Value++;
            Flush();
            Check(rows.Values.Sum(row => row.Builds) == 1001, "One memoized row edit must update just that row");
            ids.Remove(42);
            Flush();
            before = rows[42].Builds;
            rows[42].Local.Value++;
            Flush();
            Check(rows[42].Builds == before, "Removed memoized rows must release subscriptions");
        }
        try { Text("invalid").Memo(1); throw new Exception("Memo accepted a primitive view"); }
        catch (InvalidOperationException) { checks++; }

        var selectedId = new State<int>(0);
        var rowId = new State<int>(0);
        SelectionProbe? selectionProbe = null;
        using (var host = new ViewHost(() =>
        {
            var id = rowId.Value;
            return Component<SelectionProbe>(value => { selectionProbe = value; value.Selected = selectedId; value.Id = id; }).Memo(id);
        }))
        {
            rowId.Value = 1;
            Flush();
            Check(!selectionProbe!.RenderedSelection, "A derived value must reflect replacement props on a retained component");
            selectedId.Value = 1;
            Flush();
            Check(selectionProbe.RenderedSelection && selectionProbe.Builds == 3, "A prop-driven derived read must refresh its notification baseline for later source changes");
        }

        var property = "before";
        var custom = new Computed<string>(() => property);
        using (var session = new ViewSession(() => Text(custom.Value)))
        {
            session.Build();
            property = "after";
            Check(session.Build().Content == "after", "Explicit builds must refresh derived getters that read non-observable inputs");
        }
        Computed<int>? cyclic = null;
        cyclic = new(() => cyclic!.Value);
        try { _ = cyclic.Value; throw new Exception("Computed cycle accepted"); }
        catch (InvalidOperationException) { checks++; }
        return checks;
    }






}
