using SlideStudio.Core.Abstractions;
using SlideStudio.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace SlideStudio.Infrastructure.FileFormat;

// Convertit entre les modèles métier (Core) et les DTOs de sérialisation JSON.

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

public static Presentation ToDomain(SldsDocumentDto dto)
    {
        return new Presentation
        {
            Title = dto.Presentation.Title,
            SlideWidth = dto.Presentation.SlideWidth,
            SlideHeight = dto.Presentation.SlideHeight,
            Slides = dto.Presentation.Slides.Select(ToSlideDomain).ToList()
        };


    }

    private static SlideDto ToSlideDto(Slide slide)
    {
        return new SlideDto
        {
            Id = slide.Id,
            Background = new BackgroundDto
            {
                Type = slide.Background.Type,
                Color = slide.Background.Color,
                ImageId = slide.Background.ImageId
            },
            Elements = slide.Elements.Select(ToElementDto).ToList()
        };
    }