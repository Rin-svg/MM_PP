using SlideStudio.Core;
using SlideStudio.Core.Model;
using SlideStudio.UI.Export;
using Xunit;

namespace SlideStudio.UI.Tests;

public sealed class SlidePdfExporterTests
{
    [Fact]
    public void Export_WritesPdfWithOnePagePerSlide()
    {
        string directory = CreateTemporaryDirectory();
        try
        {
            Presentation presentation = new("Rapport", [new Slide(), new Slide()]);

            string path = SlidePdfExporter.Export(presentation, new MediaStore(), directory);
            byte[] pdf = File.ReadAllBytes(path);
            string content = System.Text.Encoding.ASCII.GetString(pdf);

            Assert.Equal("Rapport.pdf", Path.GetFileName(path));
            Assert.StartsWith("%PDF-1.4", content);
            Assert.Contains("/Count 2", content);
            Assert.Equal(2, CountOccurrences(content, "/Subtype /Image"));
            Assert.Contains("startxref", content);
            Assert.EndsWith("%%EOF\n", content);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void Export_RefusesExistingFileUnlessOverwriteIsExplicit()
    {
        string directory = CreateTemporaryDirectory();
        try
        {
            Presentation presentation = new("Rapport", [new Slide()]);
            string path = SlidePdfExporter.GetOutputPath(presentation, directory);
            File.WriteAllBytes(path, [1, 2, 3]);

            Assert.Throws<IOException>(
                () => SlidePdfExporter.Export(presentation, new MediaStore(), directory));
            Assert.Equal([1, 2, 3], File.ReadAllBytes(path));

            SlidePdfExporter.Export(presentation, new MediaStore(), directory, overwrite: true);
            Assert.StartsWith("%PDF-1.4", File.ReadAllText(path));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static int CountOccurrences(string value, string needle)
    {
        int count = 0;
        int start = 0;
        while ((start = value.IndexOf(needle, start, StringComparison.Ordinal)) >= 0)
        {
            count++;
            start += needle.Length;
        }
        return count;
    }

    private static string CreateTemporaryDirectory()
    {
        string directory = Path.Combine(Path.GetTempPath(), $"SlideStudio-Pdf-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        return directory;
    }
}
