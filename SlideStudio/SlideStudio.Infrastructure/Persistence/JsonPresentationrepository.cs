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