using SlideStudio.Core.Model;

namespace SlideStudio.Core;

/// <summary>Service d'ouverture et d'enregistrement d'une présentation.</summary>
public interface IDocumentStorage
{
    /// <summary>Lit une présentation depuis un fichier.</summary>
    Presentation Open(string fileName);

    /// <summary>Écrit une présentation dans un fichier.</summary>
    void Save(Presentation presentation, string fileName);
}
