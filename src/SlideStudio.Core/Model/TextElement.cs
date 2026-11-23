namespace SlideStudio.Core.Model;

/// <summary>Zone de texte avec sa mise en forme.</summary>
public sealed class TextElement : SlideElement
{
    /// <summary>Police proposée pour tout nouveau texte.</summary>
    public const string DefaultFontFamily = "Segoe UI";

    /// <summary>Taille proposée pour tout nouveau texte, en points (lisible en salle : 24 pt ou plus).</summary>
    public const double DefaultFontSizePoints = 24;

    private string _text = string.Empty;
    private string _fontFamily = DefaultFontFamily;
    private double _fontSizePoints = DefaultFontSizePoints;
    private TextAlignment _alignment;

    /// <summary>Crée une zone de texte avec la mise en forme par défaut et un nouvel identifiant.</summary>
    /// <param name="frame">Position et taille.</param>
    /// <param name="text">Contenu initial.</param>
    /// <exception cref="DocumentValidationException">Une valeur est refusée.</exception>
    public TextElement(ElementFrame frame, string text)
        : this(Guid.NewGuid(), frame, text, DefaultFontFamily, DefaultFontSizePoints, false, false, ArgbColor.Black, TextAlignment.Left)
    {
    }

    /// <summary>Crée une zone de texte complète (utilisé notamment par la lecture d'un fichier).</summary>
    /// <param name="id">Identifiant existant.</param>
    /// <param name="frame">Position et taille.</param>
    /// <param name="text">Contenu.</param>
    /// <param name="fontFamily">Nom de police.</param>
    /// <param name="fontSizePoints">Taille en points.</param>
    /// <param name="isBold">Gras.</param>
    /// <param name="isItalic">Italique.</param>
    /// <param name="color">Couleur du texte.</param>
    /// <param name="alignment">Alignement horizontal.</param>
    /// <exception cref="DocumentValidationException">Une valeur est refusée.</exception>
    public TextElement(
        Guid id,
        ElementFrame frame,
        string text,
        string fontFamily,
        double fontSizePoints,
        bool isBold,
        bool isItalic,
        ArgbColor color,
        TextAlignment alignment)
        : base(id, frame)
    {
        Text = text;
        FontFamily = fontFamily;
        FontSizePoints = fontSizePoints;
        IsBold = isBold;
        IsItalic = isItalic;
        Color = color;
        Alignment = alignment;
    }

    /// <summary>Contenu du texte (retours à la ligne autorisés, limité à <see cref="Limits.MaxTextLength"/>).</summary>
    /// <exception cref="DocumentValidationException">Texte trop long ou contenant un caractère interdit.</exception>
    public string Text
    {
        get => _text;
        set
        {
            DocumentRules.Enforce(DocumentRules.CheckText(value), nameof(Text));
            _text = value;
        }
    }

    /// <summary>Nom de la police.</summary>
    /// <exception cref="DocumentValidationException">Nom vide, trop long ou contenant un caractère interdit.</exception>
    public string FontFamily
    {
        get => _fontFamily;
        set
        {
            DocumentRules.Enforce(DocumentRules.CheckFontFamily(value), nameof(FontFamily));
            _fontFamily = value;
        }
    }

    /// <summary>Taille de police en points.</summary>
    /// <exception cref="DocumentValidationException">Taille hors plage ou non finie.</exception>
    public double FontSizePoints
    {
        get => _fontSizePoints;
        set
        {
            DocumentRules.Enforce(DocumentRules.CheckFontSize(value), nameof(FontSizePoints));
            _fontSizePoints = value;
        }
    }

    /// <summary>Texte en gras.</summary>
    public bool IsBold { get; set; }

    /// <summary>Texte en italique.</summary>
    public bool IsItalic { get; set; }

    /// <summary>Couleur du texte.</summary>
    public ArgbColor Color { get; set; }

    /// <summary>Alignement horizontal.</summary>
    /// <exception cref="DocumentValidationException">Valeur d'énumération inconnue (fichier corrompu).</exception>
    public TextAlignment Alignment
    {
        get => _alignment;
        set
        {
            // SÉCURITÉ : un fichier peut contenir « 99 » ; un enum accepte n'importe quel entier sans protester.
            if (!Enum.IsDefined(value))
            {
                throw new DocumentValidationException("L'alignement du texte est invalide.", nameof(Alignment));
            }

            _alignment = value;
        }
    }

    /// <inheritdoc />
    public override TextElement Clone() =>
        new(Guid.NewGuid(), Frame, Text, FontFamily, FontSizePoints, IsBold, IsItalic, Color, Alignment);
}
