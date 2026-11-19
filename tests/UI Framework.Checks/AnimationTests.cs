using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using UI_Framework;
using UI_Framework.Wpf;
using static UI_Framework.UI;

namespace UI_Framework.Checks;

[TestClass]
public sealed class AnimationTests
{
    private static readonly AnimationSpec linear = new(TimeSpan.FromSeconds(10), AnimationEasing.Linear);
    private static ViewMotion Target(double opacity = 0, double x = 10, double y = -10) =>
        new(opacity, x, y, AnimationProperties.Opacity | AnimationProperties.Translation, linear);

    [TestMethod]
    public void OrdinaryViewsAllocateNoControllerOrTransform() => StaTestRunner.Run(() =>
    {
        using var host = new ViewHost(() => Text("plain"));
        var frame = (Border)host.Content;
        host.Refresh();
        Assert.IsNull(NodeAnimation.GetController(frame));
        Assert.AreSame(Transform.Identity, frame.RenderTransform);
    });

    [TestMethod]
    public void MountAndUnloadedUpdatesSettleAtTargets() => StaTestRunner.Run(() =>
    {
        var view = Text("target").Opacity(.25).Translate(-4, 8)
            .Animate(AnimationProperties.Opacity | AnimationProperties.Translation, linear);
        using var host = new ViewHost(() => view);
        var frame = (Border)host.Content;
        Assert.AreEqual(.25, frame.Opacity);
        Assert.AreEqual(-4d, ((TranslateTransform)frame.RenderTransform).X);
        Assert.IsFalse(NodeAnimation.GetController(frame)!.HasClocks);
        view = view.Opacity(.75).Translate(12, 0);
        host.Refresh();
        Assert.AreEqual(.75, frame.Opacity);
        Assert.AreEqual(12d, ((TranslateTransform)frame.RenderTransform).X);
        Assert.IsFalse(NodeAnimation.GetController(frame)!.HasClocks);
    });

    [TestMethod]
    public void RetargetStartsAtDisplayedValueAndUnchangedRefreshDoesNotRestart() => StaTestRunner.Run(() =>
    {
        var frame = new Border();
        using var controller = new NodeAnimation(frame);
        var first = Target();
        controller.Update(null, first, true);
        controller.OpacityClock!.Controller!.SeekAlignedToLastTick(TimeSpan.FromSeconds(5), TimeSeekOrigin.BeginTime);
        controller.XClock!.Controller!.SeekAlignedToLastTick(TimeSpan.FromSeconds(5), TimeSeekOrigin.BeginTime);
        Assert.AreEqual(.5, frame.Opacity, .001);
        var clock = controller.OpacityClock;
        var differentTiming = first with { Animation = linear with { Duration = TimeSpan.FromSeconds(20) } };
        controller.Update(first, differentTiming, true);
        Assert.AreSame(clock, controller.OpacityClock);
        var next = Target(1, -20);
        var displayedX = ((TranslateTransform)frame.RenderTransform).X;
        controller.Update(differentTiming, next, true);
        Assert.AreNotSame(clock, controller.OpacityClock);
        controller.OpacityClock!.Controller!.SeekAlignedToLastTick(TimeSpan.Zero, TimeSeekOrigin.BeginTime);
        Assert.AreEqual(.5, frame.Opacity, .001);
        Assert.AreEqual(.5, ((DoubleAnimation)controller.OpacityClock!.Timeline).From!.Value, .001);
        Assert.AreEqual(displayedX, ((DoubleAnimation)controller.XClock!.Timeline).From!.Value, .001);
        Assert.AreEqual(1d, (double)frame.GetAnimationBaseValue(UIElement.OpacityProperty));
    });

    [TestMethod]
    public void RemovingOnePropertySettlesItWithoutRestartingOthers() => StaTestRunner.Run(() =>
    {
        var frame = new Border();
        using var controller = new NodeAnimation(frame);
        var first = Target();
        controller.Update(null, first, true);
        var xClock = controller.XClock;
        var next = first with { Properties = AnimationProperties.Translation };
        controller.Update(first, next, true);
        Assert.IsNull(controller.OpacityClock);
        Assert.AreEqual(0d, frame.Opacity);
        Assert.AreSame(xClock, controller.XClock);
        controller.Update(next, null, true);
        Assert.IsFalse(controller.HasClocks);
        Assert.AreEqual(1d, frame.Opacity);
        Assert.AreEqual(0d, ((TranslateTransform)frame.RenderTransform).X);
    });

    [TestMethod]
    public void ZeroDurationAndSuppressionCancelAllClocks() => StaTestRunner.Run(() =>
    {
        var frame = new Border();
        using var controller = new NodeAnimation(frame);
        var first = Target();
        controller.Update(null, first, true);
        var next = Target(.3) with { Animation = new(TimeSpan.Zero) };
        controller.Update(first, next, true);
        Assert.IsFalse(controller.HasClocks);
        Assert.AreEqual(.3, frame.Opacity);
        controller.Update(next, first, true);
        controller.Update(first, first, false);
        Assert.IsFalse(controller.HasClocks);
        Assert.AreEqual(0d, frame.Opacity);
    });

    [TestMethod]
    public void InheritedMotionSwitchAndUnloadSettleImmediately() => StaTestRunner.Run(() =>
    {
        var frame = new Border();
        var parent = new Border { Child = frame };
        using var controller = new NodeAnimation(frame);
        var first = Target();
        controller.Update(null, first, true);
        AnimationSettings.SetIsEnabled(parent, false);
        Assert.IsFalse(controller.HasClocks);
        Assert.AreEqual(0d, frame.Opacity);
        AnimationSettings.SetIsEnabled(parent, true);
        controller.Update(first, Target(1), true);
        Assert.IsTrue(controller.HasClocks);
        frame.RaiseEvent(new RoutedEventArgs(FrameworkElement.UnloadedEvent));
        Assert.IsFalse(controller.HasClocks);
        Assert.AreEqual(1d, frame.Opacity);
    });

    [TestMethod]
    public void CompletedClocksReleaseAndLeaveDestination() => StaTestRunner.Run(() =>
    {
        var frame = new Border();
        using var controller = new NodeAnimation(frame);
        controller.Update(null, Target(.2), true);
        foreach (var clock in new[] { controller.OpacityClock, controller.XClock, controller.YClock })
            clock!.Controller!.SeekAlignedToLastTick(TimeSpan.FromSeconds(11), TimeSeekOrigin.BeginTime);
        AnimationTestPump.Until(() => !controller.HasClocks);
        Assert.AreEqual(.2, frame.Opacity);
        Assert.IsFalse(frame.HasAnimatedProperties);
        Assert.IsFalse(((TranslateTransform)frame.RenderTransform).HasAnimatedProperties);
    });

    [TestMethod]
    public void RapidRetargetingKeepsOnlyCurrentClocksAndNoBodyWork() => StaTestRunner.Run(() =>
    {
        var builds = 0;
        using var host = new ViewHost(() => { builds++; return Text("stable").Opacity(1); });
        var controller = NodeAnimation.GetController((Border)host.Content)!;
        ViewMotion? previous = null;
        for (var i = 0; i < 500; i++)
        {
            var next = Target(i % 2 == 0 ? .2 : .8, i % 2 == 0 ? 10 : -10);
            controller.Update(previous, next, true);
            controller.OpacityClock!.Controller!.SeekAlignedToLastTick(TimeSpan.FromSeconds(1), TimeSeekOrigin.BeginTime);
            previous = next;
        }
        Assert.AreEqual(1, builds);
        Assert.IsTrue(controller.HasClocks);
        host.Dispose();
        Assert.IsFalse(controller.HasClocks);
    });

    [TestMethod]
    public void InvalidDirectDescriptionsDoNotPatchMountedTree() => StaTestRunner.Run(() =>
    {
        View view = Text("valid");
        using var host = new ViewHost(() => view);
        var frame = host.Content;
        foreach (var invalid in new[] { new ViewMotion(double.NaN), new ViewMotion(2), new ViewMotion(X: double.PositiveInfinity),
            new ViewMotion(Properties: (AnimationProperties)8), new ViewMotion(Animation: new(TimeSpan.FromTicks(-1))),
            new ViewMotion(Animation: new(TimeSpan.Zero, (AnimationEasing)99)) })
        {
            view = Text("invalid") with { Motion = invalid };
            Assert.ThrowsException<ArgumentOutOfRangeException>(host.Refresh);
            Assert.AreSame(frame, host.Content);
        }
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => Text("x").Opacity(-1));
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => Text("x").Translate(0, double.NaN));
    });

    [TestMethod]
    public void NativeTransformIsUntouchedAndOpacityDoesNotDisableInput() => StaTestRunner.Run(() =>
    {
        var transform = new RotateTransform(15);
        using var host = new ViewHost(() => WpfUI.Native(() => new Button { RenderTransform = transform })
            .Opacity(0).Translate(5, 10));
        TestVisualTree.Layout(host);
        var frame = (Border)host.Content;
        var button = TestVisualTree.Find<Button>(host).Single();
        Assert.AreSame(transform, button.RenderTransform);
        Assert.IsTrue(button.Focusable);
        Assert.IsTrue(frame.IsEnabled);
        Assert.IsTrue(frame.IsHitTestVisible);
    });

    [TestMethod]
    public void DisposedControllerAndFrameCollectEvenWithOldClockHeld() => StaTestRunner.Run(() =>
    {
        var (frame, controller, clock) = CreateDisposed();
        TestVisualTree.Flush();
        for (var i = 0; i < 3; i++) { GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect(); }
        Assert.IsFalse(frame.IsAlive);
        Assert.IsFalse(controller.IsAlive);
        GC.KeepAlive(clock);
    });

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static (WeakReference, WeakReference, AnimationClock) CreateDisposed()
    {
        var frame = new Border();
        var controller = new NodeAnimation(frame);
        controller.Update(null, Target(), true);
        var clock = controller.OpacityClock!;
        controller.Dispose();
        return (new(frame), new(controller), clock);
    }

    [TestMethod]
    public void RecyclingDropsMotionAndRestoresOnlyTarget() => StaTestRunner.Run(() =>
    {
        using var host = new ViewHost(() => VirtualList([Text("row").Id("row").Opacity(.4).Translate(3, 6)
            .Animate(AnimationProperties.Opacity | AnimationProperties.Translation, linear)], 200));
        var window = new Window { Content = host, Width = 300, Height = 250, ShowInTaskbar = false };
        try
        {
            window.Show(); TestVisualTree.Layout(host);
            var oldFrame = TestVisualTree.Find<Border>(host).Single(frame => NodeAnimation.GetController(frame) is not null);
            var controller = NodeAnimation.GetController(oldFrame)!;
            controller.Update(new(.4, 3, 6), Target(.1), true);
            TestVisualTree.Find<ItemsControl>(host).Single().Items.Refresh();
            TestVisualTree.Layout(host);
            Assert.IsFalse(controller.HasClocks);
            Assert.IsNull(NodeAnimation.GetController(oldFrame));
            var restored = TestVisualTree.Find<Border>(host).Single(frame => NodeAnimation.GetController(frame) is not null);
            Assert.AreNotSame(oldFrame, restored);
            Assert.AreEqual(.4, restored.Opacity);
            Assert.IsFalse(NodeAnimation.GetController(restored)!.HasClocks);
        }
        finally { window.Close(); }
    });

    [TestMethod]
    public void LoadedHostRetainsClockAcrossRefreshAndCancelsOnRemoval() => StaTestRunner.Run(() =>
    {
        var motion = Target(1, 0, 0);
        var present = true;
        using var host = new ViewHost(() => present ? Text("retained") with { Motion = motion } : Text("replacement").Id("new"));
        var window = new Window { Content = host, Width = 300, Height = 200, ShowInTaskbar = false };
        try
        {
            window.Show(); TestVisualTree.Layout(host);
            var frame = (Border)host.Content;
            var controller = NodeAnimation.GetController(frame)!;
            motion = Target(); host.Refresh();
            Assert.AreEqual(SystemParameters.ClientAreaAnimation, controller.HasClocks);
            var clock = controller.OpacityClock;
            host.Refresh();
            Assert.AreSame(clock, controller.OpacityClock);
            present = false; host.Refresh();
            Assert.IsFalse(controller.HasClocks);
            Assert.IsNull(NodeAnimation.GetController(frame));
        }
        finally { window.Close(); }
    });

    [TestMethod]
    public void OpacityOnlyDoesNotAllocateTranslationAndKeyedReorderReusesFrame() => StaTestRunner.Run(() =>
    {
        var reverse = false;
        var a = Text("a").Id("a").Opacity(.5).Animate(AnimationProperties.Opacity, linear);
        var b = Text("b").Id("b");
        using var host = new ViewHost(() => VStack(reverse ? [b, a] : [a, b]));
        TestVisualTree.Layout(host);
        var frame = TestVisualTree.Find<Border>(host).Single(border => NodeAnimation.GetController(border) is not null);
        var controller = NodeAnimation.GetController(frame);
        Assert.AreSame(Transform.Identity, frame.RenderTransform);
        reverse = true; host.Refresh(); TestVisualTree.Layout(host);
        var moved = TestVisualTree.Find<Border>(host).Single(border => NodeAnimation.GetController(border) is not null);
        Assert.AreSame(frame, moved);
        Assert.AreSame(controller, NodeAnimation.GetController(moved));
        Assert.AreEqual(.5, moved.Opacity);
    });

    [TestMethod]
    public void ChangingOnlyXDoesNotRestartOpacityOrY() => StaTestRunner.Run(() =>
    {
        var frame = new Border();
        using var controller = new NodeAnimation(frame);
        var first = Target();
        controller.Update(null, first, true);
        var opacity = controller.OpacityClock;
        var y = controller.YClock;
        var x = controller.XClock;
        controller.XClock!.Controller!.SeekAlignedToLastTick(TimeSpan.FromSeconds(5), TimeSeekOrigin.BeginTime);
        controller.Update(first, first with { X = -20 }, true);
        Assert.AreSame(opacity, controller.OpacityClock);
        Assert.AreSame(y, controller.YClock);
        Assert.AreNotSame(x, controller.XClock);
    });

    [TestMethod]
    public void TranslationChangesVisualPositionWithoutChangingMeasuredSize() => StaTestRunner.Run(() =>
    {
        var view = Text("fixed frame").Width(100).Height(30);
        using var host = new ViewHost(() => view);
        TestVisualTree.Layout(host);
        var frame = (Border)host.Content;
        var size = frame.DesiredSize;
        view = view.Translate(40, -6);
        host.Refresh(); TestVisualTree.Layout(host);
        Assert.AreSame(frame, host.Content);
        Assert.AreEqual(size, frame.DesiredSize);
        Assert.AreEqual(40d, ((TranslateTransform)frame.RenderTransform).X);
    });
}
