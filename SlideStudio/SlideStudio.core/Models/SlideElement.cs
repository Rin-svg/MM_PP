using System;
using System.Collections.Generic;
using System.Text;

namespace SlideStudio.Core.Models;

public abstract class SlideElement
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Type { get; set; } = string.Empty;
    public double X { get; set; }
    public double Y { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
    public int ZIndex { get; set; }
}