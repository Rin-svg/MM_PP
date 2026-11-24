using SlideStudio.Core.Abstractions;
using System;
using System.Collections.Generic;
using System.Text;

namespace SlideStudio.Infrastructure.FileFormat;

/// <summary>
/// Convertit entre les modèles métier (Core) et les DTOs de sérialisation JSON.
/// </summary>
public static class SldsMapper
{
    private static Func<object, int, object> ToSlideDto;

    public static SldsDocumentDto ToDto(Presentation presentation, Dictionary<string, MediaItemDto> media)
    {
        return new SldsDocumentDto
        {
            FormatVersion = "1.0",
            App = "SlideStudio",
            Presentation = new PresentationDto
            {
                Title = presentation.Title,
                SlideWidth = presentation.SlideWidth,
                SlideHeight = presentation.SlideHeight,
                Slides = presentation.Slides.Select(ToSlideDto).ToList()
            },
            Media = media
        };
    }
}