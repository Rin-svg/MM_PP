namespace SlideStudio.Core.Editing;

/// <summary>
/// Commande capable d'absorber la suivante : un glisser de souris produit des dizaines de mises à jour
/// de position, mais l'utilisateur s'attend à une <b>seule</b> étape d'annulation.
/// </summary>
public interface IMergeableCommand : IUndoableCommand
{
    /// <summary>
    /// Tente de fusionner la commande suivante dans celle-ci.
    /// </summary>
    /// <param name="followingCommand">Commande déjà exécutée, qui vient juste après.</param>
    /// <returns>
    /// <c>true</c> si la fusion a eu lieu : <paramref name="followingCommand"/> n'est alors pas ajoutée à l'historique.
    /// </returns>
    bool TryMergeWith(IUndoableCommand followingCommand);
}
