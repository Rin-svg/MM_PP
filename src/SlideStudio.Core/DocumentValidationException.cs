namespace SlideStudio.Core;

/// <summary>
/// Levée quand une valeur destinée au document est refusée (trop longue, hors plage, caractère interdit…).
/// </summary>
/// <remarks>
/// POURQUOI une exception dédiée : l'interface peut l'attraper séparément des erreurs d'entrée/sortie
/// et afficher son message tel quel. SÉCURITÉ : le message ne contient jamais de chemin interne ni de trace.
/// Ce n'est pas un bug du programme mais un cas normal (saisie invalide, fichier corrompu).
/// </remarks>
public sealed class DocumentValidationException : Exception
{
    /// <summary>Crée l'exception avec un message sûr, affichable à l'utilisateur.</summary>
    /// <param name="message">Message en français : quoi, pourquoi, que faire.</param>
    public DocumentValidationException(string message) : base(message)
    {
    }

    /// <summary>Constructeur sans paramètre, requis par les analyseurs pour une exception publique.</summary>
    public DocumentValidationException() : base("La valeur est refusée.")
    {
    }

    /// <summary>Crée l'exception avec un message et une exception interne.</summary>
    /// <param name="message">Message affichable.</param>
    /// <param name="innerException">Cause technique (pour le journal uniquement).</param>
    public DocumentValidationException(string message, Exception innerException) : base(message, innerException)
    {
    }

    /// <summary>Crée l'exception en précisant la propriété concernée (pour surligner le bon champ).</summary>
    /// <param name="message">Message affichable.</param>
    /// <param name="propertyName">Nom de la propriété refusée.</param>
    public DocumentValidationException(string message, string propertyName) : base(message)
    {
        PropertyName = propertyName;
    }

    /// <summary>Nom de la propriété refusée, ou <c>null</c> si non applicable.</summary>
    public string? PropertyName { get; }
}
