using System;
using System.Collections.Generic;
using System.Text;

namespace SlideStudio.Core.Models;

public class Slide
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public Background Background { get; set; } = new();
    public List<SlideElement> Elements { get; set; } = new();
}