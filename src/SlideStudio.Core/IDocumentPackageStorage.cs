namespace SlideStudio.Core;

/// <summary>Stockage de présentations accompagnées de leurs médias embarqués.</summary>
public interface IDocumentPackageStorage
{
    /// <summary>Ouvre un document et tous ses médias.</summary>
    DocumentPackage OpenPackage(string fileName);

    /// <summary>Enregistre un document et ses médias.</summary>
    void SavePackage(DocumentPackage package, string fileName);
}
