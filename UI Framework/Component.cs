namespace UI_Framework;

/// <summary>A retained owner of local state and one independently observed view body.</summary>
public abstract class Component
{
    public abstract View Body();

    /// <summary>Called once after the first body render. This is renderer lifetime, not visual Loaded.</summary>
    public virtual void OnMounted() { }

    /// <summary>Called once on removal, identity replacement, or host disposal, after subscriptions detach.</summary>
    public virtual void OnUnmounted() { }
}
