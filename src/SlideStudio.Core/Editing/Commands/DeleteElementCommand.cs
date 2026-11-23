using SlideStudio.Core.Model;

namespace SlideStudio.Core.Editing.Commands;

/// <summary>Supprime un élément ; l'annulation le remet à son niveau de superposition d'origine.</summary>
public sealed class DeleteElementCommand : IUndoableCommand
{
    private readonly Slide _slide;
    private readonly SlideElement _element;
    private int _index;

    /// <summary>Prépare la suppression.</summary>
    /// <param name="slide">Diapositive cible.</param>
    /// <param name="element">Élément à supprimer.</param>
    /// <exception cref="ArgumentNullException">Un argument est <c>null</c>.</exception>
    public DeleteElementCommand(Slide slide, SlideElement element)
    {
        ArgumentNullException.ThrowIfNull(slide);
        ArgumentNullException.ThrowIfNull(element);
        _slide = slide;
        _element = element;
    }

    /// <inheritdoc />
    public string Description => "Supprimer l'élément";

    /// <inheritdoc />
    public void Execute() => _index = _slide.RemoveElement(_element);

    /// <inheritdoc />
    public void Undo() => _slide.InsertElement(_index, _element);
}
