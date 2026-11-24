using System.IO.Compression;
using System.Text;
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
}
