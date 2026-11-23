namespace SlideStudio.Core.Editing;

/// <summary>
/// Historique annuler / rétablir, avec suivi des modifications non enregistrées.
/// </summary>
/// <remarks>
/// <para>
/// SUIVI DES MODIFICATIONS : chaque état du document reçoit un numéro de version unique. « Enregistrer » mémorise le
/// numéro courant. Le document est « modifié » si et seulement si le numéro courant est différent. Ainsi, annuler jusqu'à
/// l'état enregistré fait disparaître l'astérisque, tandis que « annuler puis faire autre chose » ne peut jamais
/// retomber par hasard sur l'état enregistré (les numéros ne sont jamais réutilisés).
/// </para>
/// <para>Cette classe n'est pas thread-safe : elle est destinée au thread d'interface.</para>
/// </remarks>
public sealed class UndoHistory
{
    private readonly List<Entry> _undoStack = [];
    private readonly List<Entry> _redoStack = [];
    private readonly int _maxDepth;
    private long _versionCounter;
    private long _baseVersion;
    private long _savedVersion;

    /// <summary>Crée un historique vide, considéré comme « enregistré ».</summary>
    /// <param name="maxDepth">Nombre maximal d'actions conservées (de 1 à 10 000).</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxDepth"/> est hors de 1 à 10 000.</exception>
    public UndoHistory(int maxDepth = Limits.MaxHistoryDepth)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(maxDepth, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(maxDepth, 10_000);
        _maxDepth = maxDepth;
    }

    /// <summary>Levé après chaque changement (exécution, annulation, rétablissement, enregistrement, vidage).</summary>
    public event EventHandler? Changed;

    /// <summary>Vrai s'il y a quelque chose à annuler.</summary>
    public bool CanUndo => _undoStack.Count > 0;

    /// <summary>Vrai s'il y a quelque chose à rétablir.</summary>
    public bool CanRedo => _redoStack.Count > 0;

    /// <summary>Description de la prochaine annulation, ou <c>null</c>.</summary>
    public string? UndoDescription => CanUndo ? _undoStack[^1].Command.Description : null;

    /// <summary>Description du prochain rétablissement, ou <c>null</c>.</summary>
    public string? RedoDescription => CanRedo ? _redoStack[^1].Command.Description : null;

    /// <summary>Vrai si le document diffère de la dernière version enregistrée.</summary>
    public bool IsModified => CurrentVersion != _savedVersion;

    private long CurrentVersion => _undoStack.Count > 0 ? _undoStack[^1].Version : _baseVersion;

    /// <summary>Exécute une commande et l'enregistre dans l'historique (ce qui vide la pile « rétablir »).</summary>
    /// <param name="command">Commande à exécuter.</param>
    /// <exception cref="ArgumentNullException"><paramref name="command"/> est <c>null</c>.</exception>
    /// <remarks>
    /// Si la commande lève une exception (valeur refusée, par exemple), l'historique reste inchangé
    /// et l'exception est propagée : l'interface affiche son message.
    /// </remarks>
    public void Execute(IUndoableCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        // UTILISATEUR : un glisser de 0 pixel ou « gras » sur un texte déjà gras ne doit pas
        // polluer l'historique avec une étape d'annulation qui ne fait rien.
        if (command.IsNoOp)
        {
            return;
        }

        // ROBUSTESSE : on exécute AVANT d'enregistrer. Si cela échoue, rien n'est enregistré.
        command.Execute();

        if (!TryMergeIntoLastEntry(command))
        {
            _versionCounter++;
            _undoStack.Add(new Entry(command, _versionCounter));
        }

        // Une nouvelle action crée une nouvelle « branche » : l'ancien futur n'existe plus.
        _redoStack.Clear();
        TrimToMaxDepth();
        RaiseChanged();
    }

    /// <summary>Annule la dernière action.</summary>
    /// <returns><c>false</c> s'il n'y avait rien à annuler (appuyer deux fois sur Ctrl+Z est donc inoffensif).</returns>
    public bool Undo()
    {
        if (_undoStack.Count == 0)
        {
            return false;
        }

        Entry entry = _undoStack[^1];

        // ROBUSTESSE : l'entrée n'est retirée de la pile qu'APRÈS le succès de l'annulation ;
        // si elle échoue, l'historique reste cohérent avec ce qu'il sait de l'état du document.
        entry.Command.Undo();

        _undoStack.RemoveAt(_undoStack.Count - 1);
        _redoStack.Add(entry);
        RaiseChanged();
        return true;
    }

    /// <summary>Rétablit la dernière action annulée.</summary>
    /// <returns><c>false</c> s'il n'y avait rien à rétablir.</returns>
    public bool Redo()
    {
        if (_redoStack.Count == 0)
        {
            return false;
        }

        Entry entry = _redoStack[^1];
        entry.Command.Execute();

        _redoStack.RemoveAt(_redoStack.Count - 1);
        _undoStack.Add(entry);
        RaiseChanged();
        return true;
    }

    /// <summary>Mémorise l'état courant comme « enregistré » (à appeler après une sauvegarde réussie).</summary>
    public void MarkSaved()
    {
        _savedVersion = CurrentVersion;
        RaiseChanged();
    }

    /// <summary>Vide l'historique (nouveau document, ouverture d'un fichier) ; le document est alors « enregistré ».</summary>
    public void Clear()
    {
        _undoStack.Clear();
        _redoStack.Clear();
        _versionCounter++;
        _baseVersion = _versionCounter;
        _savedVersion = _baseVersion;
        RaiseChanged();
    }

    private bool TryMergeIntoLastEntry(IUndoableCommand command)
    {
        if (_undoStack.Count == 0)
        {
            return false;
        }

        // POURQUOI cette condition : fusionner dans l'entrée qui correspond à l'état ENREGISTRÉ modifierait
        // cet état après coup, et « annuler » ne ramènerait plus jamais à la version sur le disque.
        if (CurrentVersion == _savedVersion)
        {
            return false;
        }

        return _undoStack[^1].Command is IMergeableCommand mergeable && mergeable.TryMergeWith(command);
    }

    private void TrimToMaxDepth()
    {
        // LIMITE : on oublie les plus anciennes actions. La version de la dernière action oubliée devient la « base »
        // (l'état obtenu en annulant tout ce qui reste). Si l'état enregistré est plus ancien que cette base, il n'est
        // plus atteignable par annulation : aucune version courante ne lui sera jamais égale, le document reste « modifié ».
        while (_undoStack.Count > _maxDepth)
        {
            _baseVersion = _undoStack[0].Version;
            _undoStack.RemoveAt(0);
        }
    }

    private void RaiseChanged() => Changed?.Invoke(this, EventArgs.Empty);

    private sealed record Entry(IUndoableCommand Command, long Version);
}
