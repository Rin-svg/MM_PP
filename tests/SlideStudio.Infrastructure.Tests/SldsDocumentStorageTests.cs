using System.IO.Compression;
using System.Text;
using SlideStudio.Core;
using SlideStudio.Core.Model;
using Xunit;

namespace SlideStudio.Infrastructure.Tests;

public sealed class SldsDocumentStorageTests
{
    [Fact]
    public void Save_then_open_round_trips_the_document()
    {
        string fileName = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".slds");
        try
        {
            Presentation presentation = new("Présentation test", [new Slide()]);
            TextElement text = new(new ElementFrame(10, 20, 300, 80), "Bonjour");
            presentation.Slides[0].AddElement(text);
            SldsDocumentStorage storage = new();

            storage.Save(presentation, fileName);
            Presentation restored = storage.Open(fileName);

            Assert.Equal("Présentation test", restored.Title);
            Assert.Single(restored.Slides);
            TextElement restoredText = Assert.IsType<TextElement>(Assert.Single(restored.Slides[0].Elements));
            Assert.Equal("Bonjour", restoredText.Text);
            Assert.Equal(new ElementFrame(10, 20, 300, 80), restoredText.Frame);
        }
        finally
        {
            if (File.Exists(fileName))
            {
                File.Delete(fileName);
            }
        }
    }

    [Fact]
    public void Open_rejects_a_zip_without_presentation_json()
    {
        string fileName = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".slds");
        try
        {
            using (ZipArchive archive = ZipFile.Open(fileName, ZipArchiveMode.Create))
            {
                ZipArchiveEntry entry = archive.CreateEntry("other.txt");
                using StreamWriter writer = new(entry.Open(), Encoding.UTF8);
                writer.Write("données étrangères");
            }

            SldsDocumentStorage storage = new();

            InvalidDataException exception = Assert.Throws<InvalidDataException>(() => storage.Open(fileName));
            Assert.Contains("presentation.json", exception.Message);
        }
        finally
        {
            if (File.Exists(fileName))
            {
                File.Delete(fileName);
            }
        }
    }

    [Fact]
    public void Open_rejects_json_too_deep()
    {
        string fileName = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".slds");
        try
        {
            using (ZipArchive archive = ZipFile.Open(fileName, ZipArchiveMode.Create))
            {
                ZipArchiveEntry entry = archive.CreateEntry("presentation.json");
                using StreamWriter writer = new(entry.Open(), Encoding.UTF8);
                string nestedValue = new string('[', 40) + "0" + new string(']', 40);
                writer.Write(
                    "{\"formatVersion\":\"1.0\",\"app\":\"SlideStudio\","
                    + "\"presentation\":{\"title\":\"Test\",\"slideWidth\":1280,\"slideHeight\":720,"
                    + "\"slides\":[{\"id\":\"00000000-0000-0000-0000-000000000001\","
                    + "\"background\":\"#FFFFFF\",\"elements\":[]}]},"
                    + "\"extra\":" + nestedValue + "}");
            }

            SldsDocumentStorage storage = new();

            Assert.Throws<InvalidDataException>(() => storage.Open(fileName));
        }
        finally
        {
            if (File.Exists(fileName))
            {
                File.Delete(fileName);
            }
        }
    }

    [Fact]
    public void Open_rejects_a_compressed_media_entry_over_the_size_limit()
    {
        string fileName = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".slds");
        try
        {
            using (ZipArchive archive = ZipFile.Open(fileName, ZipArchiveMode.Create))
            {
                WriteMinimalPresentation(archive);
                ZipArchiveEntry mediaEntry = archive.CreateEntry("media/oversized.png");
                using Stream stream = mediaEntry.Open();
                stream.Write(new byte[Limits.MaxMediaBytes + 1]);
            }

            Assert.True(new FileInfo(fileName).Length < 1_000_000);
            InvalidDataException exception = Assert.Throws<InvalidDataException>(
                () => new SldsDocumentStorage().OpenPackage(fileName));
            Assert.Contains("taille", exception.Message);
        }
        finally
        {
            if (File.Exists(fileName))
            {
                File.Delete(fileName);
            }
        }
    }

    [Fact]
    public void Open_rejects_a_media_entry_with_a_path_traversal_name()
    {
        string fileName = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".slds");
        try
        {
            using (ZipArchive archive = ZipFile.Open(fileName, ZipArchiveMode.Create))
            {
                WriteMinimalPresentation(archive);
                ZipArchiveEntry entry = archive.CreateEntry("media/../outside.png");
                using Stream stream = entry.Open();
                stream.Write(CreatePngHeader(1, 1));
            }

            InvalidDataException exception = Assert.Throws<InvalidDataException>(
                () => new SldsDocumentStorage().OpenPackage(fileName));
            Assert.Contains("chemin", exception.Message);
        }
        finally
        {
            if (File.Exists(fileName))
            {
                File.Delete(fileName);
            }
        }
    }

    [Fact]
    public void Save_then_open_round_trips_shape_and_image()
    {
        string fileName = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".slds");
        try
        {
            Slide slide = new();
            ShapeElement shape = new(
                Guid.NewGuid(),
                ShapeKind.Ellipse,
                new ElementFrame(20, 30, 200, 100),
                ArgbColor.FromRgb(1, 2, 3),
                ArgbColor.FromRgb(4, 5, 6),
                3);
            ImageElement image = new(
                new ElementFrame(40, 50, 300, 150),
                "photo.png");
            slide.AddElement(shape);
            slide.AddElement(image);
            Presentation presentation = new("Média", [slide]);
            MediaStore media = new();
            media.Add("photo.png", CreatePngHeader(1, 1));
            SldsDocumentStorage storage = new();

            storage.SavePackage(new DocumentPackage(presentation, media), fileName);
            Presentation restored = storage.OpenPackage(fileName).Presentation;

            ShapeElement restoredShape = Assert.IsType<ShapeElement>(restored.Slides[0].Elements[0]);
            Assert.Equal(ShapeKind.Ellipse, restoredShape.Kind);
            Assert.Equal(3, restoredShape.StrokeWidth);
            ImageElement restoredImage = Assert.IsType<ImageElement>(restored.Slides[0].Elements[1]);
            Assert.Equal("photo.png", restoredImage.MediaName);
        }
        finally
        {
            if (File.Exists(fileName))
            {
                File.Delete(fileName);
            }
        }
    }

    [Fact]
    public void Save_creates_a_backup_of_the_previous_file()
    {
        string fileName = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".slds");
        string backupName = fileName + ".bak";
        try
        {
            SldsDocumentStorage storage = new();
            storage.Save(new Presentation("Premier", [new Slide()]), fileName);
            storage.Save(new Presentation("Second", [new Slide()]), fileName);

            Assert.True(File.Exists(backupName));
            Presentation backup = storage.Open(backupName);
            Assert.Equal("Premier", backup.Title);
        }
        finally
        {
            if (File.Exists(fileName))
            {
                File.Delete(fileName);
            }
            if (File.Exists(backupName))
            {
                File.Delete(backupName);
            }
        }
    }

    [Fact]
    public void Open_rejects_duplicate_slide_identifiers()
    {
        string fileName = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".slds");
        try
        {
            Guid id = Guid.NewGuid();
            SldsDocumentStorage storage = new();

            using (ZipArchive archive = ZipFile.Open(fileName, ZipArchiveMode.Create))
            {
                ZipArchiveEntry entry = archive.CreateEntry("presentation.json");
                using StreamWriter writer = new(entry.Open(), Encoding.UTF8);
                string slideId = id.ToString("D");
                writer.Write(
                    "{\"formatVersion\":\"1.0\",\"app\":\"SlideStudio\",\"presentation\":"
                    + "{\"title\":\"Invalide\",\"slideWidth\":1280,\"slideHeight\":720,\"slides\":["
                    + "{\"id\":\"" + slideId + "\",\"background\":\"#FFFFFF\",\"elements\":[]},"
                    + "{\"id\":\"" + slideId + "\",\"background\":\"#FFFFFF\",\"elements\":[]}]}}");
            }

            Assert.Throws<DocumentValidationException>(() => storage.Open(fileName));
        }
        finally
        {
            if (File.Exists(fileName))
            {
                File.Delete(fileName);
            }
        }
    }

    [Fact]
    public void Package_storage_round_trips_embedded_image_bytes()
    {
        string fileName = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".slds");
        try
        {
            const string mediaName = "pixel.png";
            MediaStore media = new();
            byte[] imageBytes = CreatePngHeader(1, 1);
            media.Add(mediaName, imageBytes);

            Slide slide = new();
            slide.AddElement(new ImageElement(new ElementFrame(0, 0, 10, 10), mediaName));
            DocumentPackage package = new(new Presentation("Avec image", [slide]), media);
            SldsDocumentStorage storage = new();

            storage.SavePackage(package, fileName);
            DocumentPackage restored = storage.OpenPackage(fileName);

            Assert.Equal(imageBytes, restored.Media.GetBytes(mediaName));
            Assert.True(restored.Media.Contains(mediaName));
            using ZipArchive archive = ZipFile.OpenRead(fileName);
            Assert.NotNull(archive.GetEntry("media/pixel.png"));
        }
        finally
        {
            if (File.Exists(fileName))
            {
                File.Delete(fileName);
            }
        }
    }

    [Fact]
    public void Media_store_rejects_images_with_excessive_dimensions()
    {
        MediaStore media = new();
        byte[] imageBytes = CreatePngHeader(Limits.MaxImageDimension + 1, 1);

        DocumentValidationException exception = Assert.Throws<DocumentValidationException>(
            () => media.Add("wide.png", imageBytes));

        Assert.Contains("dimensions", exception.Message);
        Assert.Empty(media.Names);
    }

    [Fact]
    public void Document_package_rejects_images_without_embedded_media()
    {
        Slide slide = new();
        slide.AddElement(new ImageElement(new ElementFrame(0, 0, 10, 10), "missing.png"));

        Assert.Throws<DocumentValidationException>(
            () => new DocumentPackage(new Presentation("Sans média", [slide])));
    }

    [Fact]
    public void Save_rejects_oversized_json_without_replacing_existing_document()
    {
        string fileName = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".slds");
        try
        {
            SldsDocumentStorage storage = new();
            storage.Save(new Presentation("À conserver", [new Slide()]), fileName);

            Slide largeSlide = new();
            string largeText = new('x', Limits.MaxTextLength);
            for (int index = 0; index < 101; index++)
            {
                largeSlide.AddElement(new TextElement(
                    new ElementFrame(index, index, 100, 30),
                    largeText));
            }

            Assert.Throws<InvalidDataException>(
                () => storage.Save(new Presentation("Trop volumineuse", [largeSlide]), fileName));
            Assert.Equal("À conserver", storage.Open(fileName).Title);
        }
        finally
        {
            if (File.Exists(fileName))
            {
                File.Delete(fileName);
            }

            if (File.Exists(fileName + ".bak"))
            {
                File.Delete(fileName + ".bak");
            }
        }
    }

    private static byte[] CreatePngHeader(int width, int height)
    {
        byte[] bytes = new byte[24];
        byte[] signature = [137, 80, 78, 71, 13, 10, 26, 10];
        signature.CopyTo(bytes, 0);
        bytes[11] = 13;
        "IHDR"u8.CopyTo(bytes.AsSpan(12, 4));
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32BigEndian(bytes.AsSpan(16, 4), (uint)width);
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32BigEndian(bytes.AsSpan(20, 4), (uint)height);
        return bytes;
    }

    private static void WriteMinimalPresentation(ZipArchive archive)
    {
        ZipArchiveEntry entry = archive.CreateEntry("presentation.json");
        using StreamWriter writer = new(entry.Open(), Encoding.UTF8);
        writer.Write(
            "{\"formatVersion\":\"1.0\",\"app\":\"SlideStudio\","
            + "\"presentation\":{\"title\":\"Test\",\"slideWidth\":1280,\"slideHeight\":720,"
            + "\"slides\":[{\"id\":\"00000000-0000-0000-0000-000000000001\","
            + "\"background\":\"#FFFFFF\",\"elements\":[]}]}}");
    }
}
