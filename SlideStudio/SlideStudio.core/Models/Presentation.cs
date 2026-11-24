using System;
using System.Collections.Generic;
using System.Text;

namespace SlideStudio.Core.Models;

public class Presentation
{
    public string Title { get; set; } = string.Empty;
    public int SlideWidth { get; set; } = 1920;
    public int SlideHeight { get; set; } = 1080;
    public List<Slide> Slides { get; set; } = new();
}