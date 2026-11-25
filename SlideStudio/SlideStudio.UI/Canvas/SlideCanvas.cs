using System.Drawing.Drawing2D;
using SlideStudio.Core.Model;

namespace SlideStudio.UI.Canvas;

/// <summary>Affiche une diapositive et permet de sélectionner, déplacer et redimensionner ses éléments.</summary>
internal sealed class SlideCanvas : Control
{
    private const float CanvasPadding = 24;
    private const float HandleSize = 9;
    private readonly SlideRenderer _renderer;
    private Slide? _slide;
    private SlideElement? _selectedElement;
    private TextBox? _textEditor;
    private TextElement? _editingText;
    private Font? _textEditorFont;
    private SlideElement? _dragElement;
    private ResizeHandle _dragHandle;
    private PointF _lastLogicalPoint;
    private Guid _gestureId;

    public event EventHandler<CanvasSelectionChangedEventArgs>? SelectionChanged;
    public event EventHandler<CanvasFrameChangedEventArgs>? FrameChangeRequested;
    public event EventHandler<CanvasTextChangedEventArgs>? TextChangeRequested;

    public SlideCanvas(SlideRenderer renderer)
    {
        ArgumentNullException.ThrowIfNull(renderer);
        _renderer = renderer;
        Dock = DockStyle.Fill;
        BackColor = Color.FromArgb(15, 17, 26);
        TabStop = true;
        AccessibleRole = AccessibleRole.Graphic;
        AccessibleName = "Canevas de la diapositive";
        SetStyle(
            ControlStyles.UserPaint
                | ControlStyles.AllPaintingInWmPaint
                | ControlStyles.OptimizedDoubleBuffer
                | ControlStyles.ResizeRedraw,
            true);
    }

    public void SetSlide(Slide? slide)
    {
        if (!ReferenceEquals(_slide, slide))
        {
            CancelTextEdit();
        }
        _slide = slide;
        if (_selectedElement is not null && (slide is null || !slide.Elements.Contains(_selectedElement)))
        {
            SetSelectedElement(null, notify: false);
        }
        Invalidate();
    }

    public void SetSelection(IReadOnlyList<SlideElement> selection)
    {
        ArgumentNullException.ThrowIfNull(selection);
        SlideElement? selected = selection.Count == 1 && _slide?.Elements.Contains(selection[0]) == true
            ? selection[0]
            : null;
        SetSelectedElement(selected, notify: false);
    }

    public void CancelDrag()
    {
        _dragElement = null;
        _dragHandle = ResizeHandle.None;
        _gestureId = Guid.Empty;
        Capture = false;
        Invalidate();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            CancelTextEdit();
        }
        base.Dispose(disposing);
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        e.Graphics.Clear(BackColor);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        if (_slide is null || Width <= CanvasPadding * 2 || Height <= CanvasPadding * 2)
        {
            return;
        }

        RectangleF destination = GetSlideBounds();
        using (SolidBrush shadow = new(Color.FromArgb(80, 0, 0, 0)))
        {
            RectangleF shadowBounds = destination;
            shadowBounds.Offset(0, 7);
            e.Graphics.FillRectangle(shadow, shadowBounds);
        }
        _renderer.Draw(e.Graphics, _slide, destination);
        using Pen border = new(Color.FromArgb(105, 159, 173, 198), 1);
        e.Graphics.DrawRectangle(border, destination.X, destination.Y, destination.Width, destination.Height);
        DrawSelection(e.Graphics, destination);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button != MouseButtons.Left || _slide is null || !TryScreenToSlide(e.Location, out PointF point))
        {
            return;
        }

        Focus();
        SlideElement? hit = SlideRenderer.HitTest(_slide, point);
        if (hit != _selectedElement)
        {
            SetSelectedElement(hit, notify: true);
        }

        if (e.Clicks >= 2 && hit is TextElement text)
        {
            BeginTextEdit(text);
            return;
        }

        if (hit is null)
        {
            return;
        }

        _dragElement = hit;
        _dragHandle = hit == _selectedElement ? HitTestHandle(hit.Frame, e.Location) : ResizeHandle.None;
        _lastLogicalPoint = point;
        _gestureId = Guid.NewGuid();
        Capture = true;
        Cursor = _dragHandle == ResizeHandle.None ? Cursors.SizeAll : CursorFor(_dragHandle);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (_slide is null || !TryScreenToSlide(e.Location, out PointF point))
        {
            return;
        }

        if (_dragElement is not null && (e.Button & MouseButtons.Left) != 0)
        {
            ElementFrame updatedFrame = GetUpdatedFrame(_dragElement.Frame, _dragHandle,
                point.X - _lastLogicalPoint.X, point.Y - _lastLogicalPoint.Y);
            _lastLogicalPoint = point;
            if (updatedFrame != _dragElement.Frame)
            {
                FrameChangeRequested?.Invoke(this,
                    new CanvasFrameChangedEventArgs(_dragElement, updatedFrame, _gestureId));
            }
            return;
        }

        Cursor = _selectedElement is not null
            && HitTestHandle(_selectedElement.Frame, e.Location) is var handle
            && handle != ResizeHandle.None
                ? CursorFor(handle)
                : SlideRenderer.HitTest(_slide, point) is null ? Cursors.Default : Cursors.SizeAll;
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        if (e.Button == MouseButtons.Left)
        {
            CancelDrag();
            Cursor = Cursors.Default;
        }
    }

    private void SetSelectedElement(SlideElement? element, bool notify)
    {
        _selectedElement = element;
        Invalidate();
        if (notify)
        {
            SelectionChanged?.Invoke(this,
                new CanvasSelectionChangedEventArgs(element is null ? [] : [element]));
        }
    }

    private void BeginTextEdit(TextElement element)
    {
        CancelTextEdit();
        Rectangle bounds = GetTextEditorBounds(element);
        if (bounds.IsEmpty)
        {
            return;
        }

        _textEditorFont = new Font(
            element.FontFamily,
            Math.Max(7, (float)element.FontSizePoints * GetSlideBounds().Width
                / (float)Presentation.SlideWidth * 96 / 72),
            (element.IsBold ? FontStyle.Bold : FontStyle.Regular)
                | (element.IsItalic ? FontStyle.Italic : FontStyle.Regular));
        TextBox editor = new()
        {
            Bounds = bounds,
            Multiline = true,
            AcceptsReturn = true,
            AcceptsTab = true,
            WordWrap = true,
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = ToOpaqueColor(_slide!.Background),
            ForeColor = ToOpaqueColor(element.Color),
            Text = element.Text,
            Font = _textEditorFont,
            TextAlign = element.Alignment switch
            {
                TextAlignment.Center => HorizontalAlignment.Center,
                TextAlignment.Right => HorizontalAlignment.Right,
                _ => HorizontalAlignment.Left
            }
        };
        editor.KeyDown += TextEditor_KeyDown;
        editor.LostFocus += TextEditor_LostFocus;
        _editingText = element;
        _textEditor = editor;
        Controls.Add(editor);
        editor.BringToFront();
        editor.SelectAll();
        editor.Focus();
    }

    private Rectangle GetTextEditorBounds(TextElement element)
    {
        RectangleF slideBounds = GetSlideBounds();
        float scale = slideBounds.Width / (float)Presentation.SlideWidth;
        float centerX = (float)(element.Frame.X + element.Frame.Width / 2);
        float centerY = (float)(element.Frame.Y + element.Frame.Height / 2);
        float halfWidth = (float)element.Frame.Width / 2;
        float halfHeight = (float)element.Frame.Height / 2;
        float radians = (float)(element.Frame.RotationDegrees * Math.PI / 180);
        PointF[] corners =
        [
            new(-halfWidth, -halfHeight), new(halfWidth, -halfHeight),
            new(halfWidth, halfHeight), new(-halfWidth, halfHeight)
        ];
        for (int index = 0; index < corners.Length; index++)
        {
            PointF corner = corners[index];
            corners[index] = new PointF(
                slideBounds.Left + (centerX + corner.X * MathF.Cos(radians) - corner.Y * MathF.Sin(radians)) * scale,
                slideBounds.Top + (centerY + corner.X * MathF.Sin(radians) + corner.Y * MathF.Cos(radians)) * scale);
        }

        float left = corners.Min(point => point.X);
        float top = corners.Min(point => point.Y);
        float right = corners.Max(point => point.X);
        float bottom = corners.Max(point => point.Y);
        Rectangle bounds = Rectangle.FromLTRB((int)left, (int)top, (int)Math.Ceiling(right), (int)Math.Ceiling(bottom));
        bounds.Intersect(ClientRectangle);
        return bounds.Width > 0 && bounds.Height > 0 ? bounds : Rectangle.Empty;
    }

    private void TextEditor_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Escape)
        {
            CancelTextEdit();
            e.SuppressKeyPress = true;
        }
        else if (e.KeyCode == Keys.Enter && e.Control)
        {
            CommitTextEdit();
            e.SuppressKeyPress = true;
        }
    }

    private void TextEditor_LostFocus(object? sender, EventArgs e) => CommitTextEdit();

    private void CommitTextEdit()
    {
        if (_textEditor is null || _editingText is null)
        {
            return;
        }

        TextBox editor = _textEditor;
        TextElement element = _editingText;
        string text = editor.Text;
        _textEditor = null;
        _editingText = null;
        Controls.Remove(editor);
        editor.Dispose();
        _textEditorFont?.Dispose();
        _textEditorFont = null;
        if (text != element.Text)
        {
            TextChangeRequested?.Invoke(this, new CanvasTextChangedEventArgs(element, text));
        }
        Invalidate();
    }

    private void CancelTextEdit()
    {
        if (_textEditor is null)
        {
            return;
        }

        TextBox editor = _textEditor;
        _textEditor = null;
        _editingText = null;
        Controls.Remove(editor);
        editor.Dispose();
        _textEditorFont?.Dispose();
        _textEditorFont = null;
        Invalidate();
    }

    private RectangleF GetSlideBounds()
    {
        float availableWidth = Width - CanvasPadding * 2;
        float availableHeight = Height - CanvasPadding * 2;
        float scale = Math.Min(availableWidth / (float)Presentation.SlideWidth,
            availableHeight / (float)Presentation.SlideHeight);
        float width = (float)Presentation.SlideWidth * scale;
        float height = (float)Presentation.SlideHeight * scale;
        return new RectangleF((Width - width) / 2, (Height - height) / 2, width, height);
    }

    private bool TryScreenToSlide(Point screenPoint, out PointF logicalPoint)
    {
        RectangleF bounds = GetSlideBounds();
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            logicalPoint = PointF.Empty;
            return false;
        }

        logicalPoint = new PointF(
            (screenPoint.X - bounds.Left) * (float)Presentation.SlideWidth / bounds.Width,
            (screenPoint.Y - bounds.Top) * (float)Presentation.SlideHeight / bounds.Height);
        return bounds.Contains(screenPoint);
    }

    private void DrawSelection(Graphics graphics, RectangleF destination)
    {
        if (_selectedElement is null)
        {
            return;
        }

        ElementFrame frame = _selectedElement.Frame;
        float scale = destination.Width / (float)Presentation.SlideWidth;
        float handleSize = HandleSize / scale;
        GraphicsState state = graphics.Save();
        graphics.SetClip(destination);
        graphics.TranslateTransform(destination.Left, destination.Top);
        graphics.ScaleTransform(scale, scale);
        graphics.TranslateTransform((float)(frame.X + frame.Width / 2), (float)(frame.Y + frame.Height / 2));
        graphics.RotateTransform((float)frame.RotationDegrees);
        RectangleF bounds = new((float)-frame.Width / 2, (float)-frame.Height / 2,
            (float)frame.Width, (float)frame.Height);
        using Pen selectionPen = new(Color.FromArgb(255, 255, 101, 24), 2 / scale);
        using SolidBrush handleBrush = new(Color.White);
        using Pen handlePen = new(Color.FromArgb(255, 255, 101, 24), 1 / scale);
        graphics.DrawRectangle(selectionPen, bounds.X, bounds.Y, bounds.Width, bounds.Height);
        foreach (PointF handle in GetHandlePoints(bounds))
        {
            RectangleF handleBounds = new(handle.X - handleSize / 2, handle.Y - handleSize / 2,
                handleSize, handleSize);
            graphics.FillRectangle(handleBrush, handleBounds);
            graphics.DrawRectangle(handlePen, handleBounds.X, handleBounds.Y,
                handleBounds.Width, handleBounds.Height);
        }
        graphics.Restore(state);
    }

    private ResizeHandle HitTestHandle(ElementFrame frame, Point screenPoint)
    {
        RectangleF destination = GetSlideBounds();
        float scale = destination.Width / (float)Presentation.SlideWidth;
        PointF logical = new(
            (screenPoint.X - destination.Left) / scale,
            (screenPoint.Y - destination.Top) / scale);
        PointF center = new((float)(frame.X + frame.Width / 2), (float)(frame.Y + frame.Height / 2));
        float angle = (float)(frame.RotationDegrees * Math.PI / 180);
        RectangleF bounds = new((float)-frame.Width / 2, (float)-frame.Height / 2,
            (float)frame.Width, (float)frame.Height);
        ResizeHandle[] handles =
        [
            ResizeHandle.TopLeft, ResizeHandle.Top, ResizeHandle.TopRight, ResizeHandle.Right,
            ResizeHandle.BottomRight, ResizeHandle.Bottom, ResizeHandle.BottomLeft, ResizeHandle.Left
        ];
        PointF[] points = GetHandlePoints(bounds);
        float tolerance = HandleSize / scale;
        for (int index = 0; index < points.Length; index++)
        {
            float x = center.X + points[index].X * MathF.Cos(angle) - points[index].Y * MathF.Sin(angle);
            float y = center.Y + points[index].X * MathF.Sin(angle) + points[index].Y * MathF.Cos(angle);
            if (Math.Abs(logical.X - x) <= tolerance && Math.Abs(logical.Y - y) <= tolerance)
            {
                return handles[index];
            }
        }

        return ResizeHandle.None;
    }

    private static PointF[] GetHandlePoints(RectangleF bounds) =>
    [
        new(bounds.Left, bounds.Top),
        new(bounds.Left + bounds.Width / 2, bounds.Top),
        new(bounds.Right, bounds.Top),
        new(bounds.Right, bounds.Top + bounds.Height / 2),
        new(bounds.Right, bounds.Bottom),
        new(bounds.Left + bounds.Width / 2, bounds.Bottom),
        new(bounds.Left, bounds.Bottom),
        new(bounds.Left, bounds.Top + bounds.Height / 2)
    ];

    private static ElementFrame GetUpdatedFrame(ElementFrame frame, ResizeHandle handle, float deltaX, float deltaY)
    {
        if (handle == ResizeHandle.None)
        {
            return frame.Moved(deltaX, deltaY);
        }

        double radians = -frame.RotationDegrees * Math.PI / 180;
        double localX = deltaX * Math.Cos(radians) - deltaY * Math.Sin(radians);
        double localY = deltaX * Math.Sin(radians) + deltaY * Math.Cos(radians);
        double x = frame.X;
        double y = frame.Y;
        double width = frame.Width;
        double height = frame.Height;

        if (handle is ResizeHandle.TopLeft or ResizeHandle.Left or ResizeHandle.BottomLeft)
        {
            double applied = Math.Min(localX, width - 1);
            x += applied;
            width -= applied;
        }
        else if (handle is ResizeHandle.TopRight or ResizeHandle.Right or ResizeHandle.BottomRight)
        {
            width = Math.Max(1, width + localX);
        }

        if (handle is ResizeHandle.TopLeft or ResizeHandle.Top or ResizeHandle.TopRight)
        {
            double applied = Math.Min(localY, height - 1);
            y += applied;
            height -= applied;
        }
        else if (handle is ResizeHandle.BottomLeft or ResizeHandle.Bottom or ResizeHandle.BottomRight)
        {
            height = Math.Max(1, height + localY);
        }

        return frame with { X = x, Y = y, Width = width, Height = height };
    }

    private static Cursor CursorFor(ResizeHandle handle) => handle switch
    {
        ResizeHandle.Top or ResizeHandle.Bottom => Cursors.SizeNS,
        ResizeHandle.Left or ResizeHandle.Right => Cursors.SizeWE,
        ResizeHandle.TopLeft or ResizeHandle.BottomRight => Cursors.SizeNWSE,
        ResizeHandle.TopRight or ResizeHandle.BottomLeft => Cursors.SizeNESW,
        _ => Cursors.SizeAll
    };

    private static Color ToOpaqueColor(ArgbColor color)
    {
        int alpha = color.A;
        return Color.FromArgb(
            255,
            (color.R * alpha + 15 * (255 - alpha)) / 255,
            (color.G * alpha + 17 * (255 - alpha)) / 255,
            (color.B * alpha + 26 * (255 - alpha)) / 255);
    }

    private enum ResizeHandle
    {
        None,
        TopLeft,
        Top,
        TopRight,
        Right,
        BottomRight,
        Bottom,
        BottomLeft,
        Left
    }
}

internal sealed class CanvasSelectionChangedEventArgs(IReadOnlyList<SlideElement> selection) : EventArgs
{
    public IReadOnlyList<SlideElement> Selection { get; } = selection;
}

internal sealed class CanvasFrameChangedEventArgs(SlideElement element, ElementFrame frame, Guid gestureId) : EventArgs
{
    public SlideElement Element { get; } = element;
    public ElementFrame Frame { get; } = frame;
    public Guid GestureId { get; } = gestureId;
}

internal sealed class CanvasTextChangedEventArgs(TextElement element, string text) : EventArgs
{
    public TextElement Element { get; } = element;
    public string Text { get; } = text;
}
