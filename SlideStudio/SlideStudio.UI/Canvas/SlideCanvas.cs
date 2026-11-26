using System.Drawing.Drawing2D;
using SlideStudio.Core.Model;

namespace SlideStudio.UI.Canvas;

/// <summary>Affiche une diapositive et permet de sélectionner, déplacer et redimensionner ses éléments.</summary>
internal sealed class SlideCanvas : Control
{
    private const float CanvasPadding = 24;
    private const float HandleSize = 9;
    private const float RotationHandleOffset = 28;
    private const float MinimumZoom = 0.25f;
    private const float MaximumZoom = 3;
    private const string KeyboardAccessibilityHelp =
        "Ctrl+A sélectionne tous les éléments. Utilisez les flèches pour déplacer la sélection, Maj avec les flèches pour la déplacer par incréments plus grands, Suppr pour supprimer et Échap pour désélectionner.";
    private readonly SlideRenderer _renderer;
    private Slide? _slide;
    private SlideElement? _selectedElement;
    private readonly List<SlideElement> _selectedElements = [];
    private TextBox? _textEditor;
    private TextElement? _editingText;
    private Font? _textEditorFont;
    private SlideElement[] _dragElements = [];
    private ResizeHandle _dragHandle;
    private PointF _lastLogicalPoint;
    private PointF _rotationCenter;
    private double _initialPointerAngle;
    private ElementFrame _initialFrame;
    private Guid _gestureId;
    private float _zoomFactor = 1;
    private PointF _panOffset;
    private Point _lastPanPoint;
    private bool _isPanning;

    public event EventHandler<CanvasSelectionChangedEventArgs>? SelectionChanged;
    public event EventHandler<CanvasFramesChangedEventArgs>? FramesChangeRequested;
    public event EventHandler<CanvasTextChangedEventArgs>? TextChangeRequested;
    public event EventHandler<CanvasDeletionRequestedEventArgs>? DeletionRequested;
    public event EventHandler? ZoomChanged;

    public float ZoomFactor => _zoomFactor;

    public SlideCanvas(SlideRenderer renderer)
    {
        ArgumentNullException.ThrowIfNull(renderer);
        _renderer = renderer;
        Dock = DockStyle.Fill;
        BackColor = Color.FromArgb(15, 17, 26);
        TabStop = true;
        AccessibleRole = AccessibleRole.Graphic;
        AccessibleName = "Canevas de la diapositive";
        AccessibleDescription = KeyboardAccessibilityHelp;
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
        _selectedElements.RemoveAll(element => slide is null || !slide.Elements.Contains(element));
        if (_selectedElement is not null && !_selectedElements.Contains(_selectedElement))
        {
            _selectedElement = _selectedElements.LastOrDefault();
        }
        Invalidate();
    }

    public void SetSelection(IReadOnlyList<SlideElement> selection)
    {
        ArgumentNullException.ThrowIfNull(selection);
        _selectedElements.Clear();
        if (_slide is not null)
        {
            _selectedElements.AddRange(selection.Where(_slide.Elements.Contains).Distinct());
        }
        _selectedElement = _selectedElements.LastOrDefault();
        Invalidate();
    }

    public void SetAccessibleStatus(string status)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(status);
        string description = $"{status} {KeyboardAccessibilityHelp}";
        if (AccessibleDescription == description)
        {
            return;
        }

        AccessibleDescription = description;
        AccessibilityNotifyClients(AccessibleEvents.DescriptionChange, -1);
    }

    public void ZoomBy(float factor, Point? anchor = null)
    {
        if (!float.IsFinite(factor) || factor <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(factor), "Le facteur de zoom doit être positif et fini.");
        }
        float zoom = Math.Clamp(_zoomFactor * factor, MinimumZoom, MaximumZoom);
        if (Math.Abs(zoom - _zoomFactor) < 0.001f)
        {
            return;
        }

        RectangleF oldBounds = GetSlideBounds();
        float oldScale = oldBounds.Width / (float)Presentation.SlideWidth;
        Point zoomAnchor = anchor ?? new Point(Width / 2, Height / 2);
        float logicalX = oldScale > 0 ? (zoomAnchor.X - oldBounds.Left) / oldScale : (float)Presentation.SlideWidth / 2;
        float logicalY = oldScale > 0 ? (zoomAnchor.Y - oldBounds.Top) / oldScale : (float)Presentation.SlideHeight / 2;
        _zoomFactor = zoom;
        float newScale = GetFitScale() * _zoomFactor;
        float newLeft = zoomAnchor.X - logicalX * newScale;
        float newTop = zoomAnchor.Y - logicalY * newScale;
        _panOffset = new PointF(
            newLeft - (Width - (float)Presentation.SlideWidth * newScale) / 2,
            newTop - (Height - (float)Presentation.SlideHeight * newScale) / 2);
        Invalidate();
        ZoomChanged?.Invoke(this, EventArgs.Empty);
    }

    public void ResetZoom()
    {
        if (_zoomFactor == 1 && _panOffset.IsEmpty)
        {
            return;
        }

        _zoomFactor = 1;
        _panOffset = PointF.Empty;
        Invalidate();
        ZoomChanged?.Invoke(this, EventArgs.Empty);
    }

    public void CancelDrag()
    {
        _dragElements = [];
        _dragHandle = ResizeHandle.None;
        _gestureId = Guid.Empty;
        _isPanning = false;
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

    protected override void OnMouseWheel(MouseEventArgs e)
    {
        if ((ModifierKeys & Keys.Control) != 0)
        {
            ZoomBy(e.Delta > 0 ? 1.1f : 1 / 1.1f, e.Location);
            return;
        }

        base.OnMouseWheel(e);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (_slide is null)
        {
            return;
        }

        if (e.Control && e.KeyCode == Keys.A)
        {
            SetSelectedElements(_slide.Elements, notify: true);
            e.SuppressKeyPress = true;
            e.Handled = true;
            return;
        }

        if (e.KeyCode == Keys.Escape)
        {
            CancelDrag();
            e.SuppressKeyPress = true;
            e.Handled = true;
            return;
        }

        if (e.KeyCode == Keys.Delete && _selectedElements.Count > 0)
        {
            DeletionRequested?.Invoke(this,
                new CanvasDeletionRequestedEventArgs(_selectedElements.ToArray()));
            e.SuppressKeyPress = true;
            e.Handled = true;
            return;
        }

        float step = (e.Shift ? 10 : 1);
        float deltaX = e.KeyCode switch
        {
            Keys.Left => -step,
            Keys.Right => step,
            _ => 0
        };
        float deltaY = e.KeyCode switch
        {
            Keys.Up => -step,
            Keys.Down => step,
            _ => 0
        };
        if (deltaX == 0 && deltaY == 0)
        {
            return;
        }

        Guid gestureId = Guid.NewGuid();
        (SlideElement Element, ElementFrame Frame)[] updates = _selectedElements
            .Select(element => (element, element.Frame.Moved(deltaX, deltaY)))
            .Where(update => update.Item1.Frame != update.Item2)
            .ToArray();
        if (updates.Length > 0)
        {
            FramesChangeRequested?.Invoke(this,
                new CanvasFramesChangedEventArgs(updates, gestureId));
            e.SuppressKeyPress = true;
            e.Handled = true;
        }
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button == MouseButtons.Middle && _slide is not null)
        {
            _isPanning = true;
            _lastPanPoint = e.Location;
            Capture = true;
            Cursor = Cursors.Hand;
            return;
        }

        if (e.Button != MouseButtons.Left || _slide is null)
        {
            return;
        }

        bool insideSlide = TryScreenToSlide(e.Location, out PointF point);
        bool rotationHandleHit = _selectedElements.Count == 1
            && _selectedElement is not null
            && HitTestHandle(_selectedElement.Frame, e.Location) == ResizeHandle.Rotate;
        if (!insideSlide && !rotationHandleHit)
        {
            return;
        }

        Focus();
        SlideElement? hit = rotationHandleHit ? _selectedElement : SlideRenderer.HitTest(_slide, point);
        bool controlPressed = (ModifierKeys & Keys.Control) != 0;
        if (controlPressed)
        {
            if (hit is not null)
            {
                if (_selectedElements.Remove(hit))
                {
                    _selectedElement = _selectedElements.LastOrDefault();
                }
                else
                {
                    _selectedElements.Add(hit);
                    _selectedElement = hit;
                }
                NotifySelectionChanged();
            }
            return;
        }
        else if (hit is null)
        {
            SetSelectedElements([], notify: true);
        }
        else if (!_selectedElements.Contains(hit))
        {
            SetSelectedElements([hit], notify: true);
        }
        else
        {
            _selectedElement = hit;
        }

        if (!controlPressed && e.Clicks >= 2 && hit is TextElement text)
        {
            BeginTextEdit(text);
            return;
        }

        if (hit is null)
        {
            return;
        }

        _dragElements = _selectedElements.ToArray();
        _dragHandle = _dragElements.Length == 1
            ? HitTestHandle(hit.Frame, e.Location)
            : ResizeHandle.None;
        _lastLogicalPoint = point;
        _initialFrame = hit.Frame;
        if (_dragHandle == ResizeHandle.Rotate)
        {
            _rotationCenter = new PointF(
                (float)(hit.Frame.X + hit.Frame.Width / 2),
                (float)(hit.Frame.Y + hit.Frame.Height / 2));
            _initialPointerAngle = Math.Atan2(point.Y - _rotationCenter.Y, point.X - _rotationCenter.X);
        }
        _gestureId = Guid.NewGuid();
        Capture = true;
        Cursor = CursorFor(_dragHandle);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (_slide is null)
        {
            return;
        }

        if (_isPanning && (e.Button & MouseButtons.Middle) != 0)
        {
            _panOffset.X += e.X - _lastPanPoint.X;
            _panOffset.Y += e.Y - _lastPanPoint.Y;
            _lastPanPoint = e.Location;
            Invalidate();
            return;
        }

        bool insideSlide = TryScreenToSlide(e.Location, out PointF point);
        if (!insideSlide && _dragHandle != ResizeHandle.Rotate)
        {
            return;
        }

        if (_dragElements.Length > 0 && (e.Button & MouseButtons.Left) != 0)
        {
            float deltaX = point.X - _lastLogicalPoint.X;
            float deltaY = point.Y - _lastLogicalPoint.Y;
            _lastLogicalPoint = point;
            (SlideElement Element, ElementFrame Frame)[] updates;
            if (_dragHandle == ResizeHandle.Rotate)
            {
                double currentAngle = Math.Atan2(point.Y - _rotationCenter.Y, point.X - _rotationCenter.X);
                double deltaAngle = (currentAngle - _initialPointerAngle) * 180 / Math.PI;
                ElementFrame rotatedFrame = _initialFrame with
                {
                    RotationDegrees = ElementFrame.NormalizeRotation(_initialFrame.RotationDegrees + deltaAngle)
                };
                updates = [(_dragElements[0], rotatedFrame)];
            }
            else
            {
                updates = _dragElements
                    .Select(element => (element, GetUpdatedFrame(
                        element.Frame,
                        _dragHandle,
                        deltaX,
                        deltaY)))
                    .ToArray();
            }

            if (updates.Any(update => update.Element.Frame != update.Frame))
            {
                FramesChangeRequested?.Invoke(this,
                    new CanvasFramesChangedEventArgs(updates, _gestureId));
            }
            return;
        }

        Cursor = _selectedElements.Count == 1 && _selectedElement is not null
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
        else if (e.Button == MouseButtons.Middle)
        {
            _isPanning = false;
            Capture = false;
            Cursor = Cursors.Default;
        }
    }

    private void SetSelectedElements(IEnumerable<SlideElement> elements, bool notify)
    {
        _selectedElements.Clear();
        _selectedElements.AddRange(elements);
        _selectedElement = _selectedElements.LastOrDefault();
        Invalidate();
        if (notify)
        {
            NotifySelectionChanged();
        }
    }

    private void NotifySelectionChanged()
    {
        Invalidate();
        SelectionChanged?.Invoke(this, new CanvasSelectionChangedEventArgs(_selectedElements.ToArray()));
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
        float scale = GetFitScale() * _zoomFactor;
        float width = (float)Presentation.SlideWidth * scale;
        float height = (float)Presentation.SlideHeight * scale;
        return new RectangleF(
            (Width - width) / 2 + _panOffset.X,
            (Height - height) / 2 + _panOffset.Y,
            width,
            height);
    }

    private float GetFitScale() => Math.Max(0, Math.Min(
        (Width - CanvasPadding * 2) / (float)Presentation.SlideWidth,
        (Height - CanvasPadding * 2) / (float)Presentation.SlideHeight));

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
        if (_selectedElements.Count == 0)
        {
            return;
        }

        float scale = destination.Width / (float)Presentation.SlideWidth;
        float handleSize = HandleSize / scale;
        GraphicsState state = graphics.Save();
        graphics.SetClip(ClientRectangle);
        graphics.TranslateTransform(destination.Left, destination.Top);
        graphics.ScaleTransform(scale, scale);
        using Pen selectionPen = new(Color.FromArgb(255, 255, 101, 24), 2 / scale);
        using SolidBrush handleBrush = new(Color.White);
        using Pen handlePen = new(Color.FromArgb(255, 255, 101, 24), 1 / scale);
        foreach (SlideElement element in _selectedElements)
        {
            ElementFrame frame = element.Frame;
            GraphicsState elementState = graphics.Save();
            graphics.TranslateTransform((float)(frame.X + frame.Width / 2), (float)(frame.Y + frame.Height / 2));
            graphics.RotateTransform((float)frame.RotationDegrees);
            RectangleF bounds = new((float)-frame.Width / 2, (float)-frame.Height / 2,
                (float)frame.Width, (float)frame.Height);
            graphics.DrawRectangle(selectionPen, bounds.X, bounds.Y, bounds.Width, bounds.Height);

            if (_selectedElements.Count == 1)
            {
                foreach (PointF handle in GetHandlePoints(bounds))
                {
                    RectangleF handleBounds = new(handle.X - handleSize / 2, handle.Y - handleSize / 2,
                        handleSize, handleSize);
                    graphics.FillRectangle(handleBrush, handleBounds);
                    graphics.DrawRectangle(handlePen, handleBounds.X, handleBounds.Y,
                        handleBounds.Width, handleBounds.Height);
                }

                PointF rotationHandle = GetRotationHandlePoint(bounds, scale);
                using Pen connector = new(Color.FromArgb(255, 255, 101, 24), 1 / scale);
                graphics.DrawLine(connector, bounds.Left + bounds.Width / 2, bounds.Top,
                    rotationHandle.X, rotationHandle.Y);
                graphics.FillEllipse(handleBrush, rotationHandle.X - handleSize / 2,
                    rotationHandle.Y - handleSize / 2, handleSize, handleSize);
                graphics.DrawEllipse(handlePen, rotationHandle.X - handleSize / 2,
                    rotationHandle.Y - handleSize / 2, handleSize, handleSize);
            }

            graphics.Restore(elementState);
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
        PointF rotationHandle = GetRotationHandlePoint(bounds, scale);
        float rotatedHandleX = center.X + rotationHandle.X * MathF.Cos(angle) - rotationHandle.Y * MathF.Sin(angle);
        float rotatedHandleY = center.Y + rotationHandle.X * MathF.Sin(angle) + rotationHandle.Y * MathF.Cos(angle);
        if (Math.Abs(logical.X - rotatedHandleX) <= tolerance
            && Math.Abs(logical.Y - rotatedHandleY) <= tolerance)
        {
            return ResizeHandle.Rotate;
        }

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

    private static PointF GetRotationHandlePoint(RectangleF bounds, float scale) =>
        new(bounds.Left + bounds.Width / 2, bounds.Top - RotationHandleOffset / scale);

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
        ResizeHandle.Rotate => Cursors.Hand,
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
        Rotate,
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

internal sealed class CanvasFramesChangedEventArgs(
    IReadOnlyList<(SlideElement Element, ElementFrame Frame)> frames,
    Guid gestureId) : EventArgs
{
    public IReadOnlyList<(SlideElement Element, ElementFrame Frame)> Frames { get; } = frames;
    public Guid GestureId { get; } = gestureId;
}

internal sealed class CanvasTextChangedEventArgs(TextElement element, string text) : EventArgs
{
    public TextElement Element { get; } = element;
    public string Text { get; } = text;
}

internal sealed class CanvasDeletionRequestedEventArgs(IReadOnlyList<SlideElement> elements) : EventArgs
{
    public IReadOnlyList<SlideElement> Elements { get; } = elements;
}
