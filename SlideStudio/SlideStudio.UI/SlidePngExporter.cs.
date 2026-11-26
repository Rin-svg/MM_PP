
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using SlideStudio.Core;
using SlideStudio.Core.Model;
using SlideStudio.UI.Canvas;

namespace SlideStudio.UI;

/// <summary>
/// Permet d'exporter les diapositives d'une présentation
/// au format PNG.
/// </summary>
public static class SlidePngExporter
{
    /// <summary>
    /// Exporte toutes les diapositives dans un dossier.
    /// Chaque diapositive est enregistrée dans un fichier PNG.
    /// </summary>
    /// <param name="slides">Diapositives à exporter.</param>
    /// <param name="mediaStore">Stockage des médias.</param>
    /// <param name="outputDirectory">Dossier de destination.</param>
    public static void ExportAll(
        IReadOnlyList<Slide> slides,
        MediaStore mediaStore,
        string outputDirectory)
    {
        ArgumentNullException.ThrowIfNull(slides);
        ArgumentNullException.ThrowIfNull(mediaStore);

        if (string.IsNullOrWhiteSpace(outputDirectory))
        {
            throw new ArgumentException(
                "Le dossier de destination est obligatoire.",
                nameof(outputDirectory));
        }

        if (slides.Count == 0)
        {
            throw new ArgumentException(
                "La présentation ne contient aucune diapositive.",
                nameof(slides));
        }

        // Créer le dossier de destination s'il n'existe pas.
        Directory.CreateDirectory(outputDirectory);

        // Initialiser le moteur de rendu.
        using SlideRenderer renderer = new();

        renderer.SetMediaStore(mediaStore);

        // Définir les dimensions de l'image exportée.
        Size imageSize = new(
            (int)Presentation.SlideWidth,
            (int)Presentation.SlideHeight);

        // Parcourir toutes les diapositives.
        for (int index = 0; index < slides.Count; index++)
        {
            // Vérifier que la diapositive existe.
            Slide slide = slides[index];

            ArgumentNullException.ThrowIfNull(slide);

            // Générer l'image de la diapositive.
            using Bitmap image = renderer.Render(
                slide,
                imageSize);

            // Construire le nom du fichier.
            string fileName =
                $"diapositive_{index + 1:D3}.png";

            string filePath = Path.Combine(
                outputDirectory,
                fileName);

            // Enregistrer l'image au format PNG.
            image.Save(filePath, ImageFormat.Png);
        }
    }
}
