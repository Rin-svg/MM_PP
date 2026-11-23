using SlideStudio.Core.Model;

namespace SlideStudio.Core.Editing.Commands;

/// <summary>
/// Insère une diapositive. Sert aussi à « Dupliquer » : on lui donne <see cref="Slide.Clone"/> et la position suivante.
/// </summary>
public sealed class AddSlideCommand : IUndoableCommand
{
    private readonly Presentation _presentation;
    private readonly Slide _slide;
    private readonly int _index;

    /// <summary>Prépare l'insertion (rien n'est modifié avant <see cref="Execute"/>).</summary>
    /// <param name="presentation">Présentation cible.</param>
    /// <param name="slide">Diapositive à insérer.</param>
    /// <param name="index">Position d'insertion.</param>
    /// <param name="description">Texte du menu ; « Ajouter une diapositive » par défaut.</param>
    /// <exception cref="ArgumentNullException">Un argument obligatoire est <c>null</c>.</exception>
    public AddSlideCommand(Presentation presentation, Slide slide, int index, string description = "Ajouter une diapositive")
    {
        ArgumentNullException.ThrowIfNull(presentation);
        ArgumentNullException.ThrowIfNull(slide);
        ArgumentException.ThrowIfNullOrWhiteSpace(description);
        _presentation = presentation;
        _slide = slide;
        _index = index;
        Description = description;
    }

    /// <inheritdoc />
    public string Description { get; }

    /// <inheritdoc />
    public void Execute() => _presentation.InsertSlide(_index, _slide);

    /// <inheritdoc />
    public void Undo() => _presentation.RemoveSlide(_slide);
}
