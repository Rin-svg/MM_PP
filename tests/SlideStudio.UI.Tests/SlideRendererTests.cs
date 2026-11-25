using System.Drawing;
using SlideStudio.Core;
using SlideStudio.Core.Model;
using SlideStudio.UI.Canvas;
using Xunit;

namespace SlideStudio.UI.Tests;

public sealed class SlideRendererTests
{
    [Fact]
    public void Render_DrawsShapeAndTextAtAnyScale()
    {
        Slide slide = new();
        slide.AddElement(new ShapeElement(
            Guid.NewGuid(),
            ShapeKind.Rectangle,
            new ElementFrame(100, 100, 200, 100),
            ArgbColor.FromRgb(230, 40, 30),
            ArgbColor.Transparent,
            0));
        slide.AddElement(new TextElement(
            new ElementFrame(400, 100, 400, 120),
            "SlideStudio"));

        using SlideRenderer renderer = new();
        using Bitmap bitmap = renderer.Render(slide, new Size(640, 360));

        Assert.Equal(Color.FromArgb(230, 40, 30), bitmap.GetPixel(100, 75));
        bool containsText = false;
        for (int y = 50; y < 120 && !containsText; y++)
        {
            for (int x = 200; x < 450; x++)
            {
                Color pixel = bitmap.GetPixel(x, y);
                if (pixel.R < 80 && pixel.G < 80 && pixel.B < 80)
                {
                    containsText = true;
                    break;
                }
            }
        }
        Assert.True(containsText);
    }

    [Fact]
    public void Render_PreservesAspectRatioAndLeavesLetterboxTransparent()
    {
        Slide slide = new();
        using SlideRenderer renderer = new();

        using Bitmap bitmap = renderer.Render(slide, new Size(400, 400));

        Assert.Equal(0, bitmap.GetPixel(200, 50).A);
        Assert.Equal(Color.White.ToArgb(), bitmap.GetPixel(200, 200).ToArgb());
    }

    [Fact]
    public void HitTest_UsesTopmostElementAndAccountsForRotation()
    {
        Slide slide = new();
        ShapeElement lower = new(
            Guid.NewGuid(),
            ShapeKind.Rectangle,
            new ElementFrame(100, 100, 100, 50),
            ArgbColor.FromRgb(0, 0, 255),
            ArgbColor.Black,
            1);
        ShapeElement upper = new(
            Guid.NewGuid(),
            ShapeKind.Rectangle,
            new ElementFrame(100, 100, 100, 50, 90),
            ArgbColor.FromRgb(255, 0, 0),
            ArgbColor.Black,
            1);
        slide.AddElement(lower);
        slide.AddElement(upper);

        Assert.Same(upper, SlideRenderer.HitTest(slide, new PointF(150, 165)));
        Assert.Same(lower, SlideRenderer.HitTest(slide, new PointF(190, 110)));
        Assert.Null(SlideRenderer.HitTest(slide, new PointF(50, 50)));
    }

    [Fact]
    public void Render_DrawsEmbeddedImage()
    {
        using Bitmap source = new(2, 2);
        source.SetPixel(0, 0, Color.Lime);
        source.SetPixel(1, 0, Color.Lime);
        source.SetPixel(0, 1, Color.Lime);
        source.SetPixel(1, 1, Color.Lime);
        using MemoryStream stream = new();
        source.Save(stream, System.Drawing.Imaging.ImageFormat.Png);

        MediaStore media = new();
        media.Add("sample.png", stream.ToArray());
        Slide slide = new();
        slide.AddElement(new ImageElement(new ElementFrame(200, 100, 400, 200), "sample.png"));
        using SlideRenderer renderer = new();
        renderer.SetMediaStore(media);

        using Bitmap rendered = renderer.Render(slide, new Size(640, 360));

        Assert.Equal(Color.Lime.ToArgb(), rendered.GetPixel(200, 100).ToArgb());
    }
}
