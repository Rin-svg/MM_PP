using System.Drawing;
using SlideStudio.Core;
using SlideStudio.Core.Model;
using SlideStudio.UI.Export;
using Xunit;

namespace SlideStudio.UI.Tests;

public sealed class SlidePngExporterTests
{
    [Fact]
    public void Export_WritesOneFullResolutionPngPerSlide()
    {
        string directory = CreateTemporaryDirectory();
        try
        {
            Presentation presentation = new("Démo: rapide", [new Slide(), new Slide()]);

            string[] paths = SlidePngExporter.Export(presentation, new MediaStore(), directory);

            Assert.Equal(2, paths.Length);
            Assert.Equal("Démo_ rapide-001.png", Path.GetFileName(paths[0]));
            Assert.Equal("Démo_ rapide-002.png", Path.GetFileName(paths[1]));
            foreach (string path in paths)
            {
                using Bitmap bitmap = new(path);
                Assert.Equal((int)Presentation.SlideWidth, bitmap.Width);
                Assert.Equal((int)Presentation.SlideHeight, bitmap.Height);
                Assert.Equal(Color.White.ToArgb(), bitmap.GetPixel(bitmap.Width / 2, bitmap.Height / 2).ToArgb());
            }
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void Export_RefusesExistingOutputUnlessOverwriteIsExplicit()
    {
        string directory = CreateTemporaryDirectory();
        try
        {
            Presentation presentation = new("Export", [new Slide(), new Slide()]);
            string[] outputPaths = SlidePngExporter.GetOutputPaths(presentation, directory);
            string existingPath = outputPaths[1];
            File.WriteAllText(existingPath, "keep");

            Assert.Throws<IOException>(() => SlidePngExporter.Export(presentation, new MediaStore(), directory));
            Assert.Equal("keep", File.ReadAllText(existingPath));
            Assert.False(File.Exists(outputPaths[0]));

            SlidePngExporter.Export(presentation, new MediaStore(), directory, overwrite: true);
            using Bitmap bitmap = new(existingPath);
            Assert.Equal((int)Presentation.SlideWidth, bitmap.Width);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void Export_MissingMediaLeavesNoPartialOutputForThatSlide()
    {
        string directory = CreateTemporaryDirectory();
        try
        {
            Slide slide = new();
            slide.AddElement(new ImageElement(new ElementFrame(100, 100, 200, 200), "missing.png"));
            Presentation presentation = new("Media", [slide]);

            Assert.Throws<KeyNotFoundException>(
                () => SlidePngExporter.Export(presentation, new MediaStore(), directory));
            Assert.Empty(Directory.GetFiles(directory));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static string CreateTemporaryDirectory()
    {
        string directory = Path.Combine(Path.GetTempPath(), $"SlideStudio-Export-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        return directory;
    }
}
