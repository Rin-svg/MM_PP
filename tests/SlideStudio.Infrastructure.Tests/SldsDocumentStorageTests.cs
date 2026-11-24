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
                writer.Write("{\"presentation\":{\"slides\":[{\"elements\":[[[[[[[[[[[[[");
            }

            SldsDocumentStorage storage = new();

            Assert.ThrowsAny<Exception>(() => storage.Open(fileName));
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
            SldsDocumentStorage storage = new();

            storage.Save(presentation, fileName);
            Presentation restored = storage.Open(fileName);

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
}
