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
    public void FailedBuildPreservesSubscriptionsAndCanRecover() => StaTestRunner.Run(() =>
    {
        var left = new State<int>(1);
        var right = new State<int>(2);
        var useLeft = true;
        var fail = false;
        using var session = new ViewSession(() =>
        {
            var value = useLeft ? left.Value : right.Value;
            if (fail) throw new InvalidOperationException("Failed body");
            return Text(value.ToString());
        });
        var notifications = 0;
        session.Invalidated += () => notifications++;
        session.Build();
        useLeft = false;
        fail = true;
        Assert.ThrowsException<InvalidOperationException>(() => session.Build());
        left.Value++;
        right.Value++;
        Assert.AreEqual(1, notifications);
        fail = false;
        session.Build();
        notifications = 0;
        left.Value++;
        right.Value++;
        Assert.AreEqual(1, notifications);
    });

    [TestMethod]
    public void FailedComputedEvaluationPreservesDependenciesAndRecovers() => StaTestRunner.Run(() =>
    {
        var left = new State<int>(1);
        var right = new State<int>(2);
        var useLeft = true;
        var fail = false;
        var computed = new Computed<int>(() =>
        {
            var value = useLeft ? left.Value : right.Value;
            if (fail) throw new InvalidOperationException("Failed getter");
            return value;
        });
        var notifications = 0;
        Action changed = () => notifications++;
        computed.Changed += changed;
        useLeft = false;
        fail = true;
        Assert.ThrowsException<InvalidOperationException>(() => _ = computed.Value);
        right.Value++;
        Assert.AreEqual(0, notifications);
        fail = false;
        left.Value = 3; // The old subscription survives failure and triggers recovery.
        Assert.AreEqual(1, notifications);
        left.Value = 4;
        Assert.AreEqual(1, notifications);
        right.Value = 5;
        Assert.AreEqual(2, notifications);
        computed.Changed -= changed;
        right.Value = 6;
        Assert.AreEqual(2, notifications);
    });

    [TestMethod]
    public void FailedSubscriptionRollsBackNewReaders() => StaTestRunner.Run(() =>
    {
        var retained = new State<int>(1);
        var added = new State<int>(2);
        var getterCalls = 0;
        var failing = new Computed<int>(() =>
        {
            if (++getterCalls == 2) throw new InvalidOperationException("Failed attachment");
            return added.Value;
        });
        var changeBranch = false;
        using var session = new ViewSession(() =>
        {
            if (!changeBranch) return Text(retained.Value.ToString());
            _ = added.Value;
            return Text(failing.Value.ToString());
        });
        var notifications = 0;
        session.Invalidated += () => notifications++;
        session.Build();
        changeBranch = true;
        Assert.ThrowsException<InvalidOperationException>(() => session.Build());
        added.Value++;
        Assert.AreEqual(0, notifications);
        retained.Value++;
        Assert.AreEqual(1, notifications);
        session.Build();
        notifications = 0;
        retained.Value++;
        Assert.AreEqual(0, notifications);
        added.Value++;
        Assert.AreEqual(2, notifications); // Direct and computed readers both notify.
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
