namespace SlideStudio.Core;

/// <summary>
/// Toutes les limites numériques du modèle de présentation, regroupées au même endroit.
/// </summary>
/// <remarks>
/// POURQUOI centraliser : ces valeurs sont précisément celles que la sécurité et les tests manipulent
/// (« max », « max + 1 »). Un fichier piégé qui annonce 10 millions d'éléments ou une taille « NaN »
/// ne doit jamais pouvoir saturer la mémoire ou corrompre l'affichage.
/// </remarks>
public static class Limits
{
    /// <summary>Largeur d'une diapositive, en unités logiques (format 16:9, comme 1280×720).</summary>
    public const double SlideWidth = 1280;

    /// <summary>Hauteur d'une diapositive, en unités logiques.</summary>
    public const double SlideHeight = 720;

    // LIMITE : 500 diapositives. Au-delà, le panneau de miniatures et l'enregistrement deviennent lents ;
    // un fichier hostile ne peut pas non plus nous faire allouer un nombre illimité de diapositives.
    /// <summary>Nombre maximal de diapositives dans une présentation.</summary>
    public const int MaxSlides = 500;

    // LIMITE : 300 éléments par diapositive. Une diapositive lisible en contient rarement plus de 30 ;
    // 300 laisse une très grande marge tout en bornant le coût de dessin et de désérialisation.
    /// <summary>Nombre maximal d'éléments sur une diapositive.</summary>
    public const int MaxElementsPerSlide = 300;

    // LIMITE : 50 000 caractères par zone de texte (environ 25 pages). Un collage de 10 Mo est refusé
    // proprement au lieu de geler le dessin du texte.
    /// <summary>Nombre maximal de caractères dans une zone de texte.</summary>
    public const int MaxTextLength = 50_000;

    /// <summary>Nombre maximal de caractères dans le titre de la présentation.</summary>
    public const int MaxTitleLength = 200;

    /// <summary>Nombre maximal de caractères dans le texte alternatif d'une image.</summary>
    public const int MaxAltTextLength = 500;

    // LIMITE : 64 caractères, largement au-dessus du plus long nom de police courant.
    /// <summary>Nombre maximal de caractères dans un nom de police.</summary>
    public const int MaxFontFamilyLength = 64;

    /// <summary>Taille de police minimale, en points.</summary>
    public const double MinFontSizePoints = 6;

    /// <summary>Taille de police maximale, en points.</summary>
    public const double MaxFontSizePoints = 400;

    /// <summary>Épaisseur maximale d'un contour, en unités logiques.</summary>
    public const double MaxStrokeWidth = 100;

    // LIMITE : on autorise un élément à dépasser la diapositive (utile pour les animations d'entrée et le
    // rognage), mais pas à partir à l'infini : des coordonnées énormes provoquent des erreurs de dessin.
    /// <summary>Valeur absolue maximale des coordonnées X et Y d'un élément.</summary>
    public const double MaxCoordinate = 20_000;

    /// <summary>Largeur ou hauteur minimale d'un élément (jamais nulle : un élément doit rester sélectionnable).</summary>
    public const double MinElementSize = 1;

    /// <summary>Largeur ou hauteur maximale d'un élément.</summary>
    public const double MaxElementSize = 20_000;

    /// <summary>Nombre maximal de caractères dans le nom d'une image embarquée.</summary>
    public const int MaxMediaNameLength = 100;

    /// <summary>Taille maximale d'une image embarquée, en octets.</summary>
    public const int MaxMediaBytes = 20 * 1024 * 1024;

    /// <summary>Taille maximale cumulée des médias d'un document, en octets.</summary>
    public const long MaxTotalMediaBytes = 100L * 1024 * 1024;

    /// <summary>Nombre maximal d'images embarquées dans un document.</summary>
    public const int MaxMediaCount = 1_000;

    /// <summary>Taille maximale d'une archive .slds, en octets.</summary>
    public const long MaxDocumentBytes = 120L * 1024 * 1024;

    /// <summary>Largeur ou hauteur maximale d'une image, en pixels.</summary>
    public const int MaxImageDimension = 8_192;

    /// <summary>Nombre maximal de pixels d'une image, pour limiter le coût de décodage ultérieur.</summary>
    public const long MaxImagePixels = 40_000_000;

    // LIMITE : 200 actions annulables. Chaque action garde des références en mémoire : on borne l'historique.
    /// <summary>Nombre maximal d'actions conservées dans l'historique annuler/rétablir.</summary>
    public const int MaxHistoryDepth = 200;
}
