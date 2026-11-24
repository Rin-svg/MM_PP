using System;
using System.Collections.Generic;
using System.Text;

namespace SlideStudio.Core.Models;

public class Background
{
    public string Type { get; set; } = "Solid"; // Solid, Image, Gradient
    public string? Color { get; set; } = "#FFFFFF";
    public string? ImageId { get; set; }
}