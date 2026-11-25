using System.Drawing.Imaging;
using SlideStudio.Core;
using SlideStudio.Core.Model;
using SlideStudio.UI.Canvas;

namespace SlideStudio.UI.Export;

/// <summary>Exporte les diapositives d'une présentation en fichiers PNG autonomes.</summary>
public sealed class SlidePngExporter
{
    /// <summary>Détermine les chemins des fichiers exportés, dans l'ordre des diapositives.</summary>
    public static string[] GetOutputPaths(Presentation presentation, string directoryPath)
    {
        ArgumentNullException.ThrowIfNull(presentation);
        ArgumentException.ThrowIfNullOrWhiteSpace(directoryPath);
        string directory = Path.GetFullPath(directoryPath);
        string safeTitle = ToSafeFileName(presentation.Title);
        return presentation.Slides
            .Select((_, index) => Path.Combine(directory, $"{safeTitle}-{index + 1:D3}.png"))
            .ToArray();
    }

    /// <summary>Exporte toutes les diapositives en PNG, sans écraser de fichier sans autorisation explicite.</summary>
    /// <param name="presentation">Présentation à exporter.</param>
    /// <param name="mediaStore">Médias embarqués référencés par les diapositives.</param>
    /// <param name="directoryPath">Dossier existant de destination.</param>
    /// <param name="overwrite">Autorise le remplacement des fichiers de sortie existants.</param>
    /// <returns>Chemins des fichiers PNG créés, dans l'ordre des diapositives.</returns>
    public static string[] Export(
        Presentation presentation,
        MediaStore mediaStore,
        string directoryPath,
        bool overwrite = false)
    {
        ArgumentNullException.ThrowIfNull(mediaStore);
        string[] outputPaths = GetOutputPaths(presentation, directoryPath);
        string directory = Path.GetFullPath(directoryPath);
        if (!Directory.Exists(directory))
        {
            throw new DirectoryNotFoundException($"Le dossier de destination n'existe pas : {directory}");
        }

        if (!overwrite)
        {
            string? existingPath = outputPaths.FirstOrDefault(File.Exists);
            if (existingPath is not null)
            {
                throw new IOException($"Le fichier existe déjà : {existingPath}");
            }
        }

        using SlideRenderer renderer = new();
        renderer.SetMediaStore(mediaStore);
        for (int index = 0; index < presentation.Slides.Count; index++)
        {
            string destinationPath = outputPaths[index];
            string temporaryPath = Path.Combine(directory, $".{Guid.NewGuid():N}.tmp");
            try
            {
                using Bitmap bitmap = renderer.Render(
                    presentation.Slides[index],
                    new Size((int)Presentation.SlideWidth, (int)Presentation.SlideHeight));
                using (FileStream stream = new(temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    bitmap.Save(stream, ImageFormat.Png);
                }

                File.Move(temporaryPath, destinationPath, overwrite);
            }
            finally
            {
                if (File.Exists(temporaryPath))
                {
                    File.Delete(temporaryPath);
                }
            }
        }

        return outputPaths;
    }

    private static string ToSafeFileName(string title)
    {
        HashSet<char> invalidCharacters = new(Path.GetInvalidFileNameChars())
        {
            '<', '>', ':', '"', '/', '\\', '|', '?', '*'
        };
        char[] safeCharacters = title
            .Select(character => char.IsControl(character) || invalidCharacters.Contains(character) ? '_' : character)
            .ToArray();
        string safeTitle = new string(safeCharacters).Trim().TrimEnd('.');
        return string.IsNullOrWhiteSpace(safeTitle) ? "presentation" : safeTitle;
    }
}
