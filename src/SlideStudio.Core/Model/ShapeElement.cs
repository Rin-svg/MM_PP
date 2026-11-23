namespace SlideStudio.Core.Model;

/// <summary>Forme géométrique (rectangle, ellipse ou ligne).</summary>
public sealed class ShapeElement : SlideElement
{
    /// <summary>Épaisseur de contour proposée pour toute nouvelle forme.</summary>
    public const double DefaultStrokeWidth = 2;

    private ShapeKind _kind;
    private double _strokeWidth;

    /// <summary>Crée une forme avec remplissage bleu, contour sombre et un nouvel identifiant.</summary>
    /// <param name="kind">Type de forme.</param>
    /// <param name="frame">Position et taille.</param>
    /// <exception cref="DocumentValidationException">Une valeur est refusée.</exception>
    public ShapeElement(ShapeKind kind, ElementFrame frame)
        : this(Guid.NewGuid(), kind, frame, ArgbColor.FromRgb(0x2B, 0x6C, 0xB0), ArgbColor.FromRgb(0x1A, 0x36, 0x5D), DefaultStrokeWidth)
    {
    }

    /// <summary>Crée une forme complète (utilisé notamment par la lecture d'un fichier).</summary>
    /// <param name="id">Identifiant existant.</param>
    /// <param name="kind">Type de forme.</param>
    /// <param name="frame">Position et taille.</param>
    /// <param name="fill">Remplissage (transparent = aucun).</param>
    /// <param name="stroke">Couleur du contour.</param>
    /// <param name="strokeWidth">Épaisseur du contour (0 = aucun).</param>
    /// <exception cref="DocumentValidationException">Une valeur est refusée.</exception>
    public ShapeElement(Guid id, ShapeKind kind, ElementFrame frame, ArgbColor fill, ArgbColor stroke, double strokeWidth)
        : base(id, frame)
    {
        Kind = kind;
        Fill = fill;
        Stroke = stroke;
        StrokeWidth = strokeWidth;
    }

    /// <summary>Type de forme.</summary>
    /// <exception cref="DocumentValidationException">Valeur d'énumération inconnue (fichier corrompu).</exception>
    public ShapeKind Kind
    {
        get => _kind;
        set
        {
            if (!Enum.IsDefined(value))
            {
                throw new DocumentValidationException("Le type de forme est invalide.", nameof(Kind));
            }

            _kind = value;
        }
    }

    /// <summary>Couleur de remplissage (<see cref="ArgbColor.Transparent"/> = aucun remplissage).</summary>
    public ArgbColor Fill { get; set; }

    /// <summary>Couleur du contour.</summary>
    public ArgbColor Stroke { get; set; }

    /// <summary>Épaisseur du contour, entre 0 et <see cref="Limits.MaxStrokeWidth"/>.</summary>
    /// <exception cref="DocumentValidationException">Épaisseur hors plage ou non finie.</exception>
    public double StrokeWidth
    {
        get => _strokeWidth;
        set
        {
            DocumentRules.Enforce(DocumentRules.CheckStrokeWidth(value), nameof(StrokeWidth));
            _strokeWidth = value;
        }
    }

    /// <inheritdoc />
    public override ShapeElement Clone() => new(Guid.NewGuid(), Kind, Frame, Fill, Stroke, StrokeWidth);
}
