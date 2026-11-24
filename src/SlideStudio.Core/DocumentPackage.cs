using SlideStudio.Core.Model;

namespace SlideStudio.Core;

/// <summary>Présentation et médias embarqués associés.</summary>
public sealed class DocumentPackage
{
    /// <summary>Crée un paquet de document en vérifiant que chaque image référencée est présente.</summary>
    /// <param name="presentation">Présentation.</param>
    /// <param name="media">Médias embarqués, ou magasin vide.</param>
    public DocumentPackage(Presentation presentation, MediaStore? media = null)
    {
        ArgumentNullException.ThrowIfNull(presentation);
        Presentation = presentation;
        Media = media?.Clone() ?? new MediaStore();
        Validate();
    }

    /// <summary>Présentation du paquet.</summary>
    public Presentation Presentation { get; }

    /// <summary>Magasin des médias embarqués.</summary>
    public MediaStore Media { get; }

    /// <summary>Vérifie que tous les médias référencés par les diapositives sont présents.</summary>
    public void Validate()
    {
        foreach (ImageElement image in Presentation.Slides.SelectMany(slide => slide.Elements).OfType<ImageElement>())
        {
            if (!Media.Contains(image.MediaName))
            {
                throw new DocumentValidationException(
                    $"Le média « {image.MediaName} » référencé par la présentation est absent.",
                    nameof(Media));
            }
        }
    }
}
