using UI_Framework;

public sealed class FailingBodyComponent : Component
{
    public bool Fail;
    public int Unmounts;
    public override View Body() => Fail ? throw new InvalidOperationException("child body") : UI.Text("child");
    public override void OnUnmounted() => Unmounts++;
}
