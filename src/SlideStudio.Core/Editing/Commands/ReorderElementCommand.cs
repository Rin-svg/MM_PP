using SlideStudio.Core.Model;

namespace SlideStudio.Core.Editing.Commands;

/// <summary>Change le niveau de superposition d'un élément (premier plan, arrière-plan, d'un cran).</summary>
public sealed class ReorderElementCommand : IUndoableCommand
{
    private readonly Slide _slide;
    private readonly SlideElement _element;
    private readonly int _newIndex;
    private int _oldIndex;

    /// <summary>Prépare le changement d'ordre.</summary>
    /// <param name="slide">Diapositive cible.</param>
    /// <param name="element">Élément à déplacer dans la pile.</param>
    /// <param name="newIndex">Nouvelle position (0 = tout au fond).</param>
    /// <param name="description">Texte du menu ; « Changer l'ordre » par défaut.</param>
    /// <exception cref="ArgumentNullException">Un argument obligatoire est <c>null</c>.</exception>
    public ReorderElementCommand(Slide slide, SlideElement element, int newIndex, string description = "Changer l'ordre")
    {
        ArgumentNullException.ThrowIfNull(slide);
        ArgumentNullException.ThrowIfNull(element);
        ArgumentException.ThrowIfNullOrWhiteSpace(description);
        _slide = slide;
        _element = element;
        _newIndex = newIndex;
        _oldIndex = slide.IndexOf(element);
        Description = description;
    }

    /// <inheritdoc />
    public string Description { get; }

    /// <inheritdoc />
    public bool IsNoOp => _slide.IndexOf(_element) == _newIndex;

    /// <inheritdoc />
    public void Execute()
    {
        _oldIndex = _slide.IndexOf(_element);
        _slide.MoveElement(_element, _newIndex);
    }

    /// <inheritdoc />
    public void Undo() => _slide.MoveElement(_element, _oldIndex);
}
