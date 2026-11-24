using System;
using System.Collections.Generic;
using System.Text;

namespace SlideStudio.Core.Models;

public class TextStyle
{
    public string FontFamily { get; set; } = "Segoe UI";
    public double FontSize { get; set; } = 24;
    public bool Bold { get; set; }
    public bool Italic { get; set; }
    public string Color { get; set; } = "#000000";
    public string Alignment { get; set; } = "Left"; // Left, Center, Right
}