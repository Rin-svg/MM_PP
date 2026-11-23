using SlideStudio.Core.Model;
using Xunit;

namespace SlideStudio.Core.Tests;

/// <summary>Couleurs (lecture hexadécimale stricte, contraste) et géométrie du cadre.</summary>
public class ColorAndFrameTests
{
    [Theory]
    [InlineData("#FF0000", 255, 255, 0, 0)]
    [InlineData("#ff0000", 255, 255, 0, 0)]
    [InlineData("#010203", 255, 1, 2, 3)]
    [InlineData("#80FF0000", 128, 255, 0, 0)]
    [InlineData("#00000000", 0, 0, 0, 0)]
    public void TryParseHex_Valid_ReturnsColor(string text, int a, int r, int g, int b)
    {
        bool ok = ArgbColor.TryParseHex(text, out ArgbColor color);

        Assert.True(ok);
        Assert.Equal(new ArgbColor((byte)a, (byte)r, (byte)g, (byte)b), color);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("#")]
    [InlineData("FF0000")]
    [InlineData("#FF00")]
    [InlineData("#FF00000")]
    [InlineData("#FF00000000")]
    [InlineData("#GG0000")]
    [InlineData("#FF 000")]
    [InlineData("red")]
    [InlineData("#FF0000\0")]
    [InlineData(" #FF0000")]
    [InlineData("#FF0000 ")]
    [InlineData("#+F0000")]
    [InlineData("#-F0000")]
    [InlineData("#ＦＦ0000")]
    public void TryParseHex_Invalid_ReturnsFalse(string? text)
    {
        bool ok = ArgbColor.TryParseHex(text, out ArgbColor color);

        Assert.False(ok);
        Assert.Equal(default(ArgbColor), color);
    }

    [Fact]
    public void ToHex_OpaqueAndTranslucent_UseExpectedFormats()
    {
        Assert.Equal("#010203", new ArgbColor(255, 1, 2, 3).ToHex());
        Assert.Equal("#80FF0000", new ArgbColor(128, 255, 0, 0).ToHex());
    }

    [Fact]
    public void ToHexThenParse_RoundTrips()
    {
        ArgbColor[] samples =
        [
            ArgbColor.Black, ArgbColor.White, ArgbColor.Transparent,
            ArgbColor.FromRgb(0x2B, 0x6C, 0xB0), new ArgbColor(7, 8, 9, 10), new ArgbColor(255, 255, 255, 254),
        ];

        foreach (ArgbColor sample in samples)
        {
            Assert.True(ArgbColor.TryParseHex(sample.ToHex(), out ArgbColor parsed));
            Assert.Equal(sample, parsed);
        }
    }

    [Fact]
    public void ContrastRatio_BlackOnWhite_IsAbout21()
    {
        double ratio = ArgbColor.Black.ContrastRatio(ArgbColor.White);

        Assert.True(Math.Abs(ratio - 21.0) < 0.01);
    }

    [Fact]
    public void ContrastRatio_IsSymmetricAndOneForIdenticalColors()
    {
        ArgbColor blue = ArgbColor.FromRgb(0x2B, 0x6C, 0xB0);

        Assert.True(Math.Abs(blue.ContrastRatio(blue) - 1.0) < 1e-9);
        Assert.True(Math.Abs(blue.ContrastRatio(ArgbColor.White) - ArgbColor.White.ContrastRatio(blue)) < 1e-9);
    }

    [Fact]
    public void ContrastRatio_MatchesKnownWcagThresholdForGrey()
    {
        // Valeurs de référence connues : #767676 sur blanc = 4,54:1 (juste au-dessus de 4,5) ; #777777 = 4,48:1 (juste en dessous).
        // Elles prouvent que la formule est correcte, pas seulement cohérente avec elle-même.
        double passes = ArgbColor.FromRgb(0x76, 0x76, 0x76).ContrastRatio(ArgbColor.White);
        double fails = ArgbColor.FromRgb(0x77, 0x77, 0x77).ContrastRatio(ArgbColor.White);

        Assert.True(passes >= ArgbColor.MinTextContrastRatio);
        Assert.True(fails < ArgbColor.MinTextContrastRatio);
    }

    [Fact]
    public void DefaultTextColorOnDefaultBackground_MeetsContrastRule()
    {
        // Règle de design retenue : tout nouveau texte doit être lisible sur une nouvelle diapositive.
        TextElement text = TestSupport.NewText();
        var slide = new Slide();

        Assert.True(text.Color.ContrastRatio(slide.Background) >= ArgbColor.MinTextContrastRatio);
    }

    [Theory]
    [InlineData(370.0, 10.0)]
    [InlineData(-90.0, 270.0)]
    [InlineData(360.0, 0.0)]
    [InlineData(0.0, 0.0)]
    [InlineData(720.0, 0.0)]
    [InlineData(-360.0, 0.0)]
    [InlineData(45.5, 45.5)]
    public void NormalizeRotation_BringsAngleIntoZeroTo360(double input, double expected)
    {
        Assert.True(Math.Abs(ElementFrame.NormalizeRotation(input) - expected) < 1e-9);
    }

    [Fact]
    public void NormalizeRotation_TinyNegativeAngle_NeverReturns360()
    {
        double normalized = ElementFrame.NormalizeRotation(-1e-20);

        Assert.True(normalized >= 0 && normalized < 360);
        Assert.Null(DocumentRules.CheckFrame(new ElementFrame(0, 0, 10, 10, normalized)));
    }

    [Fact]
    public void NormalizeRotation_NotFinite_IsNotSilentlyRepaired()
    {
        Assert.True(double.IsNaN(ElementFrame.NormalizeRotation(double.NaN)));
        Assert.True(double.IsPositiveInfinity(ElementFrame.NormalizeRotation(double.PositiveInfinity)));
    }

    [Fact]
    public void MovedAndResized_ReturnNewFramesWithoutChangingTheOriginal()
    {
        var original = new ElementFrame(10, 20, 30, 40, 5);

        ElementFrame moved = original.Moved(1, -2);
        ElementFrame resized = original.Resized(100, 200);

        Assert.Equal(new ElementFrame(11, 18, 30, 40, 5), moved);
        Assert.Equal(new ElementFrame(10, 20, 100, 200, 5), resized);
        Assert.Equal(new ElementFrame(10, 20, 30, 40, 5), original);
    }
}
