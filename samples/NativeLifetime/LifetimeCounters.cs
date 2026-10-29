using UI_Framework;

namespace NativeLifetime;

internal sealed class LifetimeCounters
{
    internal State<int> Publisher { get; } = new(0);
    internal List<(string Kind, WeakReference Reference)> References { get; } = [];
    internal List<object> DeliberateRoots { get; } = [];
    internal List<string> Errors { get; } = [];
    internal int Created, Released, BufferCreates, BufferReleases, WrongThreadReleases;
    internal long NativeBytes;
    internal void Track(string kind, object value) => References.Add((kind, new(value)));
    internal Dictionary<string, int> Alive() => References.GroupBy(x => x.Kind)
        .ToDictionary(g => g.Key, g => g.Count(x => x.Reference.IsAlive));
}
