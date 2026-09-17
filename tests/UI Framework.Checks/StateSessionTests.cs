using Microsoft.VisualStudio.TestTools.UnitTesting;
using UI_Framework;
using static UI_Framework.UI;

[TestClass]
public sealed class StateSessionTests
{
    [TestMethod]
    public void StateSuppressesEqualWrites() => StaTestRunner.Run(() =>
    {
        var state = new State<int>(1);
        var notifications = 0;
        state.Changed += () => notifications++;
        state.Value = 1;
        Assert.AreEqual(0, notifications);
        state.Value = 2;
        Assert.AreEqual(1, notifications);
    });

    [TestMethod]
    public void SessionTracksOnlyReadState() => StaTestRunner.Run(() =>
    {
        var read = new State<int>(1);
        var ignored = new State<int>(2);
        var notifications = 0;
        using var session = new ViewSession(() => Text(read.Value.ToString()));
        session.Invalidated += () => notifications++;
        Assert.AreEqual("1", session.Build().Content);
        ignored.Value++;
        Assert.AreEqual(0, notifications);
        read.Value++;
        Assert.AreEqual(1, notifications);
    });

    [TestMethod]
    public void ConditionalReadsReplaceSubscriptions() => StaTestRunner.Run(() =>
    {
        var left = new State<int>(1);
        var right = new State<int>(2);
        var useLeft = new State<bool>(true);
        var notifications = 0;
        using var session = new ViewSession(() => Text((useLeft.Value ? left.Value : right.Value).ToString()));
        session.Invalidated += () => notifications++;
        session.Build();
        useLeft.Value = false;
        session.Build();
        notifications = 0;
        left.Value++;
        Assert.AreEqual(0, notifications);
        right.Value++;
        Assert.AreEqual(1, notifications);
    });

    [TestMethod]
    public void SessionDisposalDetachesAndRejectsBuilds() => StaTestRunner.Run(() =>
    {
        var state = new State<int>(0);
        var notifications = 0;
        var session = new ViewSession(() => Text(state.Value.ToString()));
        session.Invalidated += () => notifications++;
        session.Build();
        session.Dispose();
        session.Dispose();
        state.Value++;
        Assert.AreEqual(0, notifications);
        Assert.ThrowsException<ObjectDisposedException>(() => session.Build());
    });
}
