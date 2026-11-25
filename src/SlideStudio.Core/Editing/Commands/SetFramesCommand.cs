using SlideStudio.Core.Model;

namespace SlideStudio.Core.Editing.Commands;

/// <summary>Modifie plusieurs cadres comme une seule action annulable.</summary>
public sealed class SetFramesCommand : IMergeableCommand
{
    private readonly Entry[] _entries;
    private readonly Guid _gestureId;
    private ElementFrame[] _newFrames;

    /// <summary>Prépare le déplacement ou la transformation groupée d'éléments.</summary>
    public SetFramesCommand(IEnumerable<(SlideElement Element, ElementFrame Frame)> frames, Guid gestureId)
    {
        ArgumentNullException.ThrowIfNull(frames);
        if (gestureId == Guid.Empty)
        {
            throw new ArgumentException("L'identifiant du geste ne peut pas être vide.", nameof(gestureId));
        }

        (SlideElement Element, ElementFrame Frame)[] requestedFrames = frames.ToArray();
        List<Entry> entries = [];
        HashSet<SlideElement> seen = new(ReferenceEqualityComparer.Instance);
        foreach ((SlideElement element, _) in requestedFrames)
        {
            ArgumentNullException.ThrowIfNull(element);
            if (!seen.Add(element))
            {
                throw new ArgumentException("Un élément ne peut apparaître qu'une fois.", nameof(frames));
            }

            entries.Add(new Entry(element, element.Frame));
        }

        if (entries.Count == 0)
        {
            throw new ArgumentException("Au moins un élément est requis.", nameof(frames));
        }

        _entries = entries.ToArray();
        _newFrames = requestedFrames.Select(item => item.Frame).ToArray();
        _gestureId = gestureId;
    }

    /// <inheritdoc />
    public string Description => _entries.Length == 1 ? "Transformer l'élément" : "Déplacer les éléments";

    /// <inheritdoc />
    public bool IsNoOp => _entries.Select((entry, index) => entry.OriginalFrame == _newFrames[index]).All(equal => equal);

    /// <inheritdoc />
    public void Execute() => Apply(_newFrames);

    /// <inheritdoc />
    public void Undo() => Apply(_entries.Select(entry => entry.OriginalFrame).ToArray());

    /// <inheritdoc />
    public bool TryMergeWith(IUndoableCommand followingCommand)
    {
        if (followingCommand is not SetFramesCommand following
            || following._gestureId != _gestureId
            || following._entries.Length != _entries.Length
            || !_entries.Select(entry => entry.Element)
                .SequenceEqual(following._entries.Select(entry => entry.Element), ReferenceEqualityComparer.Instance))
        {
            return false;
        }

        _newFrames = following._newFrames;
        return true;
    }

    private void Apply(ElementFrame[] frames)
    {
        for (int index = 0; index < _entries.Length; index++)
        {
            string? error = DocumentRules.CheckFrame(frames[index]);
            if (error is not null)
            {
                throw new DocumentValidationException(error);
            }
        }

        for (int index = 0; index < _entries.Length; index++)
        {
            _entries[index].Element.Frame = frames[index];
        }
    }

    private sealed record Entry(SlideElement Element, ElementFrame OriginalFrame);
}
