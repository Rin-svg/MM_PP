using System.Text.Json.Serialization;

namespace SlideStudio.Infrastructure.FileFormat;

/// <summary>
/// Racine du format JSON contenu dans une archive .slds.
/// </summary>
public sealed class SldsDocumentDto
{
    /// <summary>Version du format.</summary>
    [JsonPropertyName("formatVersion")]
    public string FormatVersion { get; set; } = "1.0";

    /// <summary>Nom de l'application créatrice.</summary>
    [JsonPropertyName("app")]
    public string App { get; set; } = "SlideStudio";

    /// <summary>Présentation sérialisée.</summary>
    [JsonPropertyName("presentation")]
    public PresentationDto Presentation { get; set; } = new();
}

/// <summary>Présentation dans le format .slds.</summary>
public sealed class PresentationDto
{
    /// <summary>Titre.</summary>
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    /// <summary>Largeur logique.</summary>
    [JsonPropertyName("slideWidth")]
    public double SlideWidth { get; set; } = 1280;

    /// <summary>Hauteur logique.</summary>
    [JsonPropertyName("slideHeight")]
    public double SlideHeight { get; set; } = 720;

    /// <summary>Diapositives dans l'ordre.</summary>
    [JsonPropertyName("slides")]
    public List<SlideDto> Slides { get; set; } = [];
}

/// <summary>Diapositive dans le format .slds.</summary>
public sealed class SlideDto
{
    /// <summary>Identifiant.</summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>Couleur de fond.</summary>
    [JsonPropertyName("background")]
    public string Background { get; set; } = "#FFFFFF";

    /// <summary>Éléments du fond vers le premier plan.</summary>
    [JsonPropertyName("elements")]
    public List<ElementDto> Elements { get; set; } = [];
}

/// <summary>Élément polymorphique dans le format .slds.</summary>
public sealed class ElementDto
{
    /// <summary>Type polymorphique de l'élément.</summary>
    [JsonPropertyName("type")] public string Type { get; set; } = string.Empty;
    /// <summary>Identifiant stable.</summary>
    [JsonPropertyName("id")] public string Id { get; set; } = string.Empty;
    /// <summary>Cadre géométrique.</summary>
    [JsonPropertyName("frame")] public FrameDto Frame { get; set; } = new();
    /// <summary>Contenu texte, si applicable.</summary>
    [JsonPropertyName("text")] public string? Text { get; set; }
    /// <summary>Police du texte, si applicable.</summary>
    [JsonPropertyName("fontFamily")] public string? FontFamily { get; set; }
    /// <summary>Taille du texte en points, si applicable.</summary>
    [JsonPropertyName("fontSizePoints")] public double? FontSizePoints { get; set; }
    /// <summary>Indicateur gras, si applicable.</summary>
    [JsonPropertyName("isBold")] public bool? IsBold { get; set; }
    /// <summary>Indicateur italique, si applicable.</summary>
    [JsonPropertyName("isItalic")] public bool? IsItalic { get; set; }
    /// <summary>Couleur du texte, si applicable.</summary>
    [JsonPropertyName("textColor")] public string? TextColor { get; set; }
    /// <summary>Alignement du texte, si applicable.</summary>
    [JsonPropertyName("alignment")] public string? Alignment { get; set; }
    /// <summary>Type de forme, si applicable.</summary>
    [JsonPropertyName("shapeKind")] public string? ShapeKind { get; set; }
    /// <summary>Remplissage de forme, si applicable.</summary>
    [JsonPropertyName("fill")] public string? Fill { get; set; }
    /// <summary>Contour de forme, si applicable.</summary>
    [JsonPropertyName("stroke")] public string? Stroke { get; set; }
    /// <summary>Épaisseur du contour, si applicable.</summary>
    [JsonPropertyName("strokeWidth")] public double? StrokeWidth { get; set; }
    /// <summary>Nom du média embarqué, si applicable.</summary>
    [JsonPropertyName("mediaName")] public string? MediaName { get; set; }
    /// <summary>Texte alternatif de l'image, si applicable.</summary>
    [JsonPropertyName("altText")] public string? AltText { get; set; }
}

/// <summary>Cadre géométrique dans le format .slds.</summary>
public sealed class FrameDto
{
    /// <summary>Abscisse.</summary>
    [JsonPropertyName("x")] public double X { get; set; }
    /// <summary>Ordonnée.</summary>
    [JsonPropertyName("y")] public double Y { get; set; }
    /// <summary>Largeur.</summary>
    [JsonPropertyName("width")] public double Width { get; set; }
    /// <summary>Hauteur.</summary>
    [JsonPropertyName("height")] public double Height { get; set; }
    /// <summary>Rotation en degrés.</summary>
    [JsonPropertyName("rotationDegrees")] public double RotationDegrees { get; set; }
}