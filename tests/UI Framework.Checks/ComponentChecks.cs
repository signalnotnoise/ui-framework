using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using UI_Framework;
using UI_Framework.Wpf;
using static UI_Framework.UI;

internal static class ComponentChecks
{
    private static int checks;
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
        checks++;
    }
    private static void Flush() => Dispatcher.CurrentDispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);
    private static FrameworkElement Inner(object element) => (FrameworkElement)((Border)element).Child;

    public static int Run()
    {
        var order = new StateList<string>(["a", "b"]);
        var instances = new Dictionary<string, Probe>();
        var parentBuilds = 0;
        var prop = new State<string>("first");
        var host = new ViewHost(() =>
        {
            parentBuilds++;
            var label = prop.Value;
            return VStack(order.Select(key => Component<Probe>(component =>
            {
                instances[key] = component;
                component.Label = label;
            }).Id(key)).ToArray());
        });
        var a = instances["a"];
        var b = instances["b"];
        Check(a.Mounts == 1 && b.Mounts == 1, "Each component must mount once");
        Check(a.Builds == 1 && b.Builds == 1, "Initial render must build each component once");
        a.Count.Value = 1;
        a.Count.Value = 2;
        a.Count.Value = 3;
        Flush();
        Check(a.Builds == 2, "Local changes in one dispatch turn must batch into one build");
        Check(parentBuilds == 1 && b.Builds == 1, "Local updates must skip parent and sibling bodies");

        var panel = (StackPanel)Inner(host.Content);
        var aHost = (ViewHost)Inner(panel.Children[0]);
        var input = (TextBox)Inner(((StackPanel)Inner(aHost.Content)).Children[1]);
        input.Text = "retained edit";
        Flush();
        input.Select(2, 4);
        order.Move(0, 1);
        Flush();
        Check(ReferenceEquals(a, instances["a"]) && a.Count.Value == 3, "Keyed moves must retain component-local state");
        Check(ReferenceEquals(aHost, Inner(panel.Children[1])), "Keyed moves must retain component hosts");
        Check(ReferenceEquals(input, Inner(((StackPanel)Inner(aHost.Content)).Children[1])) && input.Text == "retained edit", "Keyed moves must retain text controls and edits");
        Check(input.SelectionStart == 2 && input.SelectionLength == 4, "Unchanged text must retain selection during a move");
        Check(a.Mounts == 1 && a.Unmounts == 0, "Reorder must not remount components");

        prop.Value = "updated";
        Flush();
        var label = (TextBlock)Inner(((StackPanel)Inner(aHost.Content)).Children[0]);
        Check(label.Text == "updated: 3", "Configure must deliver fresh props to a retained instance");

        order.Remove("a");
        Flush();
        var removedBuilds = a.Builds;
        Check(a.Unmounts == 1 && a.ChildrenDetachedAtUnmount, "Removal must unmount once after child host cleanup");
        a.Count.Value = 99;
        Flush();
        Check(a.Builds == removedBuilds, "Removed component state must no longer trigger builds");
        order.Add("a");
        Flush();
        Check(!ReferenceEquals(a, instances["a"]) && instances["a"].Count.Value == 0, "Reinsertion after removal must create fresh local state");

        var remounted = instances["a"];
        remounted.Count.Value = 10;
        var beforeDispose = remounted.Builds;
        host.Dispose();
        host.Dispose();
        Flush();
        Check(remounted.Builds == beforeDispose, "Disposal must cancel queued component renders");
        Check(b.Unmounts == 1 && remounted.Unmounts == 1, "Host disposal must unmount all retained components exactly once");

        var alternate = new State<bool>(false);
        Probe? previous = null;
        OtherProbe? replacement = null;
        using (var typeHost = new ViewHost(() => alternate.Value
            ? Component<OtherProbe>(value => replacement = value).Id("same")
            : Component<Probe>(value => previous = value).Id("same")))
        {
            alternate.Value = true;
            Flush();
            Check(previous!.Unmounts == 1 && replacement!.Mounts == 1, "Changing component type must replace an instance even with the same key");
        }

        var dynamicKey = new State<string>("old");
        Probe? keyedInstance = null;
        using (var keyHost = new ViewHost(() => Component<Probe>(value => keyedInstance = value).Id(dynamicKey.Value)))
        {
            var old = keyedInstance!;
            dynamicKey.Value = "new";
            Flush();
            Check(old.Unmounts == 1 && !ReferenceEquals(old, keyedInstance), "Changing a component key must reset its lifetime");
        }

        var items = new StateList<int>([1, 2]);
        var notifications = 0;
        using (var listSession = new ViewSession(() => Text(string.Join(",", items))))
        {
            listSession.Invalidated += () => notifications++;
            listSession.Build();
            items.Add(3);
            items[0] = 4;
            items.Move(0, 2);
            Check(notifications == 3 && items.SequenceEqual([2, 3, 4]), "List mutations must notify and preserve order");
            items[0] = 2;
            items.Move(0, 0);
            items.Remove(999);
            Check(notifications == 3, "List no-ops must not notify");
            try { items.Move(0, 8); throw new Exception("Invalid move accepted"); }
            catch (ArgumentOutOfRangeException) { }
            Check(items.SequenceEqual([2, 3, 4]) && notifications == 3, "Invalid moves must leave state unchanged");
            items.RemoveAt(1);
            items.Remove(4);
            items.Clear();
            items.Clear();
            Check(notifications == 6 && items.Count == 0, "Remove and clear must notify only for actual changes");
        }
        var countNotifications = 0;
        using (var countSession = new ViewSession(() => Text(items.Count.ToString())))
        {
            countSession.Invalidated += () => countNotifications++;
            countSession.Build();
            items.Add(7);
            Check(countNotifications == 1, "Reading Count must subscribe to list changes");
        }

        var threadState = new State<int>(0);
        Exception? threadError = null;
        var worker = new Thread(() =>
        {
            try { threadState.Value = 5; }
            catch (Exception error) { threadError = error; }
        });
        worker.Start();
        worker.Join();
        Check(threadError is InvalidOperationException && threadState.Value == 0, "Cross-thread state writes must fail before mutation");
        Exception? listThreadError = null;
        worker = new Thread(() =>
        {
            try { items.Add(8); }
            catch (Exception error) { listThreadError = error; }
        });
        worker.Start();
        worker.Join();
        Check(listThreadError is InvalidOperationException && items.SequenceEqual([7]), "Cross-thread list writes must fail before mutation");

        var active = new State<bool>(true);
        var oldState = new State<int>(0);
        var newState = new State<int>(0);
        var observed = 0;
        using (var session = new ViewSession(() =>
        {
            if (active.Value) return Text(oldState.Value.ToString());
            _ = newState.Value;
            throw new InvalidOperationException("Failed build");
        }))
        {
            session.Invalidated += () => observed++;
            session.Build();
            active.Value = false;
            try { session.Build(); } catch (InvalidOperationException) { }
            var before = observed;
            newState.Value++;
            Check(observed == before, "Failed builds must not subscribe to partial dependencies");
            oldState.Value++;
            Check(observed == before + 1, "Failed builds must preserve the last successful subscriptions");
        }

        MountMutation? mounted = null;
        using (var mountHost = new ViewHost(() => Component<MountMutation>(value => mounted = value)))
        {
            Flush();
            Check(mounted!.Builds == 2 && mounted.Count.Value == 1, "OnMounted updates must schedule a local render");
        }
        var configuredState = new State<string>("before");
        Probe? configured = null;
        var configureParentBuilds = 0;
        using (var configuredHost = new ViewHost(() =>
        {
            configureParentBuilds++;
            return Component<Probe>(value => { configured = value; value.Label = configuredState.Value; });
        }))
        {
            configuredState.Value = "after";
            Flush();
            Check(configured!.Label == "after" && configured.Builds == 2 && configureParentBuilds == 1,
                "State reads in configure must be observed by the component session");
        }
        return checks;
    }






}
