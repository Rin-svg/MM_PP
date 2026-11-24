using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;
namespace SlideStudio.Infrastructure.FileFormat;

/// <summary>
/// Racine du fichier .slds (format JSON).
/// </summary>
/// 
public class MediaItemDto
{
}

public class PresentationDto1
{
}
public sealed class SldsDocumentDto
{
    [JsonPropertyName("formatVersion")]
    public string FormatVersion { get; set; } = "1.0";

    [JsonPropertyName("app")]
    public string App { get; set; } = "SlideStudio";

    [JsonPropertyName("presentation")]
    public PresentationDto1 Presentation { get; set; } = new();

    [JsonPropertyName("media")]
    public Dictionary<string, MediaItemDto> Media { get; set; } = new();
}

public sealed class PresentationDto
{
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("slideWidth")]
    public int SlideWidth { get; set; } = 1920;

    [JsonPropertyName("slideHeight")]
    public int SlideHeight { get; set; } = 1080;

    [JsonPropertyName("slides")]
    public List<SlideDto> Slides { get; set; } = new();
}

public sealed class SlideDto
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("background")]
    public BackgroundDto Background { get; set; } = new();

    [JsonPropertyName("elements")]
    public List<ElementDto> Elements { get; set; } = new();

   
}

public class ElementDto
{
}

public class BackgroundDto
{
}

public sealed class BackgroundDto2
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "Solid";

    [JsonPropertyName("color")]
    public string? Color { get; set; }

    [JsonPropertyName("imageId")]
    public string? ImageId { get; set; }
}

/// <summary>
/// Élément de diapositive. Le champ "type" discrimine Text / Image / etc.
/// </summary>
public sealed class ElementDto
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty; // "Text" | "Image"

    [JsonPropertyName("x")]
    public double X { get; set; }

    [JsonPropertyName("y")]
    public double Y { get; set; }

    [JsonPropertyName("width")]
    public double Width { get; set; }

    [JsonPropertyName("height")]
    public double Height { get; set; }

    [JsonPropertyName("zIndex")]
    public int ZIndex { get; set; }

    // --- Propriétés spécifiques Text ---
    [JsonPropertyName("content")]
    public string? Content { get; set; }

    [JsonPropertyName("style")]
    public TextStyleDto? Style { get; set; }

    // --- Propriétés spécifiques Image ---
    [JsonPropertyName("imageId")]
    public string? ImageId { get; set; }
}