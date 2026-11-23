using SlideStudio.Core.Model;

namespace SlideStudio.Core.Editing.Commands;

/// <summary>Déplace une diapositive (glisser-déposer dans le panneau de miniatures).</summary>
public sealed class MoveSlideCommand : IUndoableCommand
{
    private readonly Presentation _presentation;
    private readonly Slide _slide;
    private readonly int _newIndex;
    private int _oldIndex;

    /// <summary>Prépare le déplacement.</summary>
    /// <param name="presentation">Présentation cible.</param>
    /// <param name="slide">Diapositive à déplacer.</param>
    /// <param name="newIndex">Nouvelle position.</param>
    /// <exception cref="ArgumentNullException">Un argument est <c>null</c>.</exception>
    public MoveSlideCommand(Presentation presentation, Slide slide, int newIndex)
    {
        ArgumentNullException.ThrowIfNull(presentation);
        ArgumentNullException.ThrowIfNull(slide);
        _presentation = presentation;
        _slide = slide;
        _newIndex = newIndex;
        _oldIndex = presentation.IndexOf(slide);
    }

    /// <inheritdoc />
    public string Description => "Déplacer la diapositive";

    /// <inheritdoc />
    public bool IsNoOp => _presentation.IndexOf(_slide) == _newIndex;

    /// <inheritdoc />
    public void Execute()
    {
        _oldIndex = _presentation.IndexOf(_slide);
        _presentation.MoveSlide(_slide, _newIndex);
    }

    /// <inheritdoc />
    public void Undo() => _presentation.MoveSlide(_slide, _oldIndex);
}
