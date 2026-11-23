namespace SlideStudio.Core.Editing;

/// <summary>
/// Une action de l'utilisateur qui sait s'exécuter et s'annuler (patron « Commande »).
/// </summary>
/// <remarks>
/// <para>
/// CONTRAT : si <see cref="Execute"/> lève une exception, l'état du document doit être resté inchangé
/// (valider d'abord, modifier ensuite). L'historique s'appuie sur cette garantie pour ne jamais enregistrer
/// une action qui n'a pas eu lieu.
/// </para>
/// <para>
/// POURQUOI les commandes gardent des références vers les objets (et non des copies) : après « annuler »
/// puis « rétablir », c'est le <i>même</i> objet qui revient, donc les autres commandes de l'historique qui le
/// référencent restent valides.
/// </para>
/// </remarks>
public interface IUndoableCommand
{
    /// <summary>Texte affiché dans le menu (« Annuler : Supprimer la diapositive »).</summary>
    string Description { get; }

    /// <summary>
    /// Vrai si l'exécution ne changerait rien (par exemple appliquer le gras à un texte déjà en gras).
    /// L'historique ignore alors la commande. À évaluer <b>avant</b> l'exécution.
    /// </summary>
    bool IsNoOp => false;

    /// <summary>Applique l'action (première fois, ou « rétablir »).</summary>
    void Execute();

    /// <summary>Annule l'action et remet le document dans l'état d'avant <see cref="Execute"/>.</summary>
    void Undo();
}
