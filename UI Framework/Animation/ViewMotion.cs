namespace UI_Framework;

/// <summary>Visual destinations, independent of layout, focus and input eligibility.</summary>
public sealed record ViewMotion(double Opacity = 1, double X = 0, double Y = 0,
    AnimationProperties Properties = AnimationProperties.None, AnimationSpec? Animation = null)
{
    internal static readonly ViewMotion Default = new();

    internal void Validate()
    {
        if (!double.IsFinite(Opacity) || Opacity < 0 || Opacity > 1
            || !double.IsFinite(X) || !double.IsFinite(Y)
            || (Properties & ~(AnimationProperties.Opacity | AnimationProperties.Translation)) != 0
            || Animation is { } spec && (spec.Duration < TimeSpan.Zero
                || spec.Easing is not (AnimationEasing.Linear or AnimationEasing.EaseOut)))
            throw new ArgumentOutOfRangeException(nameof(ViewMotion), "Motion requires finite targets, opacity in [0,1], known flags/easing and non-negative duration.");
    }
}
