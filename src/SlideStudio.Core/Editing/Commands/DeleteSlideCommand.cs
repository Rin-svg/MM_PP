using SlideStudio.Core.Model;

namespace SlideStudio.Core.Editing.Commands;

/// <summary>Supprime une diapositive ; l'annulation la remet exactement à la même place.</summary>
public sealed class DeleteSlideCommand : IUndoableCommand
{
    private readonly Presentation _presentation;
    private readonly Slide _slide;
    private int _index;

    /// <summary>Prépare la suppression (rien n'est modifié avant <see cref="Execute"/>).</summary>
    /// <param name="presentation">Présentation cible.</param>
    /// <param name="slide">Diapositive à supprimer.</param>
    /// <exception cref="ArgumentNullException">Un argument est <c>null</c>.</exception>
    public DeleteSlideCommand(Presentation presentation, Slide slide)
    {
        ArgumentNullException.ThrowIfNull(presentation);
        ArgumentNullException.ThrowIfNull(slide);
        _presentation = presentation;
        _slide = slide;
    }

    /// <inheritdoc />
    public string Description => "Supprimer la diapositive";

    /// <inheritdoc />
    /// <exception cref="DocumentValidationException">C'est la dernière diapositive.</exception>
    public void Execute() =>
        // La position est mémorisée au moment de l'exécution (et non de la construction) : elle peut
        // avoir changé entre-temps si d'autres actions ont été annulées ou rétablies.
        _index = _presentation.RemoveSlide(_slide);

    /// <inheritdoc />
    public void Undo() => _presentation.InsertSlide(_index, _slide);
}
