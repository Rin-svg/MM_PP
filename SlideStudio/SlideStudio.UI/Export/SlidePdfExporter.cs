using System.Drawing.Imaging;
using System.Text;
using SlideStudio.Core;
using SlideStudio.Core.Model;
using SlideStudio.UI.Canvas;

namespace SlideStudio.UI.Export;

/// <summary>Exporte une présentation sous forme de PDF, une diapositive par page.</summary>
public sealed class SlidePdfExporter
{
    /// <summary>Détermine le chemin du PDF à partir du titre de la présentation.</summary>
    public static string GetOutputPath(Presentation presentation, string directoryPath)
    {
        ArgumentNullException.ThrowIfNull(presentation);
        ArgumentException.ThrowIfNullOrWhiteSpace(directoryPath);
        string directory = Path.GetFullPath(directoryPath);
        return Path.Combine(directory, $"{ToSafeFileName(presentation.Title)}.pdf");
    }

    /// <summary>Exporte la présentation dans un PDF unique.</summary>
    public static string Export(
        Presentation presentation,
        MediaStore mediaStore,
        string directoryPath,
        bool overwrite = false)
    {
        ArgumentNullException.ThrowIfNull(mediaStore);
        if (!Directory.Exists(Path.GetFullPath(directoryPath)))
        {
            throw new DirectoryNotFoundException($"Le dossier de destination n'existe pas : {directoryPath}");
        }

        string outputPath = GetOutputPath(presentation, directoryPath);
        if (!overwrite && File.Exists(outputPath))
        {
            throw new IOException($"Le fichier existe déjà : {outputPath}");
        }

        string directory = Path.GetDirectoryName(outputPath)!;
        string temporaryPath = Path.Combine(directory, $".{Guid.NewGuid():N}.tmp");
        try
        {
            using SlideRenderer renderer = new();
            renderer.SetMediaStore(mediaStore);
            byte[][] images = new byte[presentation.Slides.Count][];
            for (int index = 0; index < presentation.Slides.Count; index++)
            {
                using Bitmap bitmap = renderer.Render(
                    presentation.Slides[index],
                    new Size((int)Presentation.SlideWidth, (int)Presentation.SlideHeight));
                using MemoryStream jpeg = new();
                bitmap.Save(jpeg, ImageFormat.Jpeg);
                images[index] = jpeg.ToArray();
            }

            byte[] pdf = BuildPdf(images);
            File.WriteAllBytes(temporaryPath, pdf);
            File.Move(temporaryPath, outputPath, overwrite);
            return outputPath;
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }

    private static byte[] BuildPdf(byte[][] images)
    {
        int pageCount = images.Length;
        int catalogObject = 1;
        int pagesObject = 2;
        int firstPageObject = 3;
        int firstContentObject = firstPageObject + pageCount;
        int firstImageObject = firstContentObject + pageCount;
        int objectCount = firstImageObject + pageCount - 1;

        using MemoryStream output = new();
        using BinaryWriter writer = new(output, Encoding.ASCII, leaveOpen: true);
        WriteAscii(writer, "%PDF-1.4\n");
        WriteAscii(writer, "%\xE2\xE3\xCF\xD3\n");

        long[] offsets = new long[objectCount + 1];

        offsets[catalogObject] = output.Length;
        WriteAscii(writer, $"{catalogObject} 0 obj\n<< /Type /Catalog /Pages {pagesObject} 0 R >>\nendobj\n");

        offsets[pagesObject] = output.Length;
        StringBuilder kids = new();
        for (int index = 0; index < pageCount; index++)
        {
            kids.Append(firstPageObject + index).Append(" 0 R ");
        }
        WriteAscii(writer, $"{pagesObject} 0 obj\n<< /Type /Pages /Kids [{kids}] /Count {pageCount} >>\nendobj\n");

        for (int index = 0; index < pageCount; index++)
        {
            int pageObject = firstPageObject + index;
            int contentObject = firstContentObject + index;
            int imageObject = firstImageObject + index;
            offsets[pageObject] = output.Length;
            WriteAscii(writer,
                $"{pageObject} 0 obj\n<< /Type /Page /Parent {pagesObject} 0 R "
                + $"/MediaBox [0 0 {(int)Presentation.SlideWidth} {(int)Presentation.SlideHeight}] "
                + $"/Resources << /XObject << /Im{index + 1} {imageObject} 0 R >> >> "
                + $"/Contents {contentObject} 0 R >>\nendobj\n");
        }

        for (int index = 0; index < pageCount; index++)
        {
            int contentObject = firstContentObject + index;
            int imageObject = firstImageObject + index;
            string content = $"q\n{(int)Presentation.SlideWidth} 0 0 {(int)Presentation.SlideHeight} 0 0 cm\n"
                + $"/Im{index + 1} Do\nQ\n";
            byte[] contentBytes = Encoding.ASCII.GetBytes(content);
            offsets[contentObject] = output.Length;
            WriteAscii(writer, $"{contentObject} 0 obj\n<< /Length {contentBytes.Length} >>\nstream\n");
            writer.Write(contentBytes);
            WriteAscii(writer, "\nendstream\nendobj\n");
        }

        for (int index = 0; index < pageCount; index++)
        {
            int imageObject = firstImageObject + index;
            byte[] jpeg = images[index];
            offsets[imageObject] = output.Length;
            WriteAscii(writer,
                $"{imageObject} 0 obj\n<< /Type /XObject /Subtype /Image "
                + $"/Width {(int)Presentation.SlideWidth} /Height {(int)Presentation.SlideHeight} "
                + $"/ColorSpace /DeviceRGB /BitsPerComponent 8 /Filter /DCTDecode /Length {jpeg.Length} >>\nstream\n");
            writer.Write(jpeg);
            WriteAscii(writer, "\nendstream\nendobj\n");
        }

        long xrefOffset = output.Length;
        WriteAscii(writer, $"xref\n0 {objectCount + 1}\n");
        WriteAscii(writer, "0000000000 65535 f \n");
        for (int index = 1; index <= objectCount; index++)
        {
            WriteAscii(writer, $"{offsets[index]:D10} 00000 n \n");
        }
        WriteAscii(writer,
            $"trailer\n<< /Size {objectCount + 1} /Root {catalogObject} 0 R >>\n"
            + $"startxref\n{xrefOffset}\n%%EOF\n");
        writer.Flush();
        return output.ToArray();
    }

    private static void WriteAscii(BinaryWriter writer, string text) =>
        writer.Write(Encoding.ASCII.GetBytes(text));

    private static string ToSafeFileName(string title)
    {
        char[] invalid = [.. Path.GetInvalidFileNameChars(), '<', '>', ':', '"', '/', '\\', '|', '?', '*'];
        HashSet<char> invalidCharacters = new(invalid);
        char[] safeCharacters = title
            .Select(character => char.IsControl(character) || invalidCharacters.Contains(character) ? '_' : character)
            .ToArray();
        string safeTitle = new string(safeCharacters).Trim().TrimEnd('.');
        return string.IsNullOrWhiteSpace(safeTitle) ? "presentation" : safeTitle;
    }
}
