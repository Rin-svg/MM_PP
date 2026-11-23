using System.Globalization;

namespace SlideStudio.Core.Model;

/// <summary>
/// Couleur ARGB indépendante de toute bibliothèque graphique (donc testable et sérialisable partout).
/// </summary>
/// <remarks>
/// POURQUOI ne pas utiliser <c>System.Drawing.Color</c> : il appartient à l'interface Windows ; le Core doit
/// rester compilable sans elle. L'interface convertit cette valeur en couleur native au moment de dessiner.
/// </remarks>
/// <param name="A">Opacité : 0 = transparent, 255 = opaque.</param>
/// <param name="R">Rouge (0 à 255).</param>
/// <param name="G">Vert (0 à 255).</param>
/// <param name="B">Bleu (0 à 255).</param>
public readonly record struct ArgbColor(byte A, byte R, byte G, byte B)
{
    /// <summary>Rapport de contraste minimal recommandé (WCAG, niveau AA) pour du texte courant.</summary>
    public const double MinTextContrastRatio = 4.5;

    /// <summary>Couleur totalement transparente (sert à dire « pas de remplissage »).</summary>
    public static ArgbColor Transparent => new(0, 0, 0, 0);

    /// <summary>Blanc opaque.</summary>
    public static ArgbColor White => new(255, 255, 255, 255);

    /// <summary>Noir opaque.</summary>
    public static ArgbColor Black => new(255, 0, 0, 0);

    /// <summary>Crée une couleur opaque à partir de ses trois canaux.</summary>
    /// <param name="r">Rouge.</param>
    /// <param name="g">Vert.</param>
    /// <param name="b">Bleu.</param>
    /// <returns>Couleur opaque.</returns>
    public static ArgbColor FromRgb(byte r, byte g, byte b) => new(255, r, g, b);

    /// <summary>
    /// Lit une couleur écrite <c>#RRGGBB</c> ou <c>#AARRGGBB</c>.
    /// </summary>
    /// <remarks>
    /// SÉCURITÉ : liste blanche stricte (un « # » puis exactement 6 ou 8 chiffres hexadécimaux). Pas d'espaces,
    /// pas de signe, pas de nom de couleur : le texte vient d'un fichier, donc il est traité comme hostile.
    /// </remarks>
    /// <param name="text">Texte à lire (peut être <c>null</c>).</param>
    /// <param name="color">Couleur lue si le résultat est <c>true</c>, sinon la valeur par défaut.</param>
    /// <returns><c>true</c> si le texte est valide.</returns>
    public static bool TryParseHex(string? text, out ArgbColor color)
    {
        color = default;

        if (text is null || (text.Length != 7 && text.Length != 9) || text[0] != '#')
        {
            return false;
        }

        ReadOnlySpan<char> digits = text.AsSpan(1);
        byte alpha = 255;
        int offset = 0;

        if (digits.Length == 8)
        {
            if (!TryParseChannel(digits[..2], out alpha))
            {
                return false;
            }

            offset = 2;
        }

        if (!TryParseChannel(digits.Slice(offset, 2), out byte red)
            || !TryParseChannel(digits.Slice(offset + 2, 2), out byte green)
            || !TryParseChannel(digits.Slice(offset + 4, 2), out byte blue))
        {
            return false;
        }

        color = new ArgbColor(alpha, red, green, blue);
        return true;
    }

    /// <summary>Écrit la couleur sous la forme <c>#RRGGBB</c> (opaque) ou <c>#AARRGGBB</c>.</summary>
    /// <returns>Texte hexadécimal, indépendant de la culture.</returns>
    public string ToHex() => A == 255
        ? string.Create(CultureInfo.InvariantCulture, $"#{R:X2}{G:X2}{B:X2}")
        : string.Create(CultureInfo.InvariantCulture, $"#{A:X2}{R:X2}{G:X2}{B:X2}");

    /// <summary>Identique à <see cref="ToHex"/> (pratique dans les messages de test).</summary>
    /// <returns>Texte hexadécimal.</returns>
    public override string ToString() => ToHex();

    /// <summary>
    /// Rapport de contraste WCAG entre cette couleur et une autre (de 1 = identiques à 21 = noir sur blanc).
    /// </summary>
    /// <remarks>L'opacité est ignorée : on compare les couleurs comme si elles étaient opaques.</remarks>
    /// <param name="other">Couleur de comparaison (par exemple le fond de la diapositive).</param>
    /// <returns>Rapport de contraste entre 1 et 21.</returns>
    public double ContrastRatio(ArgbColor other)
    {
        double first = RelativeLuminance();
        double second = other.RelativeLuminance();
        double lighter = Math.Max(first, second);
        double darker = Math.Min(first, second);
        return (lighter + 0.05) / (darker + 0.05);
    }

    private static bool TryParseChannel(ReadOnlySpan<char> twoDigits, out byte value) =>
        byte.TryParse(twoDigits, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out value);

    // Formule WCAG 2.x : on convertit chaque canal sRGB en valeur linéaire, puis on pondère selon la
    // sensibilité de l'œil (le vert compte beaucoup plus que le bleu).
    private double RelativeLuminance() =>
        (0.2126 * ToLinear(R)) + (0.7152 * ToLinear(G)) + (0.0722 * ToLinear(B));

    private static double ToLinear(byte channel)
    {
        double normalized = channel / 255.0;
        return normalized <= 0.03928 ? normalized / 12.92 : Math.Pow((normalized + 0.055) / 1.055, 2.4);
    }
}
