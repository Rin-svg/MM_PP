namespace SlideStudio.Core.Editing.Commands;

/// <summary>
/// Change une propriété d'un objet (cadre, texte, couleur…) et sait revenir à l'ancienne valeur.
/// Une seule classe générique remplace des dizaines de commandes quasi identiques.
/// </summary>
/// <remarks>
/// FUSION : deux modifications de la même propriété du même objet, appartenant au même geste
/// (<c>gestureId</c> identique et non nul), n'occupent qu'une seule entrée d'historique. L'interface crée un
/// identifiant de geste au début d'un glisser (ou d'une série de frappes) et le réutilise jusqu'au relâchement.
/// </remarks>
/// <typeparam name="TTarget">Type de l'objet modifié.</typeparam>
/// <typeparam name="TValue">Type de la valeur modifiée.</typeparam>
public sealed class SetPropertyCommand<TTarget, TValue> : IMergeableCommand
    where TTarget : class
{
    private readonly TTarget _target;
    private readonly string _propertyName;
    private readonly Func<TTarget, TValue> _getter;
    private readonly Action<TTarget, TValue> _setter;
    private readonly Guid? _gestureId;
    private TValue _newValue;
    private TValue _oldValue;

    /// <summary>Prépare la modification (rien n'est modifié avant <see cref="Execute"/>).</summary>
    /// <param name="target">Objet à modifier.</param>
    /// <param name="propertyName">Nom de la propriété (sert à décider de la fusion).</param>
    /// <param name="description">Texte du menu.</param>
    /// <param name="getter">Lit la valeur actuelle.</param>
    /// <param name="setter">Écrit une valeur (c'est lui qui valide : il lève <see cref="DocumentValidationException"/>).</param>
    /// <param name="newValue">Valeur à appliquer.</param>
    /// <param name="gestureId">Identifiant du geste en cours, ou <c>null</c> si l'action est isolée.</param>
    /// <exception cref="ArgumentNullException">Un argument obligatoire est <c>null</c>.</exception>
    public SetPropertyCommand(
        TTarget target,
        string propertyName,
        string description,
        Func<TTarget, TValue> getter,
        Action<TTarget, TValue> setter,
        TValue newValue,
        Guid? gestureId = null)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentException.ThrowIfNullOrWhiteSpace(propertyName);
        ArgumentException.ThrowIfNullOrWhiteSpace(description);
        ArgumentNullException.ThrowIfNull(getter);
        ArgumentNullException.ThrowIfNull(setter);

        _target = target;
        _propertyName = propertyName;
        Description = description;
        _getter = getter;
        _setter = setter;
        _newValue = newValue;
        _oldValue = getter(target);
        _gestureId = gestureId;
    }

    /// <inheritdoc />
    public string Description { get; }

    /// <inheritdoc />
    public bool IsNoOp => EqualityComparer<TValue>.Default.Equals(_getter(_target), _newValue);

    /// <inheritdoc />
    public void Execute()
    {
        // L'ancienne valeur est relue à CHAQUE exécution (y compris « rétablir ») : c'est la valeur réellement
        // présente juste avant, pas celle qu'on supposait à la construction.
        TValue previous = _getter(_target);

        // Le setter valide d'abord puis affecte : s'il lève une exception, la propriété est inchangée
        // et _oldValue n'est pas touché.
        _setter(_target, _newValue);
        _oldValue = previous;
    }

    /// <inheritdoc />
    public void Undo() => _setter(_target, _oldValue);

    /// <inheritdoc />
    public bool TryMergeWith(IUndoableCommand followingCommand)
    {
        if (_gestureId is null
            || followingCommand is not SetPropertyCommand<TTarget, TValue> other
            || other._gestureId != _gestureId
            || !ReferenceEquals(other._target, _target)
            || other._propertyName != _propertyName)
        {
            return false;
        }

        // On garde l'ANCIENNE valeur d'origine (début du geste) et on adopte la NOUVELLE valeur finale :
        // une seule annulation ramène au début du glisser, un seul rétablissement va à la fin.
        _newValue = other._newValue;
        return true;
    }
}
