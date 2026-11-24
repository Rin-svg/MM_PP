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

    /// <summary>Ouvre un fichier et renvoie la présentation, sans exposer son magasin média.</summary>
    public Presentation Open(string fileName) => OpenPackage(fileName).Presentation;

    /// <inheritdoc />
    public DocumentPackage OpenPackage(string fileName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);

        try
        {
            if (new FileInfo(fileName).Length > Limits.MaxDocumentBytes)
            {
                throw new InvalidDataException("Le fichier .slds dépasse la taille maximale autorisée.");
            }

            using ZipArchive archive = ZipFile.OpenRead(fileName);
            if (archive.Entries.Count > Limits.MaxMediaCount + 1)
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

            byte[] jsonBytes;
            using (Stream stream = entry.Open())
            {
                jsonBytes = ReadBounded(stream, 5_000_000);
            }

            string json = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true)
                .GetString(jsonBytes);
            if (json.Length > 0 && json[0] == '\uFEFF')
            {
                json = json[1..];
            }

            SldsDocumentDto? document = JsonSerializer.Deserialize<SldsDocumentDto>(json, ReadOptions)
                ?? throw new InvalidDataException("Le fichier ne contient pas de document valide.");

            if (document.App != "SlideStudio"
                || document.FormatVersion != "1.0"
                || document.Presentation is null)
            {
                throw new InvalidDataException("Format ou application .slds non pris en charge.");
            }

            Presentation presentation = ConvertToPresentation(document.Presentation);
            MediaStore media = new();
            HashSet<string> mediaNames = new(StringComparer.Ordinal);
            long totalMediaBytes = 0;
            foreach (ZipArchiveEntry mediaEntry in archive.Entries)
            {
                if (mediaEntry.FullName == PresentationEntryName)
                {
                    continue;
                }

                if (!mediaEntry.FullName.StartsWith("media/", StringComparison.Ordinal))
                {
                    throw new InvalidDataException("Le fichier contient une entrée non autorisée.");
                }

                string mediaName = mediaEntry.FullName["media/".Length..];
                if (mediaName.Length == 0
                    || mediaName.Contains('/')
                    || mediaName.Contains('\\')
                    || !mediaNames.Add(mediaName))
                {
                    throw new InvalidDataException("Le chemin d'un média est invalide ou dupliqué.");
                }

                if (mediaEntry.Length is 0 or > Limits.MaxMediaBytes)
                {
                    throw new InvalidDataException("La taille d'un média est invalide.");
                }

                totalMediaBytes += mediaEntry.Length;
                if (totalMediaBytes > Limits.MaxTotalMediaBytes)
                {
                    throw new InvalidDataException("La taille cumulée des médias est trop importante.");
                }

                using Stream mediaStream = mediaEntry.Open();
                byte[] mediaBytes = ReadBounded(mediaStream, Limits.MaxMediaBytes);
                media.Add(mediaName, mediaBytes);
            }

            return new DocumentPackage(presentation, media);
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

    /// <summary>Enregistre une présentation qui ne référence aucun média.</summary>
    public void Save(Presentation presentation, string fileName) =>
        SavePackage(new DocumentPackage(presentation), fileName);

    /// <inheritdoc />
    public void SavePackage(DocumentPackage package, string fileName)
    {
        ArgumentNullException.ThrowIfNull(package);
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        package.Validate();

        string fullPath = Path.GetFullPath(fileName);
        string directory = Path.GetDirectoryName(fullPath)
            ?? throw new IOException("Le dossier de destination est invalide.");

        string? temporaryPath = null;

        try
        {
            Directory.CreateDirectory(directory);
            temporaryPath = Path.Combine(directory, "." + Guid.NewGuid().ToString("N") + ".tmp");

            SldsDocumentDto document = ConvertToDto(package.Presentation);
            using (ZipArchive archive = ZipFile.Open(temporaryPath, ZipArchiveMode.Create))
            {
                ZipArchiveEntry entry = archive.CreateEntry(PresentationEntryName, CompressionLevel.SmallestSize);
                using (Stream entryStream = entry.Open())
                using (BoundedWriteStream stream = new(entryStream, 5_000_000))
                {
                    JsonSerializer.Serialize(stream, document, WriteOptions);
                }

                foreach (string mediaName in package.Media.Names)
                {
                    ZipArchiveEntry mediaEntry = archive.CreateEntry(
                        "media/" + mediaName,
                        CompressionLevel.SmallestSize);
                    using Stream mediaStream = mediaEntry.Open();
                    byte[] mediaBytes = package.Media.GetBytes(mediaName);
                    mediaStream.Write(mediaBytes);
                }
            }

            if (File.Exists(fullPath))
            {
                File.Copy(fullPath, fullPath + ".bak", overwrite: true);
            }

            File.Move(temporaryPath, fullPath, overwrite: true);
        }
        catch (IOException exception)
        {
            throw new IOException(
                "Impossible d'enregistrer le fichier .slds. Vérifiez qu'il n'est pas verrouillé et que l'espace disque est suffisant.",
                exception);
        }
        catch (UnauthorizedAccessException exception)
        {
            throw new IOException(
                "Le fichier .slds est verrouillé ou inaccessible. Vérifiez les droits d'accès.",
                exception);
        }
        finally
        {
            if (temporaryPath is not null && File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }

    private static byte[] ReadBounded(Stream stream, int maximumBytes)
    {
        using MemoryStream buffer = new();
        byte[] chunk = new byte[81920];
        int read;
        while ((read = stream.Read(chunk, 0, chunk.Length)) > 0)
        {
            if (buffer.Length + read > maximumBytes)
            {
                throw new InvalidDataException("Le contenu décompressé dépasse la taille maximale autorisée.");
            }

            buffer.Write(chunk, 0, read);
        }

        return buffer.ToArray();
    }

    private sealed class BoundedWriteStream(Stream inner, int maximumBytes) : Stream
    {
        private int _written;

        public override bool CanRead => false;
        public override bool CanSeek => false;
        public override bool CanWrite => true;
        public override long Length => _written;

        public override long Position
        {
            get => _written;
            set => throw new NotSupportedException();
        }

        public override void Flush() => inner.Flush();

        public override int Read(byte[] buffer, int offset, int count) =>
            throw new NotSupportedException();

        public override long Seek(long offset, SeekOrigin origin) =>
            throw new NotSupportedException();

        public override void SetLength(long value) =>
            throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count)
        {
            ArgumentNullException.ThrowIfNull(buffer);
            ArgumentOutOfRangeException.ThrowIfNegative(offset);
            ArgumentOutOfRangeException.ThrowIfNegative(count);
            if (offset > buffer.Length - count)
            {
                throw new ArgumentException("La plage d'écriture dépasse le tampon.", nameof(count));
            }

            EnsureCapacity(count);
            inner.Write(buffer, offset, count);
            _written += count;
        }

        public override void Write(ReadOnlySpan<byte> buffer)
        {
            EnsureCapacity(buffer.Length);
            inner.Write(buffer);
            _written += buffer.Length;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                inner.Flush();
            }

            base.Dispose(disposing);
        }

        private void EnsureCapacity(int bytesToWrite)
        {
            if (_written > maximumBytes - bytesToWrite)
            {
                throw new InvalidDataException("Les données de présentation dépassent la taille maximale autorisée.");
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

        if (dto.Slides is null || dto.Slides.Count is < 1 or > Limits.MaxSlides)
        {
            throw new InvalidDataException("Le nombre de diapositives est invalide.");
        }

        List<Slide> slides = new(dto.Slides.Count);
        foreach (SlideDto slideDto in dto.Slides)
        {
            if (slideDto is null || slideDto.Elements is null)
            {
                throw new InvalidDataException("Une diapositive est invalide.");
            }

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
        if (dto is null || dto.Frame is null)
        {
            throw new InvalidDataException("Un élément est invalide.");
        }

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
