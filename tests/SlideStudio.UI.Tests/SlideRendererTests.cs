using System.Drawing;
using SlideStudio.Core;
using SlideStudio.Core.Model;
using SlideStudio.UI.Canvas;
using Xunit;

namespace SlideStudio.UI.Tests;

public sealed class SlideRendererTests
{
    [Fact]
    public void ThumbnailCache_ReusesSlideAndEvictsAtBound()
    {
        using SlideRenderer renderer = new();
        using ThumbnailCache cache = new(renderer);
        Slide slide = new();

        using Bitmap first = new(192, 108);
        using Graphics graphics = Graphics.FromImage(first);
        cache.Draw(graphics, slide, new Rectangle(0, 0, 192, 108));
        Assert.Equal(1, cache.Count);

        cache.Draw(graphics, slide, new Rectangle(0, 0, 192, 108));
        Assert.Equal(1, cache.Count);

        cache.Invalidate(slide);
        Assert.Equal(0, cache.Count);

        for (int index = 0; index < 500; index++)
        {
            Slide fifthHundred = new();
            cache.Draw(graphics, fifthHundred, new Rectangle(0, 0, 192, 108));
        }
        Assert.True(cache.Count < 500);
    }

    [Fact]
    public void Canvas_AccessibleStatusKeepsKeyboardHelpAndCurrentSelection()
    {
        using SlideRenderer renderer = new();
        using SlideCanvas canvas = new(renderer);

        canvas.SetAccessibleStatus("Diapositive 2 sur 4. 1 élément sélectionné : zone de texte « Titre ».");

        Assert.Contains("Diapositive 2 sur 4", canvas.AccessibleDescription);
        Assert.Contains("zone de texte « Titre »", canvas.AccessibleDescription);
        Assert.Contains("Ctrl+A", canvas.AccessibleDescription);
        Assert.Contains("Échap", canvas.AccessibleDescription);
    }

    [Fact]
    public void WelcomeScreen_AnnouncesSelectedActionAndEnterActivatesIt()
    {
        using WelcomeScreen welcomeScreen = new();
        StartupAction? requestedAction = null;
        welcomeScreen.ActionRequested += (_, action) => requestedAction = action;

        Assert.Contains("Créer une présentation", welcomeScreen.AccessibleDescription);
        Assert.True(welcomeScreen.HandleNavigationKey(System.Windows.Forms.Keys.Enter));
        Assert.Equal(StartupAction.NewPresentation, requestedAction);

        requestedAction = null;
        Assert.True(welcomeScreen.HandleNavigationKey(System.Windows.Forms.Keys.Right));
        Assert.Contains("Ouvrir un projet", welcomeScreen.AccessibleDescription);
        Assert.True(welcomeScreen.HandleNavigationKey(System.Windows.Forms.Keys.Enter));

        Assert.Equal(StartupAction.OpenPresentation, requestedAction);
        Assert.True(welcomeScreen.TabStop);
        Assert.Equal(System.Windows.Forms.AccessibleRole.Pane, welcomeScreen.AccessibleRole);
    }

    [Fact]
    public void WelcomeScreen_TreatsArrowKeysAsInputForCarouselNavigation()
    {
        Assert.True(WelcomeScreen.IsCarouselNavigationKey(System.Windows.Forms.Keys.Left));
        Assert.True(WelcomeScreen.IsCarouselNavigationKey(System.Windows.Forms.Keys.Right));
        Assert.True(WelcomeScreen.IsCarouselNavigationKey(
            System.Windows.Forms.Keys.Shift | System.Windows.Forms.Keys.Left));
        Assert.False(WelcomeScreen.IsCarouselNavigationKey(System.Windows.Forms.Keys.Enter));
        Assert.False(WelcomeScreen.IsCarouselNavigationKey(System.Windows.Forms.Keys.Tab));
    }

    [Fact]
    public void LoadingScreen_ExposesProgressStateAndAnnouncesLoading()
    {
        using LoadingScreen loadingScreen = new();

        Assert.True(loadingScreen.TabStop);
        Assert.Equal(System.Windows.Forms.AccessibleRole.ProgressBar, loadingScreen.AccessibleRole);
        Assert.Equal("Préparation de votre espace", loadingScreen.AccessibleName);

        loadingScreen.Start();

        Assert.Contains("Chargement en cours : 0 %", loadingScreen.AccessibleDescription);
    }

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
