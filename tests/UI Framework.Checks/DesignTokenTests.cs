using System.Windows;
using System.Windows.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using UI_Framework;
using UI_Framework.Wpf;
using static UI_Framework.UI;

namespace UI_Framework.Checks;

[TestClass]
public sealed class DesignTokenTests
{
    private static readonly VectorIcon plus = new("M 12,4 V 20 M 4,12 H 20", strokeWidth: 2);

    [TestMethod]
    public void DefaultsAreSharedAndCopiesDoNotMutateOtherThemes()
    {
        var first = new ThemeTokens();
        var second = new ThemeTokens();
        Assert.AreSame(first.Spacing, second.Spacing);
        Assert.AreSame(first.Typography, second.Typography);
        Assert.AreSame(first.Radii, second.Radii);
        Assert.AreSame(first.Icons, second.Icons);
        var changed = first with { Spacing = first.Spacing with { Medium = 18 }, Typography = first.Typography with { Body = 20 } };
        Assert.AreEqual(12d, first.Spacing.Medium);
        Assert.AreEqual(18d, changed.Spacing.Medium);
        Assert.AreEqual(16d, Text("Default").TextSize);
        Assert.AreEqual(16d, Icon(plus).TextSize);
        Assert.AreEqual(8d, first.ControlRadius);
        Assert.AreEqual(10d, first.ControlPadding);
        Assert.AreEqual(6d, first.Icons.LabelGap);
        Assert.AreEqual(first, second);
    }

    [TestMethod]
    public void EveryScaleRejectsInvalidValuesBeforeReplacingResources() => StaTestRunner.Run(() =>
    {
        var root = new StackPanel();
        ThemeStyles.Apply(root, new ThemeTokens());
        var previous = root.Resources[typeof(Button)];
        foreach (var group in new[] { nameof(ThemeTokens.Spacing), nameof(ThemeTokens.Typography), nameof(ThemeTokens.Radii), nameof(ThemeTokens.Icons) })
        {
            var groupProperty = typeof(ThemeTokens).GetProperty(group)!;
            foreach (var valueProperty in groupProperty.PropertyType.GetProperties().Where(property => property.PropertyType == typeof(double)))
            foreach (var bad in new[] { -1d, double.NaN, double.PositiveInfinity, double.NegativeInfinity })
            {
                var tokens = Activator.CreateInstance(groupProperty.PropertyType)!;
                valueProperty.SetValue(tokens, bad);
                var theme = new ThemeTokens();
                groupProperty.SetValue(theme, tokens);
                Assert.ThrowsException<ArgumentOutOfRangeException>(() => ThemeStyles.Apply(root, theme), $"{group}.{valueProperty.Name}: {bad}");
                Assert.AreSame(previous, root.Resources[typeof(Button)]);
            }
            var missing = new ThemeTokens();
            groupProperty.SetValue(missing, null);
            Assert.ThrowsException<ArgumentNullException>(() => ThemeStyles.Apply(root, missing));
        }
    });

    [TestMethod]
    public void ZeroSpacingAndRadiiAreValidButZeroFontAndIconSizesAreNot() => StaTestRunner.Run(() =>
    {
        var root = new StackPanel();
        ThemeStyles.Apply(root, new ThemeTokens {
            Spacing = new() { ExtraSmall = 0, Small = 0, Medium = 0, Large = 0, ExtraLarge = 0, Section = 0 },
            Radii = new() { Small = 0, Medium = 0, Large = 0 }, Icons = new() { LabelGap = 0 }
        });
        foreach (var type in new[] { typeof(TypographyTokens), typeof(IconTokens) })
        foreach (var property in type.GetProperties().Where(property => property.PropertyType == typeof(double) && property.Name != nameof(IconTokens.LabelGap)))
        {
            var scale = Activator.CreateInstance(type)!;
            property.SetValue(scale, 0d);
            var theme = scale is TypographyTokens typography ? new ThemeTokens { Typography = typography } : new ThemeTokens { Icons = (IconTokens)scale };
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => ThemeStyles.Apply(root, theme));
        }
    });

    [TestMethod]
    public void ScopedIconGapUpdatesWithoutReplacingControlsOrLeakingToNestedTheme() => StaTestRunner.Run(() =>
    {
        using var first = new ViewHost(() => Button(plus, "First", () => { }));
        using var second = new ViewHost(() => Button(plus, "Second", () => { }));
        var root = new StackPanel(); root.Children.Add(first); root.Children.Add(second);
        ThemeStyles.Apply(root, new ThemeTokens { Icons = new() { LabelGap = 9 } });
        ThemeStyles.Apply(second, new ThemeTokens { Icons = new() { LabelGap = 3 } });
        TestVisualTree.Layout(root);
        var firstIcon = TestVisualTree.Find<IconElement>(first).Single();
        var secondIcon = TestVisualTree.Find<IconElement>(second).Single();
        var firstButton = TestVisualTree.Find<Button>(first).Single();
        Assert.AreEqual(9d, firstIcon.Margin.Right);
        Assert.AreEqual(3d, secondIcon.Margin.Right);
        ThemeStyles.Apply(root, new ThemeTokens { Icons = new() { LabelGap = 15 } });
        TestVisualTree.Layout(root);
        Assert.AreSame(firstButton, TestVisualTree.Find<Button>(first).Single());
        Assert.AreSame(firstIcon, TestVisualTree.Find<IconElement>(first).Single());
        Assert.AreEqual(15d, firstIcon.Margin.Right);
        Assert.AreEqual(3d, secondIcon.Margin.Right);
    });

    [TestMethod]
    public void IconOnlyButtonHasNoLabelGapAndUsesCurrentThemeWhenLabelReturns() => StaTestRunner.Run(() =>
    {
        var view = IconButton(plus, "Add", () => { });
        using var host = new ViewHost(() => view);
        ThemeStyles.Apply(host, new ThemeTokens { Icons = new() { LabelGap = 14 } });
        TestVisualTree.Layout(host);
        var icon = TestVisualTree.Find<IconElement>(host).Single();
        Assert.AreEqual(0d, icon.Margin.Right);
        view = Button(plus, "Add", () => { });
        host.Refresh(); TestVisualTree.Layout(host);
        Assert.AreEqual(14d, icon.Margin.Right);
        view = IconButton(plus, "Add", () => { });
        host.Refresh(); TestVisualTree.Layout(host);
        Assert.AreEqual(0d, icon.Margin.Right);
    });

    [TestMethod]
    public void ExplicitCompositionUsesTheSameTokensForLayoutTextIconsAndCorners() => StaTestRunner.Run(() =>
    {
        var theme = new ThemeTokens { Typography = new() { Heading = 30 }, Icons = new() { Medium = 28 }, Spacing = new() { Medium = 18 }, Radii = new() { Large = 14 } };
        using var host = new ViewHost(() => VStack(
            Text("Heading").FontSize(theme.Typography.Heading), Icon(plus, theme.Icons.Medium))
            .Spacing(theme.Spacing.Medium).Padding(theme.Spacing.Large).CornerRadius(theme.Radii.Large));
        ThemeStyles.Apply(host, theme); TestVisualTree.Layout(host);
        var text = TestVisualTree.Find<TextBlock>(host).Single();
        var icon = TestVisualTree.Find<IconElement>(host).Single();
        Assert.AreEqual(theme.Typography.Heading, text.FontSize);
        Assert.AreEqual(theme.Icons.Medium, icon.DesiredSize.Width);
        Assert.AreEqual(new Thickness(theme.Spacing.Large), ((Border)host.Content).Padding);
        Assert.AreEqual(new CornerRadius(theme.Radii.Large), ((Border)host.Content).CornerRadius);
        theme = theme with { Typography = theme.Typography with { Heading = 26 } };
        host.Refresh(); TestVisualTree.Layout(host);
        Assert.AreSame(text, TestVisualTree.Find<TextBlock>(host).Single());
        Assert.AreEqual(26d, text.FontSize);
    });
}
