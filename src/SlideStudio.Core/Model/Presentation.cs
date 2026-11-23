using System.Collections.ObjectModel;

namespace SlideStudio.Core.Model;

/// <summary>
/// Un document de présentation : un titre et une suite ordonnée de diapositives (au moins une).
/// </summary>
/// <remarks>
/// Comme pour <see cref="Slide"/>, les méthodes de modification sont destinées aux commandes d'édition.
/// Cette classe n'est pas thread-safe : elle est destinée au thread d'interface.
/// </remarks>
public sealed class Presentation
{
    /// <summary>Titre proposé pour une nouvelle présentation.</summary>
    public const string DefaultTitle = "Présentation sans titre";

    private readonly List<Slide> _slides = [];
    private readonly ReadOnlyCollection<Slide> _slidesView;
    private string _title = DefaultTitle;

    /// <summary>Crée une présentation vierge : titre par défaut et une diapositive vide.</summary>
    public Presentation()
        : this(DefaultTitle, [new Slide()])
    {
    }

    /// <summary>Crée une présentation complète (utilisé notamment par la lecture d'un fichier).</summary>
    /// <param name="title">Titre.</param>
    /// <param name="slides">Diapositives dans l'ordre (au moins une).</param>
    /// <exception cref="ArgumentNullException"><paramref name="slides"/> est <c>null</c>.</exception>
    /// <exception cref="DocumentValidationException">Titre refusé, aucune diapositive, trop de diapositives ou doublons.</exception>
    public Presentation(string title, IEnumerable<Slide> slides)
    {
        ArgumentNullException.ThrowIfNull(slides);

        Title = title;
        _slidesView = new ReadOnlyCollection<Slide>(_slides);

        foreach (Slide slide in slides)
        {
            InsertSlide(_slides.Count, slide);
        }

        // INVARIANT : une présentation n'est jamais vide, l'interface a toujours quelque chose à afficher.
        if (_slides.Count == 0)
        {
            throw new DocumentValidationException("Une présentation doit contenir au moins une diapositive.");
        }
    }

    /// <summary>Titre de la présentation (une seule ligne, limité à <see cref="Limits.MaxTitleLength"/>).</summary>
    /// <exception cref="DocumentValidationException">Titre trop long ou contenant un caractère interdit.</exception>
    public string Title
    {
        get => _title;
        set
        {
            DocumentRules.Enforce(DocumentRules.CheckTitle(value), nameof(Title));
            _title = value;
        }
    }

    /// <summary>Largeur des diapositives en unités logiques.</summary>
    public static double SlideWidth => Limits.SlideWidth;

    /// <summary>Hauteur des diapositives en unités logiques.</summary>
    public static double SlideHeight => Limits.SlideHeight;

    /// <summary>Diapositives en lecture seule, dans l'ordre de présentation.</summary>
    public IReadOnlyList<Slide> Slides => _slidesView;

    /// <summary>Insère une diapositive à une position donnée.</summary>
    /// <param name="index">Position, de 0 à <c>Slides.Count</c>.</param>
    /// <param name="slide">Diapositive à insérer.</param>
    /// <exception cref="ArgumentNullException"><paramref name="slide"/> est <c>null</c>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> est hors de la liste.</exception>
    /// <exception cref="DocumentValidationException">Présentation pleine ou diapositive déjà présente.</exception>
    public void InsertSlide(int index, Slide slide)
    {
        ArgumentNullException.ThrowIfNull(slide);
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(index, _slides.Count);

        if (_slides.Count >= Limits.MaxSlides)
        {
            throw new DocumentValidationException(
                FormattableString.Invariant($"Une présentation ne peut pas contenir plus de {Limits.MaxSlides} diapositives."));
        }

        if (_slides.Exists(existing => existing.Id == slide.Id))
        {
            throw new DocumentValidationException("Cette diapositive est déjà présente dans la présentation.");
        }

        _slides.Insert(index, slide);
    }

    /// <summary>Retire une diapositive et renvoie sa position (pour l'annulation).</summary>
    /// <param name="slide">Diapositive à retirer.</param>
    /// <returns>Position qu'occupait la diapositive.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="slide"/> est <c>null</c>.</exception>
    /// <exception cref="DocumentValidationException">C'est la dernière diapositive.</exception>
    /// <exception cref="InvalidOperationException">La diapositive n'appartient pas à la présentation.</exception>
    public int RemoveSlide(Slide slide)
    {
        ArgumentNullException.ThrowIfNull(slide);

        int index = _slides.IndexOf(slide);
        if (index < 0)
        {
            throw new InvalidOperationException("La diapositive n'appartient pas à cette présentation.");
        }

        // UTILISATEUR : supprimer la dernière diapositive est refusé avec un message clair
        // (l'interface grise aussi la commande, mais le modèle reste la barrière).
        if (_slides.Count == 1)
        {
            throw new DocumentValidationException(
                "Impossible de supprimer la dernière diapositive : une présentation doit en contenir au moins une.");
        }

        _slides.RemoveAt(index);
        return index;
    }

    /// <summary>Déplace une diapositive dans la liste (glisser-déposer dans le panneau de miniatures).</summary>
    /// <param name="slide">Diapositive à déplacer.</param>
    /// <param name="newIndex">Nouvelle position, de 0 à <c>Slides.Count - 1</c>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="slide"/> est <c>null</c>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="newIndex"/> est hors de la liste.</exception>
    /// <exception cref="InvalidOperationException">La diapositive n'appartient pas à la présentation.</exception>
    public void MoveSlide(Slide slide, int newIndex)
    {
        ArgumentNullException.ThrowIfNull(slide);
        ArgumentOutOfRangeException.ThrowIfNegative(newIndex);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(newIndex, _slides.Count);

        int oldIndex = _slides.IndexOf(slide);
        if (oldIndex < 0)
        {
            throw new InvalidOperationException("La diapositive n'appartient pas à cette présentation.");
        }

        _slides.RemoveAt(oldIndex);
        _slides.Insert(newIndex, slide);
    }

    /// <summary>Renvoie la position d'une diapositive, ou -1 si elle n'y est pas.</summary>
    /// <param name="slide">Diapositive recherchée.</param>
    /// <returns>Position ou -1.</returns>
    public int IndexOf(Slide slide) => _slides.IndexOf(slide);
}
