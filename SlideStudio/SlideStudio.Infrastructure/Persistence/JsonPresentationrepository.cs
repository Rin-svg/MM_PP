using System.Text.Json;
using System.Text.Json.Serialization;
using SlideStudio.Core.Abstractions;
using SlideStudio.Core.Models;
using SlideStudio.Infrastructure.FileFormat;

namespace SlideStudio.Infrastructure.Persistence;

// Implémentation de IPresentationRepository basée sur le format JSON .slds.
public sealed class JsonPresentationRepository : IPresentationRepository
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNameCaseInsensitive = true
    };

    private readonly SafeFileWriter _fileWriter;

    public JsonPresentationRepository(SafeFileWriter fileWriter)
    {
        _fileWriter = fileWriter ?? throw new ArgumentNullException(nameof(fileWriter));
    }

    public Task<Core.Abstractions.Presentation> LoadAsync(string filePath, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task SaveAsync(Core.Abstractions.Presentation presentation, string filePath, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}

public async Task SaveAsync(
        Presentation presentation,
        string filePath,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(presentation);
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        // Pour l'instant les images sont gérées via le dictionnaire Media dans le DTO.
        // Une évolution future pourra extraire automatiquement les images depuis les éléments.
        var media = new Dictionary<string, MediaItemDto>();

        var document = SldsMapper.ToDto(presentation, media);
        var json = JsonSerializer.Serialize(document, SerializerOptions);

        await _fileWriter.WriteAtomicAsync(filePath, json, cancellationToken)
                         .ConfigureAwait(false);
    }

    public async Task<Presentation> LoadAsync(
        string filePath,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        if (!File.Exists(filePath))
            throw new FileNotFoundException("Le fichier de présentation est introuvable.", filePath);

        var json = await File.ReadAllTextAsync(filePath, cancellationToken)
                             .ConfigureAwait(false);

        SldsDocumentDto? document;
        try
        {
            document = JsonSerializer.Deserialize<SldsDocumentDto>(json, SerializerOptions);
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException(
                "Le fichier .slds est corrompu ou n'est pas un JSON valide.", ex);
        }

        if (document is null)
            throw new InvalidDataException("Le fichier .slds est vide ou invalide.");

        // Vérification basique de version
        if (document.FormatVersion != "1.0")
        {
            throw new NotSupportedException(
                $"Version de format non supportée : {document.FormatVersion}. " +
                "Cette version de SlideStudio ne lit que le format 1.0.");
        }

        return SldsMapper.ToDomain(document);
    }
}
