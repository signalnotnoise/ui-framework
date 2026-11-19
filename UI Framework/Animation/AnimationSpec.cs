namespace UI_Framework;

/// <summary>Immutable timing policy. Zero duration settles immediately.</summary>
public sealed record AnimationSpec(TimeSpan Duration, AnimationEasing Easing = AnimationEasing.EaseOut);
