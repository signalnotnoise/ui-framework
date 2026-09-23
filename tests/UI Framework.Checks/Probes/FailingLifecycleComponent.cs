using UI_Framework;

public sealed class FailingLifecycleComponent : Component
{
    public override View Body() => UI.Text("lifecycle failure");
    public override void OnMounted() => throw new InvalidOperationException("mount failure");
    public override void OnUnmounted() => throw new ArgumentException("unmount failure");
}
