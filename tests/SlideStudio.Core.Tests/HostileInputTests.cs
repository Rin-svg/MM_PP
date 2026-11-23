using SlideStudio.Core.Model;
using Xunit;

namespace SlideStudio.Core.Tests;

/// <summary>
/// Entrées hostiles : chaque champ texte et chaque nombre reçoit des valeurs piégées.
/// Attendu : soit un rejet propre (DocumentValidationException), soit un traitement inoffensif
/// (valeur conservée telle quelle comme texte brut). JAMAIS une autre exception, JAMAIS une valeur « corrigée » en silence.
/// </summary>
public class HostileInputTests
{
    private static readonly string[] _hostileStrings =
    [
        string.Empty,
        "   \t  ",
        new string('A', 1_000_000),
        "'; DROP TABLE Clients; --",
        "<script>alert(1)</script>",
        @"..\..\Windows\System32\x",
        "../../etc/passwd",
        @"C:\Windows\x.png",
        "CON",
        "a\0b",
        "line1\r\nFAKE LOG ENTRY",
        "😀👨‍👩‍👧",
        "İıǅ\u202Eevil",
        "%s%n%x {0} ${jndi:x}",
        "\uD800",
        "\uFFFF",
        "\u2028\u2029",
    ];

    private static readonly double[] _hostileNumbers =
    [
        double.NaN,
        double.PositiveInfinity,
        double.NegativeInfinity,
        double.MaxValue,
        double.MinValue,
        double.Epsilon,
        -0.0,
        1e308,
        -1e-308,
        int.MaxValue,
        long.MinValue,
    ];

    [Fact]
    public void TextProperty_HostileStrings_AreRejectedCleanlyOrStoredVerbatim()
    {
        foreach (string hostile in _hostileStrings)
        {
            TextElement element = TestSupport.NewText("avant");

            bool accepted = TrySet(() => element.Text = hostile);

            // Rejeté => ancienne valeur intacte ; accepté => valeur strictement identique (aucune « correction »).
            Assert.Equal(accepted ? hostile : "avant", element.Text);
        }
    }

    [Fact]
    public void FontFamilyProperty_HostileStrings_AreRejectedCleanlyOrStoredVerbatim()
    {
        foreach (string hostile in _hostileStrings)
        {
            TextElement element = TestSupport.NewText();

            bool accepted = TrySet(() => element.FontFamily = hostile);

            Assert.Equal(accepted ? hostile : TextElement.DefaultFontFamily, element.FontFamily);
        }
    }

    [Fact]
    public void MediaNameAndAltText_HostileStrings_AreRejectedCleanlyOrStoredVerbatim()
    {
        foreach (string hostile in _hostileStrings)
        {
            var image = new ImageElement(TestSupport.ValidFrame(), "logo.png");

            bool nameAccepted = TrySet(() => image.MediaName = hostile);
            bool altAccepted = TrySet(() => image.AltText = hostile);

            Assert.Equal(nameAccepted ? hostile : "logo.png", image.MediaName);
            Assert.Equal(altAccepted ? hostile : string.Empty, image.AltText);
        }
    }

    [Fact]
    public void PresentationTitle_HostileStrings_AreRejectedCleanlyOrStoredVerbatim()
    {
        foreach (string hostile in _hostileStrings)
        {
            var presentation = new Presentation();

            bool accepted = TrySet(() => presentation.Title = hostile);

            Assert.Equal(accepted ? hostile : Presentation.DefaultTitle, presentation.Title);
        }
    }

    [Fact]
    public void MediaName_NeverAcceptsAnythingContainingASeparatorOrColon()
    {
        foreach (string hostile in _hostileStrings)
        {
            if (DocumentRules.CheckMediaName(hostile) is null)
            {
                Assert.False(hostile.Contains('/') || hostile.Contains('\\') || hostile.Contains(':') || hostile.Contains('\0'));
            }
        }
    }

    [Fact]
    public void HostileNumbers_NeverCauseAnythingButCleanRejection()
    {
        foreach (double hostile in _hostileNumbers)
        {
            TextElement text = TestSupport.NewText();
            ShapeElement shape = TestSupport.NewShape();
            ElementFrame originalFrame = text.Frame;

            bool sizeAccepted = TrySet(() => text.FontSizePoints = hostile);
            bool strokeAccepted = TrySet(() => shape.StrokeWidth = hostile);

            // Un nombre accepté est forcément fini et dans la plage : aucun NaN ne se glisse dans le modèle.
            Assert.Equal(sizeAccepted, hostile >= Limits.MinFontSizePoints && hostile <= Limits.MaxFontSizePoints);
            Assert.Equal(strokeAccepted, hostile >= 0 && hostile <= Limits.MaxStrokeWidth);

            // Chaque nombre piégé est essayé dans chacune des cinq cases du cadre.
            Assert.True(FrameFieldsBehave(hostile, originalFrame));
        }
    }

    private static bool FrameFieldsBehave(double hostile, ElementFrame original)
    {
        ElementFrame[] candidates =
        [
            original with { X = hostile },
            original with { Y = hostile },
            original with { Width = hostile },
            original with { Height = hostile },
            original with { RotationDegrees = hostile },
        ];

        foreach (ElementFrame candidate in candidates)
        {
            TextElement element = TestSupport.NewText();
            bool accepted = TrySet(() => element.Frame = candidate);

            // Accepté seulement si CheckFrame l'a validé ; sinon le cadre d'origine est intact.
            if (accepted != (DocumentRules.CheckFrame(candidate) is null))
            {
                return false;
            }

            if (!accepted && element.Frame != original)
            {
                return false;
            }

            if (accepted && (!double.IsFinite(element.Frame.X) || !double.IsFinite(element.Frame.Width)))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>Exécute l'affectation : renvoie true si acceptée, false si rejetée proprement ; toute autre exception échoue le test.</summary>
    private static bool TrySet(Action assignment)
    {
        try
        {
            assignment();
            return true;
        }
        catch (DocumentValidationException)
        {
            return false;
        }
    }
}
