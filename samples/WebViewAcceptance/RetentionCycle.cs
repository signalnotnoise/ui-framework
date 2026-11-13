namespace WebViewAcceptance;

internal sealed class RetentionCycle
{
    internal readonly Dictionary<string, WeakReference> References = [];
    internal object? DeliberateRoot;
    internal object Evidence = new();
    internal void Track(string name, object value) => References.Add(name, new WeakReference(value));
    internal string[] Survivors() => References.Where(pair => pair.Value.IsAlive).Select(pair => pair.Key).ToArray();
}
