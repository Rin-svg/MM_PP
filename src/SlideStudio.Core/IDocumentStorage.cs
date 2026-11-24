using SlideStudio.Core.Model;

namespace SlideStudio.Core;

/// <summary>Service d'ouverture et d'enregistrement d'une présentation.</summary>
public interface IDocumentStorage : IDocumentPackageStorage
{
    /// <summary>Lit une présentation depuis un fichier (compatibilité pour les documents sans média).</summary>
    Presentation Open(string fileName) => OpenPackage(fileName).Presentation;

    /// <summary>Écrit une présentation sans média embarqué.</summary>
    void Save(Presentation presentation, string fileName) =>
        SavePackage(new DocumentPackage(presentation), fileName);
}
