using System;
using System.Collections.Generic;
using System.Text;

namespace SlideStudio.Core.Models;

public class TextElement : SlideElement
{
    public TextElement()
    {
        Type = "Text";
    }

    public string Content { get; set; } = string.Empty;
    public TextStyle Style { get; set; } = new();
}