using SlideStudio.Core.Model;

namespace SlideStudio.Core.Editing.Commands;

/// <summary>Ajoute un élément au premier plan d'une diapositive (aussi utilisé pour « Coller »).</summary>
public sealed class AddElementCommand : IUndoableCommand
{
    private readonly Slide _slide;
    private readonly SlideElement _element;

    /// <summary>Prépare l'ajout.</summary>
    /// <param name="slide">Diapositive cible.</param>
    /// <param name="element">Élément à ajouter.</param>
    /// <param name="description">Texte du menu ; « Ajouter un élément » par défaut.</param>
    /// <exception cref="ArgumentNullException">Un argument obligatoire est <c>null</c>.</exception>
    public AddElementCommand(Slide slide, SlideElement element, string description = "Ajouter un élément")
    {
        ArgumentNullException.ThrowIfNull(slide);
        ArgumentNullException.ThrowIfNull(element);
        ArgumentException.ThrowIfNullOrWhiteSpace(description);
        _slide = slide;
        _element = element;
        Description = description;
    }

    /// <inheritdoc />
    public string Description { get; }

    /// <inheritdoc />
    public void Execute() => _slide.AddElement(_element);

    /// <inheritdoc />
    public void Undo() => _slide.RemoveElement(_element);
}
