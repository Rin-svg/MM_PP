using System;
using System.Collections.Generic;
using System.Text;

namespace SlideStudio.Core.Models;

public class ImageElement : SlideElement
{
    public ImageElement()
    {
        Type = "Image";
    }

    public string ImageId { get; set; } = string.Empty;
}
