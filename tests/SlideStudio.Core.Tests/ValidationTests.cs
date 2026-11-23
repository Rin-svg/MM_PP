using SlideStudio.Core.Model;
using Xunit;

namespace SlideStudio.Core.Tests;

/// <summary>Règles de validation : valeurs limites (max, max+1), cas invalides, et « une valeur refusée ne change rien ».</summary>
public class ValidationTests
{
    // ------------------------------------------------------------------ Texte

    [Fact]
    public void Text_ExactlyAtLimit_IsAccepted()
    {
        TextElement element = TestSupport.NewText();

        element.Text = new string('A', Limits.MaxTextLength);

        Assert.Equal(Limits.MaxTextLength, element.Text.Length);
    }

    [Fact]
    public void Text_OneOverLimit_IsRejectedAndPreviousValueKept()
    {
        TextElement element = TestSupport.NewText("avant");

        DocumentValidationException error = Assert.Throws<DocumentValidationException>(
            () => element.Text = new string('A', Limits.MaxTextLength + 1));

        Assert.Equal("avant", element.Text);
        Assert.Equal("Text", error.PropertyName);
        Assert.Contains("trop long", error.Message);
    }

    [Fact]
    public void Text_TenMegabytesPasted_IsRejected()
    {
        TextElement element = TestSupport.NewText();

        Assert.Throws<DocumentValidationException>(() => element.Text = new string('x', 10 * 1024 * 1024));
    }

    [Theory]
    [InlineData("a\0b")]
    [InlineData("a\u0001b")]
    [InlineData("a\u001Fb")]
    [InlineData("a\u007Fb")]
    [InlineData("a\u0085b")]
    public void Text_ControlCharacter_IsRejected(string text)
    {
        TextElement element = TestSupport.NewText();

        Assert.Throws<DocumentValidationException>(() => element.Text = text);
    }

    [Fact]
    public void Text_LineBreaksAndTabs_AreAccepted()
    {
        TextElement element = TestSupport.NewText();

        element.Text = "ligne1\r\nligne2\tfin";

        Assert.Equal("ligne1\r\nligne2\tfin", element.Text);
    }

    [Fact]
    public void Text_EmojiAndComposedSequences_AreAcceptedUnchanged()
    {
        TextElement element = TestSupport.NewText();
        const string text = "😀👨‍👩‍👧 é e\u0301";

        element.Text = text;

        // Aucune « correction » silencieuse : le texte ressort octet pour octet identique.
        Assert.Equal(text, element.Text);
    }

    [Fact]
    public void Text_LoneSurrogate_IsRejected()
    {
        // ATTENTION : ces chaînes sont construites ICI et non dans un [InlineData]. Les arguments d'attribut sont
        // enregistrés en UTF-8 dans l'assembly : le compilateur y remplace une moitié d'émoji isolée par U+FFFD
        // (un caractère valide), et le test ne vérifierait plus rien.
        string[] hostile =
        [
            new string(['\uD83D']),
            "x" + new string(['\uDE00']),
            new string(['\uD83D']) + "x",
            "ok" + new string(['\uD83D', '\uD83D', '\uDE00']),
        ];

        foreach (string text in hostile)
        {
            TextElement element = TestSupport.NewText("avant");

            Assert.Throws<DocumentValidationException>(() => element.Text = text);
            Assert.Equal("avant", element.Text);
        }
    }

    [Fact]
    public void Text_ProperSurrogatePair_IsAccepted()
    {
        TextElement element = TestSupport.NewText();

        element.Text = new string(['\uD83D', '\uDE00']);

        Assert.Equal(2, element.Text.Length);
    }

    [Fact]
    public void Text_RightToLeftAndBidiOverride_IsAcceptedAsPlainText()
    {
        TextElement element = TestSupport.NewText();

        element.Text = "שלום \u202Eevil";

        Assert.Equal("שלום \u202Eevil", element.Text);
    }

    [Fact]
    public void Text_Empty_IsAccepted()
    {
        TextElement element = TestSupport.NewText("x");

        element.Text = string.Empty;

        Assert.Equal(string.Empty, element.Text);
    }

    [Fact]
    public void CheckText_Null_ReturnsMessage()
    {
        Assert.NotNull(DocumentRules.CheckText(null));
    }

    // ------------------------------------------------------------------ Titre et texte alternatif (une seule ligne)

    [Fact]
    public void Title_AtLimit_IsAcceptedAndOneOverIsRejected()
    {
        var presentation = new Presentation();

        presentation.Title = new string('T', Limits.MaxTitleLength);
        Assert.Equal(Limits.MaxTitleLength, presentation.Title.Length);

        Assert.Throws<DocumentValidationException>(() => presentation.Title = new string('T', Limits.MaxTitleLength + 1));
        Assert.Equal(Limits.MaxTitleLength, presentation.Title.Length);
    }

    [Theory]
    [InlineData("a\nb")]
    [InlineData("a\rb")]
    [InlineData("a\tb")]
    [InlineData("a\u2028b")]
    [InlineData("a\u2029b")]
    public void Title_LineBreak_IsRejected(string title)
    {
        var presentation = new Presentation();

        Assert.Throws<DocumentValidationException>(() => presentation.Title = title);
        Assert.Equal(Presentation.DefaultTitle, presentation.Title);
    }

    [Fact]
    public void AltText_AtLimitAccepted_OneOverRejected()
    {
        var image = new ImageElement(TestSupport.ValidFrame(), "logo.png");

        image.AltText = new string('a', Limits.MaxAltTextLength);
        Assert.Throws<DocumentValidationException>(() => image.AltText = new string('a', Limits.MaxAltTextLength + 1));
        Assert.Throws<DocumentValidationException>(() => image.AltText = "deux\nlignes");
        Assert.Equal(Limits.MaxAltTextLength, image.AltText.Length);
    }

    // ------------------------------------------------------------------ Police

    [Theory]
    [InlineData("Segoe UI")]
    [InlineData("Arial")]
    [InlineData("Times New Roman")]
    [InlineData("Noto Sans (Display)")]
    [InlineData("Courier-New_2.0")]
    [InlineData("Ünïcödé Sans")]
    public void FontFamily_Valid_IsAccepted(string name)
    {
        TextElement element = TestSupport.NewText();

        element.FontFamily = name;

        Assert.Equal(name, element.FontFamily);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Arial\0")]
    [InlineData("a;b")]
    [InlineData("<script>")]
    [InlineData(" Arial")]
    [InlineData("Arial ")]
    [InlineData("Ar\"ial")]
    [InlineData("..\\Arial")]
    public void FontFamily_Invalid_IsRejectedAndPreviousKept(string name)
    {
        TextElement element = TestSupport.NewText();

        Assert.Throws<DocumentValidationException>(() => element.FontFamily = name);

        Assert.Equal(TextElement.DefaultFontFamily, element.FontFamily);
    }

    [Fact]
    public void FontFamily_LengthLimit_IsEnforced()
    {
        TextElement element = TestSupport.NewText();

        element.FontFamily = new string('F', Limits.MaxFontFamilyLength);
        Assert.Throws<DocumentValidationException>(() => element.FontFamily = new string('F', Limits.MaxFontFamilyLength + 1));
    }

    // ------------------------------------------------------------------ Tailles numériques

    [Theory]
    [InlineData(6.0)]
    [InlineData(24.0)]
    [InlineData(400.0)]
    public void FontSize_InRange_IsAccepted(double points)
    {
        TextElement element = TestSupport.NewText();

        element.FontSizePoints = points;

        Assert.Equal(points, element.FontSizePoints);
    }

    [Theory]
    [InlineData(5.99)]
    [InlineData(400.01)]
    [InlineData(0.0)]
    [InlineData(-1.0)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void FontSize_OutOfRangeOrNotFinite_IsRejected(double points)
    {
        TextElement element = TestSupport.NewText();

        Assert.Throws<DocumentValidationException>(() => element.FontSizePoints = points);

        Assert.Equal(TextElement.DefaultFontSizePoints, element.FontSizePoints);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(2.0)]
    [InlineData(100.0)]
    public void StrokeWidth_InRange_IsAccepted(double width)
    {
        ShapeElement shape = TestSupport.NewShape();

        shape.StrokeWidth = width;

        Assert.Equal(width, shape.StrokeWidth);
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(100.01)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void StrokeWidth_OutOfRange_IsRejected(double width)
    {
        ShapeElement shape = TestSupport.NewShape();

        Assert.Throws<DocumentValidationException>(() => shape.StrokeWidth = width);
        Assert.Equal(ShapeElement.DefaultStrokeWidth, shape.StrokeWidth);
    }

    // ------------------------------------------------------------------ Cadre (position, taille, rotation)

    [Theory]
    [InlineData(0.0, 0.0, 1.0, 1.0, 0.0)]
    [InlineData(-20000.0, -20000.0, 20000.0, 20000.0, 359.999)]
    [InlineData(100.0, 100.0, 300.0, 200.0, 0.0)]
    [InlineData(1280.0, 720.0, 10.0, 10.0, 180.0)]
    public void Frame_Valid_IsAccepted(double x, double y, double width, double height, double rotation)
    {
        TextElement element = TestSupport.NewText();
        var frame = new ElementFrame(x, y, width, height, rotation);

        element.Frame = frame;

        Assert.Equal(frame, element.Frame);
    }

    [Theory]
    [InlineData(20001.0, 0.0, 10.0, 10.0, 0.0)]
    [InlineData(0.0, -20001.0, 10.0, 10.0, 0.0)]
    [InlineData(0.0, 0.0, 0.0, 10.0, 0.0)]
    [InlineData(0.0, 0.0, 10.0, 0.0, 0.0)]
    [InlineData(0.0, 0.0, -5.0, 10.0, 0.0)]
    [InlineData(0.0, 0.0, 20001.0, 10.0, 0.0)]
    [InlineData(double.NaN, 0.0, 10.0, 10.0, 0.0)]
    [InlineData(0.0, double.NaN, 10.0, 10.0, 0.0)]
    [InlineData(0.0, 0.0, double.NaN, 10.0, 0.0)]
    [InlineData(0.0, 0.0, 10.0, double.NaN, 0.0)]
    [InlineData(0.0, 0.0, 10.0, 10.0, 360.0)]
    [InlineData(0.0, 0.0, 10.0, 10.0, -1.0)]
    [InlineData(0.0, 0.0, 10.0, 10.0, double.NaN)]
    [InlineData(double.PositiveInfinity, 0.0, 10.0, 10.0, 0.0)]
    public void Frame_Invalid_IsRejectedAndPreviousFrameKept(double x, double y, double width, double height, double rotation)
    {
        TextElement element = TestSupport.NewText();
        ElementFrame before = element.Frame;

        Assert.Throws<DocumentValidationException>(() => element.Frame = new ElementFrame(x, y, width, height, rotation));

        Assert.Equal(before, element.Frame);
    }

    [Fact]
    public void Constructor_InvalidFrame_IsRejected()
    {
        Assert.Throws<DocumentValidationException>(() => _ = new TextElement(new ElementFrame(0, 0, 0, 0), "x"));
    }

    // ------------------------------------------------------------------ Identifiants et énumérations

    [Fact]
    public void Element_EmptyGuid_IsRejected()
    {
        Assert.Throws<DocumentValidationException>(() => _ = new ShapeElement(
            Guid.Empty, ShapeKind.Rectangle, TestSupport.ValidFrame(), ArgbColor.White, ArgbColor.Black, 1));
    }

    [Fact]
    public void Enums_UnknownValue_IsRejected()
    {
        TextElement text = TestSupport.NewText();
        ShapeElement shape = TestSupport.NewShape();

        Assert.Throws<DocumentValidationException>(() => text.Alignment = (TextAlignment)99);
        Assert.Throws<DocumentValidationException>(() => shape.Kind = (ShapeKind)(-1));
        Assert.Equal(TextAlignment.Left, text.Alignment);
        Assert.Equal(ShapeKind.Rectangle, shape.Kind);
    }

    // ------------------------------------------------------------------ Nom d'image embarquée

    [Theory]
    [InlineData("logo.png")]
    [InlineData("a-b_c.JPG")]
    [InlineData("photo.jpeg")]
    [InlineData("x.gif")]
    [InlineData("img.bmp")]
    [InlineData("0.png")]
    public void MediaName_Valid_IsAccepted(string name)
    {
        Assert.Null(DocumentRules.CheckMediaName(name));
    }

    [Theory]
    [InlineData("")]
    [InlineData("../x.png")]
    [InlineData("..\\x.png")]
    [InlineData("a/b.png")]
    [InlineData("a\\b.png")]
    [InlineData("C:x.png")]
    [InlineData("image.png:stream")]
    [InlineData("CON.png")]
    [InlineData("nul.png")]
    [InlineData("COM1.png")]
    [InlineData("lpt9.bmp")]
    [InlineData(".png")]
    [InlineData(".hidden.png")]
    [InlineData("x.exe")]
    [InlineData("x")]
    [InlineData("x.png.exe")]
    [InlineData("a b.png")]
    [InlineData("a..png")]
    [InlineData("é.png")]
    [InlineData("a\0.png")]
    [InlineData("x.png.")]
    public void MediaName_Invalid_IsRejected(string name)
    {
        Assert.NotNull(DocumentRules.CheckMediaName(name));
    }

    [Fact]
    public void MediaName_LengthLimit_IsEnforced()
    {
        string atLimit = new string('a', Limits.MaxMediaNameLength - 4) + ".png";
        string overLimit = new string('a', Limits.MaxMediaNameLength - 3) + ".png";

        Assert.Null(DocumentRules.CheckMediaName(atLimit));
        Assert.NotNull(DocumentRules.CheckMediaName(overLimit));
    }

    [Fact]
    public void MediaName_Null_ReturnsMessage()
    {
        Assert.NotNull(DocumentRules.CheckMediaName(null));
    }
}
