using System;
using System.Collections.Generic;
using System.Text;

namespace SlideStudio.Infrastructure.Images;

/// <summary>
/// Utilitaires pour convertir des images vers/depuis Base64.
/// </summary>
public static class ImageHelper
{
    /// <summary>
    /// Charge un fichier image et le retourne en Base64.
    /// </summary>
    public static async Task<(string Base64, string MimeType, string FileName)> LoadAsBase64Async(
        string imagePath,
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(imagePath))
            throw new FileNotFoundException("Image introuvable.", imagePath);

        var bytes = await File.ReadAllBytesAsync(imagePath, cancellationToken)
                              .ConfigureAwait(false);

        var base64 = Convert.ToBase64String(bytes);
        var extension = Path.GetExtension(imagePath).ToLowerInvariant();
        var mimeType = extension switch
        {
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".gif" => "image/gif",
            ".bmp" => "image/bmp",
            ".webp" => "image/webp",
            _ => "application/octet-stream"
        };

        var fileName = Path.GetFileName(imagePath);
        return (base64, mimeType, fileName);
    }

    /// <summary>
    /// Décode une chaîne Base64 et l'enregistre sur le disque.
    /// </summary>
    public static async Task SaveFromBase64Async(
        string base64Data,
        string outputPath,
        CancellationToken cancellationToken = default)
    {
        var bytes = Convert.FromBase64String(base64Data);
        await File.WriteAllBytesAsync(outputPath, bytes, cancellationToken)
                  .ConfigureAwait(false);
    }
}