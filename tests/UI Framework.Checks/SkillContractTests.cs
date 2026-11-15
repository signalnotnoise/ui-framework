using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using UI_Framework;
using UI_Framework.Wpf;
using static UI_Framework.UI;

[TestClass]
public sealed class SkillContractTests
{
    [TestMethod]
    public void FieldStateSurvivesBuildsWithoutAccumulatingSubscriptions() => StaTestRunner.Run(() =>
    {
        var example = new SkillStateExample();
        using var session = new ViewSession(example.Body);
        var notifications = 0;
        session.Invalidated += () => notifications++;
        session.Build();
        example.SetFilter(" warning ");
        Assert.AreEqual(2, notifications); // Direct field and derived value are both observed.
        Assert.AreEqual("warning", session.Build().Children[1].Content);
        session.Build();
        notifications = 0;
        example.SetFilter(" error ");
        Assert.AreEqual(2, notifications);
        Assert.AreEqual("error", session.Build().Children[1].Content);
        notifications = 0;
        example.SetFilter("error"); // Only the raw field changes; the computed result is equal.
        Assert.AreEqual(1, notifications);
        session.Dispose();
        notifications = 0;
        example.SetFilter("info");
        Assert.AreEqual(0, notifications);
    });

    [TestMethod]
    public void DockAndSplitExampleFillsFiniteViewport() => StaTestRunner.Run(() =>
    {
        var example = new SkillLayoutExample();
        using var host = new ViewHost(example.Body);
        host.Measure(new Size(900, 600));
        host.Arrange(new Rect(0, 0, 900, 600));
        host.UpdateLayout();
        var sources = TestVisualTree.Find<TextBlock>(host).Single(x => x.Text == "Sources");
        var message = TestVisualTree.Find<TextBlock>(host).Single(x => x.Text == "Message");
        var severity = TestVisualTree.Find<TextBlock>(host).Single(x => x.Text == "Severity");
        Assert.AreEqual(32, sources.TranslatePoint(new Point(), host).Y, 0.1);
        Assert.IsTrue(message.TranslatePoint(new Point(), host).X >= 240);
        Assert.AreEqual(2 * severity.ActualWidth, message.ActualWidth, 0.1);
    });

    [TestMethod]
    public void StackChildrenDoNotDistributeSpaceByFlexWeight() => StaTestRunner.Run(() =>
    {
        using var host = new ViewHost(() => HStack(Text("same").Flex(2), Text("same")));
        TestVisualTree.Layout(host);
        var labels = TestVisualTree.Find<TextBlock>(host).ToArray();
        Assert.AreEqual(labels[0].ActualWidth, labels[1].ActualWidth, 0.1);
        Assert.IsTrue(labels.Sum(x => x.ActualWidth) < 500);
    });

    [TestMethod]
    public void ReplaceAllRetainsKeyedEditorsAndRemovalResetsState() => StaTestRunner.Run(() =>
    {
        var example = new SkillRowsExample();
        using var host = new ViewHost(example.Body);
        TestVisualTree.Layout(host);
        var original = TestVisualTree.Find<TextBox>(host).Single(x => AutomationProperties.GetName(x) == "log-a");
        original.Text = "local edit";
        example.ReplaceRows("log-b", "log-a");
        TestVisualTree.Layout(host);
        var retained = TestVisualTree.Find<TextBox>(host).Single(x => AutomationProperties.GetName(x) == "log-a");
        Assert.AreSame(original, retained);
        Assert.AreEqual("local edit", retained.Text);
        example.ReplaceRows("log-b");
        TestVisualTree.Layout(host);
        example.ReplaceRows("log-b", "log-a");
        TestVisualTree.Layout(host);
        var fresh = TestVisualTree.Find<TextBox>(host).Single(x => AutomationProperties.GetName(x) == "log-a");
        Assert.AreNotSame(original, fresh);
        Assert.AreEqual("", fresh.Text);
    });
}
