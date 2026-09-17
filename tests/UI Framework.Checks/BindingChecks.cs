using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using UI_Framework;
using UI_Framework.Wpf;
using static UI_Framework.UI;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

internal static class BindingChecks
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
        var state = new State<string>("initial");
        var binding = state.Binding();
        binding.Value = "edited";
        Check(state.Value == "edited", "Direct binding must write into source state");
        state.Value = "external";
        Check(binding.Value == "external", "Binding reads must be live");
        var notifications = 0;
        using (var session = new ViewSession(() => Text(binding.Value)))
        {
            session.Invalidated += () => notifications++;
            session.Build();
            binding.Value = "external";
            Check(notifications == 0, "Equal binding writes must not invalidate state");
            binding.Value = "next";
            Check(notifications == 1, "Reads through bindings must track dependencies");
        }

        var profile = new State<Profile>(new("Alex", 1, new(false, "light")));
        var name = profile.Binding(p => p.Name, (p, value) => p with { Name = value });
        var enabled = profile.Binding(p => p.Preferences, (p, value) => p with { Preferences = value })
            .Select(p => p.Enabled, (p, value) => p with { Enabled = value });
        profile.Value = profile.Value with { Revision = 9, Preferences = new(false, "dark") };
        name.Value = "Sam";
        enabled.Value = true;
        Check(profile.Value == new Profile("Sam", 9, new(true, "dark")), "Nested projections must preserve latest unrelated fields");
        var projectedNotifications = 0;
        using (var session = new ViewSession(() => Text(enabled.Value.ToString())))
        {
            session.Invalidated += () => projectedNotifications++;
            session.Build();
            enabled.Value = false;
            Check(projectedNotifications == 1, "Nested projected reads must subscribe to the source state");
        }

        var edits = 0;
        var custom = new Binding<string>(() => state.Value, value => { edits++; state.Value = value.Trim().ToUpperInvariant(); });
        using (var host = new ViewHost(() => VStack(TextField(custom), TextField(state.Binding()))))
        {
            var panel = (StackPanel)Inner(host.Content);
            var first = (TextBox)Inner(panel.Children[0]);
            var second = (TextBox)Inner(panel.Children[1]);
            first.Text = " shared ";
            Flush();
            Check(state.Value == "SHARED" && first.Text == "SHARED" && second.Text == "SHARED", "Custom transformations must reconcile both bound controls");
            Check(edits == 1, "Programmatic text reconciliation must not feed back into a setter");
            first.Text = " SHARED ";
            Flush();
            Check(first.Text == "SHARED" && edits == 2, "Normalization must update the control even when source state is unchanged");
            state.Value = "outside";
            Flush();
            Check(first.Text == "outside" && second.Text == "outside" && edits == 2, "External changes must update controls without invoking their setter");
        }

        var rejected = new Binding<string>(() => state.Value, _ => { });
        using (var host = new ViewHost(() => TextField(rejected)))
        {
            var input = (TextBox)Inner(host.Content);
            input.Text = "rejected";
            Check(input.Text == state.Value, "Rejected text edits must restore the accepted value");
        }

        var left = new State<string>("left");
        var right = new State<string>("right");
        var useRight = new State<bool>(false);
        using (var host = new ViewHost(() => TextField(useRight.Value ? right.Binding() : left.Binding())))
        {
            var input = (TextBox)Inner(host.Content);
            useRight.Value = true;
            Flush();
            input.Text = "right edited";
            Flush();
            Check(left.Value == "left" && right.Value == "right edited" && ReferenceEquals(input, Inner(host.Content)), "Retained controls must write through the latest binding");
            left.Value = "old source";
            Flush();
            Check(input.Text == "right edited", "Old binding source must no longer drive the control");
        }

        var flag = new State<bool>(false);
        var toggleWrites = 0;
        var flagBinding = new Binding<bool>(() => flag.Value, value => { toggleWrites++; flag.Value = value; });
        using (var host = new ViewHost(() => Toggle("Enabled", flagBinding)))
        {
            var toggle = (CheckBox)Inner(host.Content);
            toggle.IsChecked = true;
            Flush();
            Check(flag.Value && toggleWrites == 1, "Toggle edits must update a binding once");
            flag.Value = false;
            Flush();
            Check(toggle.IsChecked == false && toggleWrites == 1, "Programmatic toggle updates must not write back");
        }
        using (var host = new ViewHost(() => Toggle("Rejected", new Binding<bool>(() => flag.Value, _ => { }))))
        {
            var toggle = (CheckBox)Inner(host.Content);
            toggle.IsChecked = true;
            Check(toggle.IsChecked == false && !flag.Value, "Rejected toggle edits must restore the source value");
        }

        var bulk = new StateList<int>([1, 2, 3]);
        var changed = 0;
        bulk.Changed += () => changed++;
        bulk.ReplaceAll(bulk.Reverse());
        Check(bulk.SequenceEqual([3, 2, 1]) && changed == 1, "Bulk replacement must enumerate a snapshot and notify once");
        bulk.ReplaceAll(bulk);
        Check(changed == 1, "Equal replacement must not notify");
        IEnumerable<int> FailingSequence() { yield return 4; throw new InvalidOperationException("Enumeration failure"); }
        try { bulk.ReplaceAll(FailingSequence()); } catch (InvalidOperationException) { }
        Check(bulk.SequenceEqual([3, 2, 1]) && changed == 1, "Failed bulk enumeration must leave the collection unchanged");

        var builds = 0;
        using (var host = new ViewHost(() => { builds++; return TextField(state.Binding()); }))
        {
            for (var i = 0; i < 10000; i++) binding.Value = i.ToString();
            Flush();
            Check(builds == 2 && ((TextBox)Inner(host.Content)).Text == "9999", "Ten thousand writes must coalesce into one render with the final value");
        }
        Exception? threadError = null;
        var worker = new Thread(() => { try { name.Value = "wrong thread"; } catch (Exception error) { threadError = error; } });
        worker.Start();
        worker.Join();
        Check(threadError is InvalidOperationException && profile.Value.Name == "Sam", "Projected bindings must enforce source-state thread ownership");

        var scrollState = new State<string>("scroll");
        using (var host = new ViewHost(() => Scroll(TextField(scrollState)).Height(200).Width(300)))
        {
            var scroll = (ScrollViewer)Inner(host.Content);
            var input = (TextBox)Inner(scroll.Content);
            scrollState.Value = "updated";
            Flush();
            Check(ReferenceEquals(input, Inner(scroll.Content)) && input.Text == "updated", "Scroll containers must retain and update their child");
        }
        return checks;
    }
}
