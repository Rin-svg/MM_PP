using SlideStudio.Core.Abstractions;
using SlideStudio.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace SlideStudio.Infrastructure.FileFormat;

// Convertit entre les modèles metier (Core) et les DTOs de sérialisation JSON.


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
    private static Slide ToSlideDomain(SlideDto dto)
    {
        return new Slide
        {
            Id = dto.Id,
            Background = new Background
            {
                Type = dto.Background.Type,
                Color = dto.Background.Color,
                ImageId = dto.Background.ImageId
            },
            Elements = dto.Elements.Select(ToElementDomain).Where(e => e is not null).Cast<SlideElement>().ToList()
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

    private static Slide ToSlideDomain(SlideDto dto)
    {
        return new Slide
        {
            Id = dto.Id,
            Background = new Background
            {
                Type = dto.Background.Type,
                Color = dto.Background.Color,
                ImageId = dto.Background.ImageId
            },
            Elements = dto.Elements.Select(ToElementDomain).Where(e => e is not null).Cast<SlideElement>().ToList()
        };
    }

    private static ElementDto ToElementDto(SlideElement element)
    {
        var dto = new ElementDto
        {
            Id = element.Id,
            Type = element.Type,
            X = element.X,
            Y = element.Y,
            Width = element.Width,
            Height = element.Height,
            ZIndex = element.ZIndex
        };

        switch (element)
        {
            case TextElement text:
                dto.Content = text.Content;
                dto.Style = new TextStyleDto
                {
                    FontFamily = text.Style.FontFamily,
                    FontSize = text.Style.FontSize,
                    Bold = text.Style.Bold,
                    Italic = text.Style.Italic,
                    Color = text.Style.Color,
                    Alignment = text.Style.Alignment
                };
                break;

            case ImageElement image:
                dto.ImageId = image.ImageId;
                break;
        }

        return dto;
    }

    private static SlideElement? ToElementDomain(ElementDto dto)
    {
        return dto.Type switch
        {
            "Text" => new TextElement
            {
                Id = dto.Id,
                Type = "Text",
                X = dto.X,
                Y = dto.Y,
                Width = dto.Width,
                Height = dto.Height,
                ZIndex = dto.ZIndex,
                Content = dto.Content ?? string.Empty,
                Style = dto.Style is null
                    ? new TextStyle()
                    : new TextStyle
                    {
                        FontFamily = dto.Style.FontFamily,
                        FontSize = dto.Style.FontSize,
                        Bold = dto.Style.Bold,
                        Italic = dto.Style.Italic,
                        Color = dto.Style.Color,
                        Alignment = dto.Style.Alignment
                    }
            },
            "Image" => new ImageElement
            {
                Id = dto.Id,
                Type = "Image",
                X = dto.X,
                Y = dto.Y,
                Width = dto.Width,
                Height = dto.Height,
                ZIndex = dto.ZIndex,
                ImageId = dto.ImageId ?? string.Empty
            },
            _ => null // Type inconnu → ignoré pour robustesse
        };
    }
}