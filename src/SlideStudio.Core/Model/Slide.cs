using System.Collections.ObjectModel;

namespace SlideStudio.Core.Model;

/// <summary>
/// Une diapositive : un fond et une pile d'éléments. L'ordre de la liste est l'ordre de superposition
/// (le premier est tout au fond, le dernier tout devant).
/// </summary>
/// <remarks>
/// Les méthodes de modification sont publiques pour que les commandes d'édition (couche Editing) puissent les appeler,
/// mais l'interface ne doit pas les appeler directement : sinon l'action ne sera pas annulable.
/// Cette classe n'est pas thread-safe : elle est destinée au thread d'interface.
/// </remarks>
public sealed class Slide
{
    private readonly List<SlideElement> _elements = [];
    private readonly ReadOnlyCollection<SlideElement> _elementsView;

    /// <summary>Crée une diapositive vierge à fond blanc.</summary>
    public Slide()
        : this(Guid.NewGuid(), ArgbColor.White, [])
    {
    }

    /// <summary>Crée une diapositive complète (utilisé notamment par la lecture d'un fichier).</summary>
    /// <param name="id">Identifiant existant.</param>
    /// <param name="background">Couleur de fond.</param>
    /// <param name="elements">Éléments, du fond vers le premier plan.</param>
    /// <exception cref="ArgumentNullException"><paramref name="elements"/> est <c>null</c>.</exception>
    /// <exception cref="DocumentValidationException">Identifiant invalide, trop d'éléments ou éléments en double.</exception>
    public Slide(Guid id, ArgbColor background, IEnumerable<SlideElement> elements)
    {
        ArgumentNullException.ThrowIfNull(elements);

        if (id == Guid.Empty)
        {
            throw new DocumentValidationException("L'identifiant de la diapositive est invalide.", nameof(Id));
        }

        Id = id;
        Background = background;
        _elementsView = new ReadOnlyCollection<SlideElement>(_elements);

        // On passe par AddElement pour appliquer exactement les mêmes contrôles (limite, doublons)
        // que pour une insertion normale : une seule règle, un seul endroit.
        foreach (SlideElement element in elements)
        {
            AddElement(element);
        }
    }

    /// <summary>Identifiant unique de la diapositive.</summary>
    public Guid Id { get; }

    /// <summary>Couleur de fond.</summary>
    public ArgbColor Background { get; set; }

    /// <summary>Éléments en lecture seule, du fond vers le premier plan.</summary>
    /// <remarks>POURQUOI une vue en lecture seule : empêcher tout code d'ajouter un élément sans passer par les contrôles.</remarks>
    public IReadOnlyList<SlideElement> Elements => _elementsView;

    /// <summary>Ajoute un élément au premier plan.</summary>
    /// <param name="element">Élément à ajouter.</param>
    /// <exception cref="ArgumentNullException"><paramref name="element"/> est <c>null</c>.</exception>
    /// <exception cref="DocumentValidationException">Diapositive pleine ou élément déjà présent.</exception>
    public void AddElement(SlideElement element) => InsertElement(_elements.Count, element);

    /// <summary>Insère un élément à une position précise de la pile.</summary>
    /// <param name="index">Position, de 0 (fond) à <c>Elements.Count</c> (premier plan).</param>
    /// <param name="element">Élément à insérer.</param>
    /// <exception cref="ArgumentNullException"><paramref name="element"/> est <c>null</c>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> est hors de la pile.</exception>
    /// <exception cref="DocumentValidationException">Diapositive pleine ou élément déjà présent.</exception>
    public void InsertElement(int index, SlideElement element)
    {
        ArgumentNullException.ThrowIfNull(element);
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(index, _elements.Count);

        // Tous les contrôles passent AVANT la modification : en cas d'échec, la diapositive est inchangée.
        if (_elements.Count >= Limits.MaxElementsPerSlide)
        {
            throw new DocumentValidationException(
                FormattableString.Invariant(
                    $"Une diapositive ne peut pas contenir plus de {Limits.MaxElementsPerSlide} éléments."));
        }

        // SÉCURITÉ / ROBUSTESSE : deux éléments de même identifiant rendraient l'annulation ambiguë.
        // Un fichier piégé peut en contenir ; on refuse plutôt que d'en écraser un.
        if (_elements.Exists(existing => existing.Id == element.Id))
        {
            throw new DocumentValidationException("Cet élément est déjà présent sur la diapositive.");
        }

        _elements.Insert(index, element);
    }

    /// <summary>Retire un élément et renvoie sa position, pour pouvoir le remettre au même endroit (annulation).</summary>
    /// <param name="element">Élément à retirer.</param>
    /// <returns>Position qu'occupait l'élément.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="element"/> est <c>null</c>.</exception>
    /// <exception cref="InvalidOperationException">L'élément n'appartient pas à cette diapositive (erreur de programmation).</exception>
    public int RemoveElement(SlideElement element)
    {
        ArgumentNullException.ThrowIfNull(element);

        int index = _elements.IndexOf(element);
        if (index < 0)
        {
            throw new InvalidOperationException("L'élément n'appartient pas à cette diapositive.");
        }

        _elements.RemoveAt(index);
        return index;
    }

    /// <summary>Déplace un élément dans la pile (premier plan, arrière-plan, d'un cran…).</summary>
    /// <param name="element">Élément à déplacer.</param>
    /// <param name="newIndex">Nouvelle position, de 0 à <c>Elements.Count - 1</c>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="element"/> est <c>null</c>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="newIndex"/> est hors de la pile.</exception>
    /// <exception cref="InvalidOperationException">L'élément n'appartient pas à cette diapositive.</exception>
    public void MoveElement(SlideElement element, int newIndex)
    {
        ArgumentNullException.ThrowIfNull(element);
        ArgumentOutOfRangeException.ThrowIfNegative(newIndex);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(newIndex, _elements.Count);

        int oldIndex = _elements.IndexOf(element);
        if (oldIndex < 0)
        {
            throw new InvalidOperationException("L'élément n'appartient pas à cette diapositive.");
        }

        _elements.RemoveAt(oldIndex);
        _elements.Insert(newIndex, element);
    }

    /// <summary>Renvoie la position d'un élément dans la pile, ou -1 s'il n'y est pas.</summary>
    /// <param name="element">Élément recherché.</param>
    /// <returns>Position ou -1.</returns>
    public int IndexOf(SlideElement element) => _elements.IndexOf(element);

    /// <summary>Crée une copie profonde : nouvel identifiant de diapositive <b>et</b> nouveaux identifiants d'éléments.</summary>
    /// <returns>Diapositive indépendante de l'original (modifier l'une ne change pas l'autre).</returns>
    public Slide Clone() => new(Guid.NewGuid(), Background, _elements.Select(element => element.Clone()));
}
