using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using SlideStudio.Core;
using SlideStudio.Core.Model;

namespace SlideStudio.UI.Canvas;

/// <summary>Rendu réutilisable des diapositives dans le canevas, les miniatures et les exports.</summary>
public sealed class SlideRenderer : IDisposable
{
    private const long MaxCachedImageBytes = 64L * 1024 * 1024;
    private readonly Dictionary<string, Image> _images = new(StringComparer.Ordinal);
    private MediaStore? _mediaStore;
    private long _cachedImageBytes;

    /// <summary>Associe les médias du document courant au renderer.</summary>
    public void SetMediaStore(MediaStore? mediaStore)
    {
        if (ReferenceEquals(_mediaStore, mediaStore))
        {
            return;
        }

        ClearImages();
        _mediaStore = mediaStore;
    }

    /// <summary>Dessine une diapositive en conservant ses proportions dans la zone donnée.</summary>
    public void Draw(Graphics graphics, Slide slide, RectangleF bounds)
    {
        ArgumentNullException.ThrowIfNull(graphics);
        ArgumentNullException.ThrowIfNull(slide);
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return;
        }

        float scale = Math.Min(
            bounds.Width / (float)Presentation.SlideWidth,
            bounds.Height / (float)Presentation.SlideHeight);
        RectangleF destination = new(
            bounds.Left + (bounds.Width - (float)Presentation.SlideWidth * scale) / 2,
            bounds.Top + (bounds.Height - (float)Presentation.SlideHeight * scale) / 2,
            (float)Presentation.SlideWidth * scale,
            (float)Presentation.SlideHeight * scale);

        GraphicsState state = graphics.Save();
        try
        {
            graphics.SetClip(destination);
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
            graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            using SolidBrush background = new(ToDrawingColor(slide.Background));
            graphics.FillRectangle(background, destination);
            graphics.TranslateTransform(destination.Left, destination.Top);
            graphics.ScaleTransform(scale, scale);

            foreach (SlideElement element in slide.Elements)
            {
                DrawElement(graphics, element);
            }
        }
        finally
        {
            graphics.Restore(state);
        }
    }

    /// <summary>Crée une image de la diapositive à la taille demandée.</summary>
    public Bitmap Render(Slide slide, Size size)
    {
        ArgumentNullException.ThrowIfNull(slide);
        if (size.Width <= 0 || size.Height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(size), "La taille du rendu doit être positive.");
        }

        Bitmap bitmap = new(size.Width, size.Height, PixelFormat.Format32bppPArgb);
        try
        {
            using Graphics graphics = Graphics.FromImage(bitmap);
            graphics.Clear(Color.Transparent);
            Draw(graphics, slide, new RectangleF(PointF.Empty, size));
            return bitmap;
        }
        catch
        {
            bitmap.Dispose();
            throw;
        }
    }

    /// <summary>Renvoie l'élément du premier plan sous le point exprimé en coordonnées de diapositive.</summary>
    public static SlideElement? HitTest(Slide slide, PointF point)
    {
        ArgumentNullException.ThrowIfNull(slide);
        for (int index = slide.Elements.Count - 1; index >= 0; index--)
        {
            SlideElement element = slide.Elements[index];
            if (Contains(element, point))
            {
                return element;
            }
        }

        return null;
    }

    /// <inheritdoc />
    public void Dispose() => ClearImages();

    private void DrawElement(Graphics graphics, SlideElement element)
    {
        ElementFrame frame = element.Frame;
        GraphicsState state = graphics.Save();
        graphics.TranslateTransform((float)(frame.X + frame.Width / 2), (float)(frame.Y + frame.Height / 2));
        graphics.RotateTransform((float)frame.RotationDegrees);
        RectangleF bounds = new(
            (float)(-frame.Width / 2),
            (float)(-frame.Height / 2),
            (float)frame.Width,
            (float)frame.Height);

        switch (element)
        {
            case TextElement text:
                DrawText(graphics, text, bounds);
                break;
            case ShapeElement shape:
                DrawShape(graphics, shape, bounds);
                break;
            case ImageElement image:
                DrawImage(graphics, image, bounds);
                break;
        }

        graphics.Restore(state);
    }

    private static void DrawText(Graphics graphics, TextElement element, RectangleF bounds)
    {
        FontStyle style = (element.IsBold ? FontStyle.Bold : FontStyle.Regular)
            | (element.IsItalic ? FontStyle.Italic : FontStyle.Regular);
        using Font font = new(element.FontFamily, (float)element.FontSizePoints, style, GraphicsUnit.Point);
        using SolidBrush brush = new(ToDrawingColor(element.Color));
        using StringFormat format = new()
        {
            Alignment = element.Alignment switch
            {
                TextAlignment.Center => StringAlignment.Center,
                TextAlignment.Right => StringAlignment.Far,
                _ => StringAlignment.Near
            },
            LineAlignment = StringAlignment.Near,
            Trimming = StringTrimming.Word,
            FormatFlags = StringFormatFlags.LineLimit
        };
        GraphicsState state = graphics.Save();
        graphics.SetClip(bounds);
        graphics.DrawString(element.Text, font, brush, bounds, format);
        graphics.Restore(state);
    }

    private static void DrawShape(Graphics graphics, ShapeElement element, RectangleF bounds)
    {
        using SolidBrush fill = new(ToDrawingColor(element.Fill));
        using Pen stroke = new(ToDrawingColor(element.Stroke), (float)element.StrokeWidth)
        {
            LineJoin = LineJoin.Round
        };

        switch (element.Kind)
        {
            case ShapeKind.Rectangle:
                if (element.Fill.A > 0)
                {
                    graphics.FillRectangle(fill, bounds);
                }
                if (element.StrokeWidth > 0 && element.Stroke.A > 0)
                {
                    graphics.DrawRectangle(stroke, bounds.X, bounds.Y, bounds.Width, bounds.Height);
                }
                break;
            case ShapeKind.Ellipse:
                if (element.Fill.A > 0)
                {
                    graphics.FillEllipse(fill, bounds);
                }
                if (element.StrokeWidth > 0 && element.Stroke.A > 0)
                {
                    graphics.DrawEllipse(stroke, bounds);
                }
                break;
            case ShapeKind.Line:
                if (element.StrokeWidth > 0 && element.Stroke.A > 0)
                {
                    graphics.DrawLine(stroke, bounds.Left, bounds.Top, bounds.Right, bounds.Bottom);
                }
                break;
        }
    }

    private void DrawImage(Graphics graphics, ImageElement element, RectangleF bounds)
    {
        bool disposeAfterDrawing = false;
        if (!_images.TryGetValue(element.MediaName, out Image? image))
        {
            if (_mediaStore is null)
            {
                throw new InvalidOperationException(
                    $"Le média « {element.MediaName} » ne peut pas être rendu sans magasin de médias.");
            }

            byte[] bytes = _mediaStore.GetBytes(element.MediaName);
            using MemoryStream stream = new(bytes, writable: false);
            using Image decoded = Image.FromStream(stream, useEmbeddedColorManagement: false, validateImageData: true);
            image = new Bitmap(decoded);
            long imageBytes = (long)image.Width * image.Height * 4;
            if (imageBytes > MaxCachedImageBytes)
            {
                disposeAfterDrawing = true;
            }
            else
            {
                if (_cachedImageBytes + imageBytes > MaxCachedImageBytes)
                {
                    ClearImages();
                }
                _images.Add(element.MediaName, image);
                _cachedImageBytes += imageBytes;
            }
        }

        try
        {
            graphics.DrawImage(image, bounds);
        }
        finally
        {
            if (disposeAfterDrawing)
            {
                image.Dispose();
            }
        }
    }

    private static bool Contains(SlideElement element, PointF point)
    {
        ElementFrame frame = element.Frame;
        double radians = -frame.RotationDegrees * Math.PI / 180;
        double dx = point.X - (frame.X + frame.Width / 2);
        double dy = point.Y - (frame.Y + frame.Height / 2);
        float localX = (float)(dx * Math.Cos(radians) - dy * Math.Sin(radians));
        float localY = (float)(dx * Math.Sin(radians) + dy * Math.Cos(radians));
        float halfWidth = (float)(frame.Width / 2);
        float halfHeight = (float)(frame.Height / 2);

        return element switch
        {
            ShapeElement { Kind: ShapeKind.Ellipse } =>
                (localX * localX) / (halfWidth * halfWidth)
                    + (localY * localY) / (halfHeight * halfHeight) <= 1,
            ShapeElement { Kind: ShapeKind.Line } =>
                DistanceToSegment(localX, localY, -halfWidth, -halfHeight, halfWidth, halfHeight)
                    <= Math.Max(6, ((ShapeElement)element).StrokeWidth / 2),
            _ => Math.Abs(localX) <= halfWidth && Math.Abs(localY) <= halfHeight
        };
    }

    private static double DistanceToSegment(float x, float y, float x1, float y1, float x2, float y2)
    {
        double dx = x2 - x1;
        double dy = y2 - y1;
        double lengthSquared = dx * dx + dy * dy;
        double projection = lengthSquared == 0
            ? 0
            : Math.Clamp(((x - x1) * dx + (y - y1) * dy) / lengthSquared, 0, 1);
        double closestX = x1 + projection * dx;
        double closestY = y1 + projection * dy;
        double offsetX = x - closestX;
        double offsetY = y - closestY;
        return Math.Sqrt(offsetX * offsetX + offsetY * offsetY);
    }

    private void ClearImages()
    {
        foreach (Image image in _images.Values)
        {
            image.Dispose();
        }
        _images.Clear();
        _cachedImageBytes = 0;
    }

    private static Color ToDrawingColor(ArgbColor color) => Color.FromArgb(color.A, color.R, color.G, color.B);
}
