namespace SlideStudio.Core.Model;

/// <summary>
/// Élément posé sur une diapositive (texte, forme, image…). Classe de base : identité et cadre.
/// </summary>
/// <remarks>
/// POURQUOI chaque élément a un <see cref="Id"/> : l'historique annuler/rétablir et l'interface retrouvent un
/// élément par son identité, pas par sa position dans la liste (qui change quand on réordonne).
/// ATTENTION : toute modification venant de l'interface doit passer par une commande
/// (<see cref="Editing.UndoHistory"/>), sinon elle ne sera pas annulable.
/// </remarks>
public abstract class SlideElement
{
    private ElementFrame _frame;

    /// <summary>Initialise l'identité et le cadre (validés).</summary>
    /// <param name="id">Identifiant unique, jamais <see cref="Guid.Empty"/>.</param>
    /// <param name="frame">Cadre initial.</param>
    /// <exception cref="DocumentValidationException">L'identifiant ou le cadre est refusé.</exception>
    protected SlideElement(Guid id, ElementFrame frame)
    {
        // SÉCURITÉ : un fichier piégé peut fournir 00000000-… ; cette valeur est réservée à « pas d'identifiant ».
        if (id == Guid.Empty)
        {
            throw new DocumentValidationException("L'identifiant de l'élément est invalide.", nameof(Id));
        }

        Id = id;
        Frame = frame;
    }

    /// <summary>Identifiant unique de l'élément (stable pendant toute sa vie).</summary>
    public Guid Id { get; }

    /// <summary>Position, taille et rotation. L'affectation est validée : une valeur refusée ne change rien.</summary>
    /// <exception cref="DocumentValidationException">Le cadre est hors limites ou contient NaN/infini.</exception>
    public ElementFrame Frame
    {
        get => _frame;
        set
        {
            DocumentRules.Enforce(DocumentRules.CheckFrame(value), nameof(Frame));
            _frame = value;
        }
    }

    /// <summary>Crée une copie profonde avec un <b>nouvel</b> identifiant (copier-coller, duplication de diapositive).</summary>
    /// <returns>Copie indépendante de l'original.</returns>
    public abstract SlideElement Clone();
}
