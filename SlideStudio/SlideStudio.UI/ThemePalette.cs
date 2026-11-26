using System.Drawing;

namespace SlideStudio.UI;

/// <summary>
/// Palette de couleurs utilis?e dans SlideStudio.
/// </summary>
public static class ThemePalette
{
    /// <summary>Background color for the application.</summary>
    public static Color Background { get; } =
        Color.FromArgb(245, 247, 250);

    /// <summary>Primary accent color.</summary>
    public static Color Primary { get; } =
        Color.FromArgb(45, 105, 180);

    /// <summary>Secondary surface color.</summary>
    public static Color Secondary { get; } =
        Color.FromArgb(225, 232, 240);

    /// <summary>Primary text color.</summary>
    public static Color Text { get; } =
        Color.FromArgb(35, 42, 52);

    /// <summary>Muted text color.</summary>
    public static Color MutedText { get; } =
        Color.FromArgb(110, 120, 135);

    /// <summary>Success status color.</summary>
    public static Color Success { get; } =
        Color.FromArgb(40, 150, 95);

    /// <summary>Error and danger color.</summary>
    public static Color Danger { get; } =
        Color.FromArgb(200, 65, 65);

    /// <summary>Border color.</summary>
    public static Color Border { get; } =
        Color.FromArgb(210, 218, 228);
}
