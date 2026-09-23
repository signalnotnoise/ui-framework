using UI_Framework;

public sealed class ThrowOnMounted : Component
{
    public static bool ShouldThrow;
    public static int Unmounts;

    public override View Body() => UI.Text("throwing component");

    public override void OnMounted()
    {
        if (ShouldThrow) throw new InvalidOperationException("component mount failed");
    }

    public override void OnUnmounted() => Unmounts++;
}
