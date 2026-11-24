using System.IO.Compression;
using System.Text;
using System.Text.Json;
using SlideStudio.Core;
using SlideStudio.Core.Model;
using SlideStudio.Infrastructure.FileFormat;

namespace SlideStudio.Infrastructure;

/// <summary>Implémentation sûre du stockage JSON .slds au format archive zip.</summary>
public sealed class SldsDocumentStorage : IDocumentStorage
{
    private const string PresentationEntryName = "presentation.json";

    private static readonly JsonSerializerOptions ReadOptions = new()
    {
        MaxDepth = 32,
        AllowTrailingCommas = false,
        ReadCommentHandling = JsonCommentHandling.Disallow
    };

    private static readonly JsonSerializerOptions WriteOptions = new()
    {
        WriteIndented = true
    };

    /// <inheritdoc />
    public Presentation Open(string fileName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);

        try
        {
            using ZipArchive archive = ZipFile.OpenRead(fileName);
            if (archive.Entries.Count > Limits.MaxSlides + Limits.MaxElementsPerSlide)
            {
                throw new InvalidDataException("Le fichier contient trop d'entrées.");
            }

            if (archive.Entries.Count(entry => entry.FullName == PresentationEntryName) != 1)
            {
                throw new InvalidDataException("Le fichier doit contenir exactement une entrée presentation.json.");
            }

            ZipArchiveEntry? entry = archive.GetEntry(PresentationEntryName);
            if (entry is null)
            {
                throw new InvalidDataException("Le fichier ne contient pas presentation.json.");
            }

            if (entry.Length > 5_000_000)
            {
                throw new InvalidDataException("Les données de présentation sont trop volumineuses.");
            }

            using Stream stream = entry.Open();
            using StreamReader reader = new(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
            string json = reader.ReadToEnd();
            SldsDocumentDto? document = JsonSerializer.Deserialize<SldsDocumentDto>(json, ReadOptions)
                ?? throw new InvalidDataException("Le fichier ne contient pas de document valide.");

            if (document.App != "SlideStudio"
                || document.FormatVersion != "1.0"
                || document.Presentation is null)
            {
                throw new InvalidDataException("Format ou application .slds non pris en charge.");
            }

            return ConvertToPresentation(document.Presentation);
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException("Le JSON presentation.json est invalide.", exception);
        }
        catch (IOException exception)
        {
            throw new IOException("Impossible d'ouvrir le fichier .slds.", exception);
        }
        catch (UnauthorizedAccessException exception)
        {
            throw new IOException("Le fichier .slds est inaccessible.", exception);
        }
    }

    /// <inheritdoc />
    public void Save(Presentation presentation, string fileName)
    {
        ArgumentNullException.ThrowIfNull(presentation);
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);

        string fullPath = Path.GetFullPath(fileName);
        string directory = Path.GetDirectoryName(fullPath)
            ?? throw new IOException("Le dossier de destination est invalide.");

        Directory.CreateDirectory(directory);
        string temporaryPath = Path.Combine(directory, "." + Guid.NewGuid().ToString("N") + ".tmp");

        try
        {
            SldsDocumentDto document = ConvertToDto(presentation);
            using (ZipArchive archive = ZipFile.Open(temporaryPath, ZipArchiveMode.Create))
            {
                ZipArchiveEntry entry = archive.CreateEntry(PresentationEntryName, CompressionLevel.SmallestSize);
                using Stream stream = entry.Open();
                JsonSerializer.Serialize(stream, document, WriteOptions);
            }

            if (File.Exists(fullPath))
            {
                File.Copy(fullPath, fullPath + ".bak", overwrite: true);
            }

            File.Move(temporaryPath, fullPath, overwrite: true);
        }
        catch (IOException exception)
        {
            throw new IOException("Impossible d'enregistrer le fichier .slds.", exception);
        }
        catch (UnauthorizedAccessException exception)
        {
            throw new IOException("Le fichier .slds est verrouillé ou inaccessible.", exception);
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }

    private static Presentation ConvertToPresentation(PresentationDto dto)
    {
        if (dto.SlideWidth != Presentation.SlideWidth
            || dto.SlideHeight != Presentation.SlideHeight)
        {
            throw new InvalidDataException("Les dimensions de la diapositive ne sont pas prises en charge.");
        }

        if (dto.Slides.Count is < 1 or > Limits.MaxSlides)
        {
            throw new InvalidDataException("Le nombre de diapositives est invalide.");
        }

        List<Slide> slides = new(dto.Slides.Count);
        foreach (SlideDto slideDto in dto.Slides)
        {
            if (!Guid.TryParseExact(slideDto.Id, "D", out Guid slideId))
            {
                throw new InvalidDataException("Identifiant de diapositive invalide.");
            }

            if (!ArgbColor.TryParseHex(slideDto.Background, out ArgbColor background))
            {
                throw new InvalidDataException("Couleur de fond invalide.");
            }

            if (slideDto.Elements.Count > Limits.MaxElementsPerSlide)
            {
                throw new InvalidDataException("Une diapositive contient trop d'éléments.");
            }

            List<SlideElement> elements = new(slideDto.Elements.Count);
            foreach (ElementDto elementDto in slideDto.Elements)
            {
                elements.Add(ConvertToElement(elementDto));
            }

            slides.Add(new Slide(slideId, background, elements));
        }

        return new Presentation(dto.Title, slides);
    }

    private static SlideElement ConvertToElement(ElementDto dto)
    {
        if (!Guid.TryParseExact(dto.Id, "D", out Guid id))
        {
            throw new InvalidDataException("Identifiant d'élément invalide.");
        }

        ElementFrame frame = new(dto.Frame.X, dto.Frame.Y, dto.Frame.Width, dto.Frame.Height, dto.Frame.RotationDegrees);
        return dto.Type switch
        {
            "text" => ConvertToText(id, frame, dto),
            "shape" => ConvertToShape(id, frame, dto),
            "image" => ConvertToImage(id, frame, dto),
            _ => throw new InvalidDataException("Type d'élément inconnu.")
        };
    }

    private static TextElement ConvertToText(Guid id, ElementFrame frame, ElementDto dto)
    {
        if (dto.FontFamily is null
            || dto.FontSizePoints is null
            || dto.TextColor is null
            || dto.Alignment is null
            || dto.Text is null
            || !ArgbColor.TryParseHex(dto.TextColor, out ArgbColor color)
            || !Enum.TryParse<TextAlignment>(dto.Alignment, ignoreCase: false, out TextAlignment alignment)
            || !Enum.IsDefined(alignment))
        {
            throw new InvalidDataException("Élément texte invalide.");
        }

        return new TextElement(
            id,
            frame,
            dto.Text,
            dto.FontFamily,
            dto.FontSizePoints.Value,
            dto.IsBold ?? false,
            dto.IsItalic ?? false,
            color,
            alignment);
    }

    private static ShapeElement ConvertToShape(Guid id, ElementFrame frame, ElementDto dto)
    {
        if (dto.Fill is null
            || dto.Stroke is null
            || dto.StrokeWidth is null
            || dto.ShapeKind is null
            || !ArgbColor.TryParseHex(dto.Fill, out ArgbColor fill)
            || !ArgbColor.TryParseHex(dto.Stroke, out ArgbColor stroke)
            || !Enum.TryParse<ShapeKind>(dto.ShapeKind, ignoreCase: false, out ShapeKind kind)
            || !Enum.IsDefined(kind))
        {
            throw new InvalidDataException("Élément forme invalide.");
        }

        return new ShapeElement(id, kind, frame, fill, stroke, dto.StrokeWidth.Value);
    }

    private static ImageElement ConvertToImage(Guid id, ElementFrame frame, ElementDto dto)
    {
        if (dto.MediaName is null)
        {
            throw new InvalidDataException("Élément image invalide.");
        }

        return new ImageElement(id, frame, dto.MediaName, dto.AltText ?? string.Empty);
    }

    private static SldsDocumentDto ConvertToDto(Presentation presentation)
    {
        PresentationDto presentationDto = new()
        {
            Title = presentation.Title,
            SlideWidth = Presentation.SlideWidth,
            SlideHeight = Presentation.SlideHeight,
            Slides = presentation.Slides.Select(ConvertSlideToDto).ToList()
        };

        return new SldsDocumentDto
        {
            Presentation = presentationDto
        };
    }

    private static SlideDto ConvertSlideToDto(Slide slide) =>
        new()
        {
            Id = slide.Id.ToString("D"),
            Background = slide.Background.ToHex(),
            Elements = slide.Elements.Select(ConvertElementToDto).ToList()
        };

    private static ElementDto ConvertElementToDto(SlideElement element) =>
        element switch
        {
            TextElement text => new ElementDto
            {
                Type = "text",
                Id = text.Id.ToString("D"),
                Frame = ConvertFrame(text.Frame),
                Text = text.Text,
                FontFamily = text.FontFamily,
                FontSizePoints = text.FontSizePoints,
                IsBold = text.IsBold,
                IsItalic = text.IsItalic,
                TextColor = text.Color.ToHex(),
                Alignment = text.Alignment.ToString()
            },
            ShapeElement shape => new ElementDto
            {
                Type = "shape",
                Id = shape.Id.ToString("D"),
                Frame = ConvertFrame(shape.Frame),
                ShapeKind = shape.Kind.ToString(),
                Fill = shape.Fill.ToHex(),
                Stroke = shape.Stroke.ToHex(),
                StrokeWidth = shape.StrokeWidth
            },
            ImageElement image => new ElementDto
            {
                Type = "image",
                Id = image.Id.ToString("D"),
                Frame = ConvertFrame(image.Frame),
                MediaName = image.MediaName,
                AltText = image.AltText
            },
            _ => throw new InvalidOperationException("Type d'élément non pris en charge.")
        };

    private static FrameDto ConvertFrame(ElementFrame frame) =>
        new()
        {
            X = frame.X,
            Y = frame.Y,
            Width = frame.Width,
            Height = frame.Height,
            RotationDegrees = frame.RotationDegrees
        };
}
