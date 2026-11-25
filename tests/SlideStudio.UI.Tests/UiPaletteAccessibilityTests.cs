using System.Drawing;
using Xunit;

namespace SlideStudio.UI.Tests;

public sealed class UiPaletteAccessibilityTests
{
    public static IEnumerable<object[]> AccessibleColorPairs =>
    [
        ["AccentText on accent", UiPalette.AccentText, UiPalette.Accent],
        ["AccentText on accent hover", UiPalette.AccentText, UiPalette.AccentHover],
        ["AccentText on accent pressed", UiPalette.AccentText, UiPalette.AccentPressed],
        ["Accent on card hover", UiPalette.Accent, UiPalette.CardHover],
        ["AccentLight on card hover highlight", UiPalette.AccentPressed, UiPalette.CardHoverHighlight],
        ["Primary text on window", UiPalette.PrimaryText, UiPalette.Window],
        ["Primary text on panel", UiPalette.PrimaryText, UiPalette.Panel],
        ["Primary text on elevated surface", UiPalette.PrimaryText, UiPalette.Elevated],
        ["Primary text on selection", UiPalette.PrimaryText, UiPalette.Selection],
        ["Primary text on card hover highlight", UiPalette.PrimaryText, UiPalette.CardHoverHighlight],
        ["Muted text on window", UiPalette.MutedText, UiPalette.Window],
        ["Muted text on panel", UiPalette.MutedText, UiPalette.Panel],
        ["Muted text on elevated surface", UiPalette.MutedText, UiPalette.Elevated],
        ["Muted text on disabled surface", UiPalette.MutedText, UiPalette.DisabledSurface],
        ["Muted text on card hover highlight", UiPalette.MutedText, UiPalette.CardHoverHighlight],
        ["Accent on window", UiPalette.Accent, UiPalette.Window],
        ["Accent on panel", UiPalette.Accent, UiPalette.Panel],
        ["Accent on elevated surface", UiPalette.Accent, UiPalette.Elevated]
    ];

    [Theory]
    [MemberData(nameof(AccessibleColorPairs))]
    public void TextAndSurfaceColors_MeetMinimumContrast(string description, Color foreground, Color background)
    {
        Assert.True(
            GetContrastRatio(foreground, background) >= 4.5,
            $"{description} must have a contrast ratio of at least 4.5:1.");
    }

    private static double GetContrastRatio(Color first, Color second)
    {
        double firstLuminance = GetRelativeLuminance(first);
        double secondLuminance = GetRelativeLuminance(second);
        double lighter = Math.Max(firstLuminance, secondLuminance);
        double darker = Math.Min(firstLuminance, secondLuminance);
        return (lighter + 0.05) / (darker + 0.05);
    }

    private static double GetRelativeLuminance(Color color) =>
        0.2126 * Linearize(color.R / 255d)
        + 0.7152 * Linearize(color.G / 255d)
        + 0.0722 * Linearize(color.B / 255d);

    private static double Linearize(double channel) =>
        channel <= 0.04045
            ? channel / 12.92
            : Math.Pow((channel + 0.055) / 1.055, 2.4);
}
