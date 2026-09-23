using Microsoft.VisualStudio.TestTools.UnitTesting;
using UI_Framework;

[TestClass]
public sealed class StateNotificationTests
{
    [TestMethod]
    public void CustomStateCannotNotifyFromAnotherThread()
    {
        var state = new NotificationProbe();
        var notified = false;
        state.Changed += () => notified = true;
        Task.Run(() => Assert.ThrowsException<InvalidOperationException>(state.Notify)).GetAwaiter().GetResult();
        Assert.IsFalse(notified);
        state.Notify();
        Assert.IsTrue(notified);
    }

    [TestMethod]
    public void ThrowingObserversDoNotStarveOtherSessions()
    {
        var state = new State<int>(0);
        var first = new InvalidOperationException("first");
        state.Changed += () => throw first;
        using var session = new ViewSession(() => UI.Text(state.Value.ToString()));
        session.Build();
        var invalidations = 0;
        session.Invalidated += () => invalidations++;
        Assert.AreSame(first, Assert.ThrowsException<InvalidOperationException>(() => state.Value = 1));
        Assert.AreEqual(1, invalidations);
        state.Changed += () => throw new ArgumentException("last");
        var errors = Assert.ThrowsException<AggregateException>(() => state.Value = 2);
        Assert.AreEqual(2, errors.InnerExceptions.Count);
        Assert.AreEqual(2, invalidations);
        Assert.AreEqual("2", session.Build().Content);
    }

    [TestMethod]
    public void ComputedNotificationsAlsoReachLaterObservers()
    {
        var state = new State<int>(0);
        var computed = new Computed<int>(() => state.Value * 2);
        Action throwing = () => throw new InvalidOperationException("computed observer");
        var notifications = 0;
        Action observing = () => notifications++;
        computed.Changed += throwing;
        computed.Changed += observing;
        try
        {
            Assert.ThrowsException<InvalidOperationException>(() => state.Value = 1);
            Assert.AreEqual(1, notifications);
            Assert.AreEqual(2, computed.Value);
        }
        finally { computed.Changed -= throwing; computed.Changed -= observing; }
    }

    [TestMethod]
    public void SnapshotEnumerationSurvivesMutation()
    {
        var list = new StateList<int>([1, 2, 3]);
        using var values = list.GetEnumerator();
        list.ReplaceAll([4, 5]);
        var captured = new List<int>();
        while (values.MoveNext()) captured.Add(values.Current);
        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, captured);
        CollectionAssert.AreEqual(new[] { 4, 5 }, list.ToArray());
    }
}
