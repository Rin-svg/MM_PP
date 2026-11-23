namespace SlideStudio.Core.Model;

/// <summary>
/// Image posée sur une diapositive. Le modèle ne garde que le <b>nom</b> du fichier embarqué ;
/// les octets de l'image vivent dans le fichier de présentation (gérés par la couche Infrastructure).
/// </summary>
/// <remarks>
/// POURQUOI ne pas stocker les octets ici : le Core reste léger et testable, et la taille et les dimensions
/// d'une image doivent être vérifiées par le code qui la lit, avant décodage (menace : image géante).
/// </remarks>
public sealed class ImageElement : SlideElement
{
    private string _mediaName = string.Empty;
    private string _altText = string.Empty;

    /// <summary>Crée une image avec un nouvel identifiant et sans texte alternatif.</summary>
    /// <param name="frame">Position et taille.</param>
    /// <param name="mediaName">Nom du fichier embarqué (voir <see cref="DocumentRules.CheckMediaName"/>).</param>
    /// <exception cref="DocumentValidationException">Une valeur est refusée.</exception>
    public ImageElement(ElementFrame frame, string mediaName)
        : this(Guid.NewGuid(), frame, mediaName, string.Empty)
    {
    }

    /// <summary>Crée une image complète (utilisé notamment par la lecture d'un fichier).</summary>
    /// <param name="id">Identifiant existant.</param>
    /// <param name="frame">Position et taille.</param>
    /// <param name="mediaName">Nom du fichier embarqué.</param>
    /// <param name="altText">Description pour les lecteurs d'écran.</param>
    /// <exception cref="DocumentValidationException">Une valeur est refusée.</exception>
    public ImageElement(Guid id, ElementFrame frame, string mediaName, string altText)
        : base(id, frame)
    {
        MediaName = mediaName;
        AltText = altText;
    }

    /// <summary>Nom simple du fichier embarqué (pas de dossier, extension d'image connue).</summary>
    /// <exception cref="DocumentValidationException">Nom refusé (voir <see cref="DocumentRules.CheckMediaName"/>).</exception>
    public string MediaName
    {
        get => _mediaName;
        set
        {
            DocumentRules.Enforce(DocumentRules.CheckMediaName(value), nameof(MediaName));
            _mediaName = value;
        }
    }

    /// <summary>Description de l'image pour les lecteurs d'écran (accessibilité).</summary>
    /// <exception cref="DocumentValidationException">Texte trop long ou contenant un caractère interdit.</exception>
    public string AltText
    {
        get => _altText;
        set
        {
            DocumentRules.Enforce(DocumentRules.CheckAltText(value), nameof(AltText));
            _altText = value;
        }
    }

    /// <inheritdoc />
    public override ImageElement Clone() => new(Guid.NewGuid(), Frame, MediaName, AltText);
}
