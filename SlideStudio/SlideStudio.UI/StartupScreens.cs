using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace SlideStudio.UI;

internal enum StartupAction
{
    NewPresentation,
    OpenPresentation
}

internal sealed class WelcomeScreen : UserControl
{
    private static readonly Color WindowColor = UiPalette.Window;
    private static readonly Color PanelColor = UiPalette.Panel;
    private static readonly Color BorderColor = UiPalette.Border;
    private static readonly Color PrimaryTextColor = UiPalette.PrimaryText;
    private static readonly Color MutedTextColor = UiPalette.MutedText;
    private static readonly Color AccentColor = UiPalette.Accent;
    private static readonly Color AccentLightColor = UiPalette.AccentPressed;
    private const int SceneTop = 66;
    private readonly System.Windows.Forms.Timer _motionTimer = new() { Interval = 16 };
    private Bitmap? _staticBackground;
    private Bitmap? _createSlideArtwork;
    private Bitmap? _openSlideArtwork;
    private Bitmap? _createCardArtwork;
    private Bitmap? _openCardArtwork;
    private Rectangle _createCardBounds;
    private Rectangle _openCardBounds;
    private Rectangle _selectorBounds;
    private float _createHover;
    private float _openHover;
    private int _hoveredCard = -1;
    private int _focusedCard = -1;
    private int _pressedCard = -1;
    private int _selectedCard;
    private float _carouselPosition;
    private bool _activateAfterTransition;
    private double _phase;
    private long _lastMotionTick;

    public event EventHandler<StartupAction>? ActionRequested;

    public WelcomeScreen()
    {
        Dock = DockStyle.Fill;
        BackColor = WindowColor;
        ForeColor = PrimaryTextColor;
        TabStop = true;
        AccessibleRole = AccessibleRole.Pane;
        AccessibleName = "Accueil SlideStudio";
        UpdateAccessibleStatus();
        SetStyle(
            ControlStyles.UserPaint
                | ControlStyles.AllPaintingInWmPaint
                | ControlStyles.OptimizedDoubleBuffer
                | ControlStyles.Opaque
                | ControlStyles.ResizeRedraw,
            true);

        _lastMotionTick = Environment.TickCount64;
        _motionTimer.Tick += (_, _) =>
        {
            long now = Environment.TickCount64;
            double elapsed = Math.Min((now - _lastMotionTick) / 1000d, 0.05d);
            _lastMotionTick = now;
            _phase += elapsed * 1.17;
            _carouselPosition += (_selectedCard - _carouselPosition) * (float)Math.Min(1, elapsed * 9);
            if (Math.Abs(_selectedCard - _carouselPosition) < 0.002f)
            {
                _carouselPosition = _selectedCard;
                if (_activateAfterTransition)
                {
                    _activateAfterTransition = false;
                    ActionRequested?.Invoke(this, _selectedCard == 0
                        ? StartupAction.NewPresentation
                        : StartupAction.OpenPresentation);
                }
            }

            float previousCreateHover = _createHover;
            float previousOpenHover = _openHover;
            _createHover += ((_hoveredCard == 0 ? 1 : 0) - _createHover) * 0.22f;
            _openHover += ((_hoveredCard == 1 ? 1 : 0) - _openHover) * 0.22f;
            if (Math.Abs(previousCreateHover - _createHover) > 0.001f
                || Math.Abs(previousOpenHover - _openHover) > 0.001f
                || elapsed > 0)
            {
                Invalidate();
            }
        };
        _motionTimer.Start();
    }

    protected override void OnSizeChanged(EventArgs e)
    {
        base.OnSizeChanged(e);
        RebuildArtwork();
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        if (_staticBackground is null || _createSlideArtwork is null || _openSlideArtwork is null
            || _createCardArtwork is null || _openCardArtwork is null)
        {
            return;
        }

        Graphics graphics = e.Graphics;
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.DrawImageUnscaled(_staticBackground, Point.Empty);
        float selectorBob = (float)Math.Sin(_phase) * 7;
        Bitmap selectorArtwork = _selectedCard == 0 ? _createSlideArtwork! : _openSlideArtwork!;
        float selectorX = Width * 0.5f - 115;
        float selectorY = SceneTop + SceneHeight * 0.31f;
        DrawFloatingSlide(graphics, selectorArtwork, selectorX, selectorY,
            selectorBob, 1.05f);
        _selectorBounds = new Rectangle((int)(selectorX - 18), (int)(selectorY - 22),
            (int)(200 * 1.05f + 36), (int)(140 * 1.05f + 36));

        int sideCard = _selectedCard == 0 ? 1 : 0;
        DrawCarouselCard(graphics, sideCard, sideCard == 0 ? _createCardArtwork : _openCardArtwork,
            sideCard == 0 ? _createHover : _openHover, false, _pressedCard == sideCard);
        DrawCarouselCard(graphics, _selectedCard, _selectedCard == 0 ? _createCardArtwork : _openCardArtwork,
            _selectedCard == 0 ? _createHover : _openHover, true, _pressedCard == _selectedCard);
        _createCardBounds = GetCardBounds(0);
        _openCardBounds = GetCardBounds(1);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        _createCardBounds = GetCardBounds(0);
        _openCardBounds = GetCardBounds(1);
        int hoveredCard = _selectorBounds.Contains(e.Location) ? _selectedCard
            : _createCardBounds.Contains(e.Location) ? 0
            : _openCardBounds.Contains(e.Location) ? 1
            : -1;
        if (hoveredCard != _hoveredCard)
        {
            _hoveredCard = hoveredCard;
            Cursor = hoveredCard >= 0 ? Cursors.Hand : Cursors.Default;
        }
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        _hoveredCard = -1;
        Cursor = Cursors.Default;
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button != MouseButtons.Left)
        {
            return;
        }

        _pressedCard = GetCardAt(e.Location);
        _focusedCard = _pressedCard >= 0 ? _pressedCard : _focusedCard;
        if (_pressedCard >= 0)
        {
            Focus();
            Invalidate();
        }
    }

    protected override void OnGotFocus(EventArgs e)
    {
        base.OnGotFocus(e);
        if (_focusedCard < 0)
        {
            _focusedCard = _selectedCard;
        }
        UpdateAccessibleStatus();
        Invalidate();
    }

    protected override bool IsInputKey(Keys keyData) =>
        IsCarouselNavigationKey(keyData) || base.IsInputKey(keyData);

    internal static bool IsCarouselNavigationKey(Keys keyData) =>
        (keyData & Keys.KeyCode) is Keys.Left or Keys.Right;

    protected override void OnMouseUp(MouseEventArgs e)
    {
        int releasedCard = GetCardAt(e.Location);
        bool select = e.Button == MouseButtons.Left && _pressedCard >= 0 && _pressedCard == releasedCard;
        _pressedCard = -1;
        base.OnMouseUp(e);
        Invalidate();
        if (select)
        {
            if (releasedCard == _selectedCard)
            {
                ActionRequested?.Invoke(this, _selectedCard == 0
                    ? StartupAction.NewPresentation
                    : StartupAction.OpenPresentation);
            }
            else
            {
                SelectCard(releasedCard, activateAfterTransition: true);
            }
        }
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (HandleNavigationKey(e.KeyCode))
        {
            e.Handled = true;
        }
    }

    internal bool HandleNavigationKey(Keys key)
    {
        if (key is Keys.Left or Keys.Right)
        {
            SelectCard(_selectedCard == 0 ? 1 : 0, activateAfterTransition: false);
            return true;
        }

        if (key is Keys.Enter or Keys.Space)
        {
            int card = _focusedCard >= 0 ? _focusedCard : _selectedCard;
            ActionRequested?.Invoke(this, card == 0
                ? StartupAction.NewPresentation
                : StartupAction.OpenPresentation);
            return true;
        }

        return false;
    }

    protected override void OnMouseWheel(MouseEventArgs e)
    {
        base.OnMouseWheel(e);
        if (e.Delta != 0)
        {
            SelectCard(_selectedCard == 0 ? 1 : 0, activateAfterTransition: false);
        }
    }

    private void SelectCard(int card, bool activateAfterTransition)
    {
        _focusedCard = card;
        if (_selectedCard != card)
        {
            _selectedCard = card;
            _activateAfterTransition = activateAfterTransition;
        }
        else if (activateAfterTransition)
        {
            ActionRequested?.Invoke(this, card == 0
                ? StartupAction.NewPresentation
                : StartupAction.OpenPresentation);
        }
        UpdateAccessibleStatus();
        Invalidate();
    }

    private void UpdateAccessibleStatus()
    {
        string selectedAction = _selectedCard == 0
            ? "Créer une présentation"
            : "Ouvrir un projet";
        string description =
            $"Action sélectionnée : {selectedAction}. Utilisez les flèches gauche et droite pour choisir, puis Entrée ou Espace pour continuer.";
        if (AccessibleDescription == description)
        {
            return;
        }

        AccessibleDescription = description;
        if (IsHandleCreated)
        {
            AccessibilityNotifyClients(AccessibleEvents.DescriptionChange, -1);
        }
    }

    private int GetCardAt(Point point) => _selectorBounds.Contains(point) ? _selectedCard
        : _createCardBounds.Contains(point) ? 0
        : _openCardBounds.Contains(point) ? 1
        : -1;

    private Rectangle GetCardBounds(int card)
    {
        float distance = card - _carouselPosition;
        float scale = Math.Max(0.58f, 1f - Math.Abs(distance) * 0.28f);
        float centerX = Width * 0.5f + distance * Math.Min(Width * 0.31f, (_createCardArtwork?.Width ?? 350) * 0.83f);
        float centerY = SceneTop + SceneHeight * 0.69f + Math.Abs(distance) * 28;
        float angle = -distance * 9;
        float radians = angle * MathF.PI / 180;
        int width = _createCardArtwork?.Width ?? 350;
        int height = _createCardArtwork?.Height ?? 226;
        int boundsWidth = (int)Math.Ceiling((Math.Abs(MathF.Cos(radians)) * width
            + Math.Abs(MathF.Sin(radians)) * height) * scale);
        int boundsHeight = (int)Math.Ceiling((Math.Abs(MathF.Sin(radians)) * width
            + Math.Abs(MathF.Cos(radians)) * height) * scale);
        return new Rectangle(
            (int)(centerX - boundsWidth / 2f),
            (int)(centerY - boundsHeight / 2f),
            boundsWidth,
            boundsHeight);
    }

    private int SceneHeight => Height - 91;

    private void RebuildArtwork()
    {
        DisposeArtwork();
        if (Width <= 0 || Height <= 0)
        {
            return;
        }

        int cardWidth = Math.Min(360, Math.Max(240, Width * 40 / 100));
        int cardHeight = cardWidth * 226 / 350;

        _staticBackground = new Bitmap(Width, Height, PixelFormat.Format32bppPArgb);
        using (Graphics graphics = Graphics.FromImage(_staticBackground))
        {
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            DrawStaticBackground(graphics);
        }

        _createSlideArtwork = CreateSlideArtwork(true);
        _openSlideArtwork = CreateSlideArtwork(false);
        _createCardArtwork = CreateActionCardArtwork(cardWidth, cardHeight, true);
        _openCardArtwork = CreateActionCardArtwork(cardWidth, cardHeight, false);
        _createCardBounds = GetCardBounds(0);
        _openCardBounds = GetCardBounds(1);
        Invalidate();
    }

    private void DisposeArtwork()
    {
        _staticBackground?.Dispose();
        _createSlideArtwork?.Dispose();
        _openSlideArtwork?.Dispose();
        _createCardArtwork?.Dispose();
        _openCardArtwork?.Dispose();
        _staticBackground = null;
        _createSlideArtwork = null;
        _openSlideArtwork = null;
        _createCardArtwork = null;
        _openCardArtwork = null;
    }

    private void DrawStaticBackground(Graphics graphics)
    {
        using LinearGradientBrush background = new(ClientRectangle, WindowColor, Color.FromArgb(21, 25, 38), LinearGradientMode.Vertical);
        graphics.FillRectangle(background, ClientRectangle);

        Rectangle scene = new(25, SceneTop, Width - 50, SceneHeight);
        if (scene.Width <= 0 || scene.Height <= 0)
        {
            return;
        }

        Rectangle shadowBounds = Rectangle.Inflate(scene, 0, 2);
        shadowBounds.Offset(0, 5);
        using SolidBrush frameShadow = new(Color.FromArgb(76, 0, 0, 0));
        FillRounded(graphics, frameShadow, shadowBounds, 30);
        using LinearGradientBrush frame = new(scene, Color.FromArgb(34, 40, 56), Color.FromArgb(19, 23, 34), LinearGradientMode.Vertical);
        FillRounded(graphics, frame, scene, 30);
        using Pen frameBorder = new(Color.FromArgb(150, BorderColor), 1.2f);
        DrawRounded(graphics, frameBorder, scene, 30);

        DrawGlow(graphics, new PointF(Width * 0.5f, scene.Top + scene.Height * 0.48f), Math.Min(Width, Height) * 0.27f, 0.12f);
        DrawHeroLandscape(graphics, scene);
        DrawStaticText(graphics, scene);
    }

    private void DrawStaticText(Graphics graphics, Rectangle scene)
    {
        using SolidBrush accent = new(AccentColor);
        using SolidBrush primary = new(PrimaryTextColor);
        using SolidBrush muted = new(MutedTextColor);
        using Font brandFont = new(UiPalette.DisplayFontFamily, 12, FontStyle.Bold);
        using Font editionFont = new(UiPalette.TextFontFamily, 8.5f, FontStyle.Bold);
        using Font eyebrowFont = new(UiPalette.TextFontFamily, 9, FontStyle.Bold);
        using Font titleFont = new(UiPalette.DisplayFontFamily, 30, FontStyle.Regular);
        using Font descriptionFont = new(UiPalette.TextFontFamily, 11, FontStyle.Regular);
        using Font footerFont = new(UiPalette.TextFontFamily, 9, FontStyle.Regular);

        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.FillEllipse(accent, 29, 36, 8, 8);
        graphics.DrawString("SLIDESTUDIO", brandFont, primary, 48, 29);
        graphics.DrawString("ESPACE CRÉATIF", editionFont, muted, Width - 222, 34);
        DrawCenteredText(graphics, "CHOISISSEZ VOTRE PROCHAIN MOUVEMENT", eyebrowFont, AccentColor,
            new Rectangle(0, scene.Top + 12, Width, 25));
        DrawCenteredText(graphics, "Votre espace de création.", titleFont, PrimaryTextColor,
            new Rectangle(15, scene.Top + 45, Width - 30, 58));
        DrawCenteredText(graphics, "Faites défiler les options et placez votre choix au centre.", descriptionFont, MutedTextColor,
            new Rectangle(0, scene.Top + 105, Width, 32));
        DrawCenteredText(graphics, "MOLETTE OU FLÈCHES  ← →  POUR FAIRE TOURNER LE SÉLECTEUR", footerFont, MutedTextColor,
            new Rectangle(0, Height - 52, Width, 36));
    }

    private static void DrawCenteredText(Graphics graphics, string text, Font font, Color color, Rectangle bounds) =>
        TextRenderer.DrawText(graphics, text, font, bounds, color,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);

    private static Bitmap CreateSlideArtwork(bool newPresentation)
    {
        Bitmap artwork = new(200, 140, PixelFormat.Format32bppPArgb);
        using Graphics graphics = Graphics.FromImage(artwork);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.TranslateTransform(15, 15);
        DrawSlideArtwork(graphics, newPresentation);
        return artwork;
    }

    private static void DrawSlideArtwork(Graphics graphics, bool newPresentation)
    {
        using SolidBrush shadow = new(Color.FromArgb(72, 0, 0, 0));
        using SolidBrush edge = new(Color.FromArgb(210, 224, 75, 12));
        using LinearGradientBrush face = new(
            new Rectangle(0, 0, 160, 100),
            Color.FromArgb(248, 49, 56, 76),
            Color.FromArgb(240, 26, 31, 45),
            LinearGradientMode.Vertical);
        using Pen outline = new(Color.FromArgb(155, AccentLightColor), 1.4f);
        Point[] shadowPoints = [new(-8, 12), new(160, 12), new(160, 112), new(-8, 112)];
        Point[] frontPoints = [new(0, 0), new(160, 0), new(160, 100), new(0, 100)];
        Point[] sidePoints = [new(160, 0), new(174, -11), new(174, 89), new(160, 100)];
        Point[] topPoints = [new(0, 0), new(14, -11), new(174, -11), new(160, 0)];
        graphics.FillPolygon(shadow, shadowPoints);
        graphics.FillPolygon(edge, sidePoints);
        graphics.FillPolygon(edge, topPoints);
        graphics.FillPolygon(face, frontPoints);
        graphics.DrawPolygon(outline, frontPoints);
        using SolidBrush marker = new(Color.FromArgb(245, AccentColor));
        using SolidBrush light = new(Color.FromArgb(104, 244, 246, 252));
        using SolidBrush accentGlow = new(Color.FromArgb(44, AccentColor));
        FillRounded(graphics, accentGlow, new Rectangle(10, 10, 140, 80), 12);
        graphics.FillRectangle(marker, 16, 17, 40, 4);
        graphics.FillRectangle(light, 16, 31, 82, 3);
        graphics.FillRectangle(light, 16, 40, 60, 3);
        graphics.FillRectangle(light, 16, 49, 96, 2);
        if (newPresentation)
        {
            using LinearGradientBrush chart = new(new Rectangle(103, 23, 43, 35), Color.FromArgb(225, 255, 129, 55), Color.FromArgb(45, 255, 101, 24), LinearGradientMode.Vertical);
            Point[] chartShape = [new(104, 58), new(110, 47), new(119, 51), new(128, 33), new(137, 40), new(146, 25), new(146, 58)];
            graphics.FillPolygon(chart, chartShape);
            using Pen chartLine = new(Color.FromArgb(238, AccentLightColor), 2);
            graphics.DrawLines(chartLine, [new(104, 58), new(110, 47), new(119, 51), new(128, 33), new(137, 40), new(146, 25)]);
        }
        else
        {
            using SolidBrush folderBack = new(Color.FromArgb(235, AccentColor));
            using SolidBrush folderFace = new(Color.FromArgb(230, 255, 145, 75));
            using Pen folderLine = new(Color.FromArgb(238, AccentLightColor), 2);
            Point[] folder = [new(104, 34), new(117, 34), new(123, 40), new(146, 40), new(146, 59), new(104, 59)];
            graphics.FillPolygon(folderBack, folder);
            graphics.FillPolygon(folderFace, [new(108, 42), new(143, 42), new(140, 55), new(108, 55)]);
            graphics.DrawLines(folderLine, [new(104, 34), new(117, 34), new(123, 40), new(146, 40)]);
        }
    }

    private static Bitmap CreateActionCardArtwork(int width, int height, bool newPresentation)
    {
        Bitmap artwork = new(width, height, PixelFormat.Format32bppPArgb);
        using Graphics graphics = Graphics.FromImage(artwork);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
        Rectangle bounds = new(5, 13, width - 12, height - 25);
        Color baseColor = PanelColor;
        using LinearGradientBrush background = new(
            bounds,
            Color.FromArgb(baseColor.R + 8, baseColor.G + 8, baseColor.B + 10),
            baseColor,
            LinearGradientMode.Vertical);
        using Pen border = new(BorderColor, 1f);
        FillRounded(graphics, background, bounds, 18);
        DrawRounded(graphics, border, bounds, 18);

        GraphicsState state = graphics.Save();
        Rectangle iconArea = new(bounds.Left + 22, bounds.Top + 18, 72, 54);
        graphics.TranslateTransform(iconArea.Left + iconArea.Width / 2f, iconArea.Top + iconArea.Height / 2f);
        graphics.RotateTransform(newPresentation ? -3 : 3);
        DrawMiniPresentation(graphics, newPresentation);
        graphics.Restore(state);

        string eyebrow = newPresentation ? "NOUVEAU PROJET" : "REPRENDRE";
        string title = newPresentation ? "Créer une présentation" : "Ouvrir un projet";
        string description = newPresentation ? "Partez d'une toile vierge." : "Reprenez un projet existant.";
        string action = newPresentation ? "COMMENCER  →" : "CHOISIR UN FICHIER  →";
        using SolidBrush eyebrowBrush = new(AccentLightColor);
        using Font eyebrowFont = new(UiPalette.TextFontFamily, 8, FontStyle.Bold);
        graphics.DrawString(eyebrow, eyebrowFont, eyebrowBrush, bounds.Left + 22, bounds.Top + 87);
        using SolidBrush titleBrush = new(PrimaryTextColor);
        using Font titleFont = new(UiPalette.DisplayFontFamily, 16, FontStyle.Bold);
        graphics.DrawString(title, titleFont, titleBrush, new RectangleF(bounds.Left + 22, bounds.Top + 105, bounds.Width - 42, 33));
        using SolidBrush descriptionBrush = new(MutedTextColor);
        using Font descriptionFont = new(UiPalette.TextFontFamily, 9, FontStyle.Regular);
        graphics.DrawString(description, descriptionFont, descriptionBrush, new RectangleF(bounds.Left + 22, bounds.Top + 139, bounds.Width - 44, 34));
        using SolidBrush actionBrush = new(PrimaryTextColor);
        using Font actionFont = new(UiPalette.TextFontFamily, 9, FontStyle.Bold);
        graphics.DrawString(action, actionFont, actionBrush, bounds.Left + 22, bounds.Bottom - 22);
        return artwork;
    }

    private static void DrawMiniPresentation(Graphics graphics, bool newPresentation)
    {
        using SolidBrush shadow = new(Color.FromArgb(65, 0, 0, 0));
        using SolidBrush side = new(Color.FromArgb(193, 224, 75, 12));
        using SolidBrush face = new(Color.FromArgb(255, 46, 52, 70));
        using Pen outline = new(Color.FromArgb(255, 255, 129, 55), 1);
        graphics.FillRectangle(shadow, -29, -17, 62, 42);
        Point[] depth = [new(24, -23), new(32, -30), new(32, 9), new(24, 17)];
        graphics.FillPolygon(side, depth);
        Rectangle faceBounds = new(-32, -24, 56, 38);
        FillRounded(graphics, face, faceBounds, 5);
        DrawRounded(graphics, outline, faceBounds, 5);
        using SolidBrush accent = new(Color.FromArgb(255, 255, 101, 24));
        using SolidBrush content = new(Color.FromArgb(125, 244, 246, 252));
        graphics.FillRectangle(accent, -24, -16, newPresentation ? 23 : 15, 3);
        graphics.FillRectangle(content, -24, -8, 34, 2);
        graphics.FillRectangle(content, -24, -2, 24, 2);
        if (!newPresentation)
        {
            graphics.FillEllipse(accent, 4, -15, 10, 10);
        }
    }

    private static void DrawFloatingSlide(Graphics graphics, Bitmap artwork, float x, float y, float bob, float scale)
    {
        GraphicsState state = graphics.Save();
        graphics.TranslateTransform(x, y + bob);
        graphics.RotateTransform(scale > 0.9f ? -7 : scale > 0.6f ? 11 : -13);
        graphics.ScaleTransform(scale, scale);
        graphics.DrawImageUnscaled(artwork, -15, -15);
        graphics.Restore(state);
    }

    private void DrawCarouselCard(
        Graphics graphics,
        int card,
        Bitmap artwork,
        float hover,
        bool focused,
        bool pressed)
    {
        float distance = card - _carouselPosition;
        float scale = Math.Max(0.58f, 1f - Math.Abs(distance) * 0.28f);
        float centerX = Width * 0.5f + distance * Math.Min(Width * 0.31f, artwork.Width * 0.83f);
        float centerY = SceneTop + SceneHeight * 0.69f + Math.Abs(distance) * 28;
        float angle = -distance * 9;
        float lift = hover * 7 - (pressed ? 3 : 0);
        GraphicsState state = graphics.Save();
        graphics.TranslateTransform(centerX, centerY - lift);
        graphics.RotateTransform(angle);
        graphics.ScaleTransform(scale, scale);

        Rectangle cardBounds = new(-artwork.Width / 2 + 5, -artwork.Height / 2 + 13,
            artwork.Width - 12, artwork.Height - 25);
        Rectangle shadowBounds = new(cardBounds.X + 5, cardBounds.Y + 12, cardBounds.Width, cardBounds.Height);
        using SolidBrush shadow = new(Color.FromArgb((int)(42 + hover * 36), 0, 0, 0));
        FillRounded(graphics, shadow, shadowBounds, 18);
        if (focused || hover > 0.1f)
        {
            using Pen selectedGlow = new(Color.FromArgb(focused ? 72 : 30, 255, 101, 24), 8);
            DrawRounded(graphics, selectedGlow, Rectangle.Inflate(cardBounds, 2, 2), 20);
        }
        graphics.DrawImageUnscaled(artwork, -artwork.Width / 2, -artwork.Height / 2);
        using Pen cardOutline = new(
            Blend(BorderColor, Color.FromArgb(255, 145, 84), focused ? 1 : hover * 0.75f),
            focused ? 1.8f : 1f);
        DrawRounded(graphics, cardOutline, cardBounds, 18);
        if (focused)
        {
            Rectangle focusBounds = Rectangle.Inflate(cardBounds, -5, -5);
            using Pen focusPen = new(Color.FromArgb(150, 255, 129, 55), 1) { DashPattern = [3, 2] };
            DrawRounded(graphics, focusPen, focusBounds, 14);
        }
        graphics.Restore(state);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _motionTimer.Dispose();
            DisposeArtwork();
        }
        base.Dispose(disposing);
    }

    private static void DrawGlow(Graphics graphics, PointF center, float radius, float opacity)
    {
        using GraphicsPath path = new();
        path.AddEllipse(center.X - radius, center.Y - radius, radius * 2, radius * 2);
        using PathGradientBrush glow = new(path)
        {
            CenterColor = Color.FromArgb((int)(255 * opacity), AccentColor),
            SurroundColors = [Color.FromArgb(0, AccentColor)]
        };
        graphics.FillPath(glow, path);
    }

    private static void DrawHeroLandscape(Graphics graphics, Rectangle scene)
    {
        GraphicsState state = graphics.Save();
        using GraphicsPath clip = RoundedPath(scene, 30);
        graphics.SetClip(clip);
        using LinearGradientBrush horizon = new(
            new Rectangle(scene.Left, scene.Top + scene.Height / 2, scene.Width, scene.Height / 2),
            Color.FromArgb(0, 255, 101, 24),
            Color.FromArgb(30, 255, 101, 24),
            LinearGradientMode.Vertical);
        graphics.FillRectangle(horizon, scene.Left, scene.Top + scene.Height / 2, scene.Width, scene.Height / 2);
        using Pen grid = new(Color.FromArgb(14, 206, 214, 231), 1);
        for (int line = 1; line <= 4; line++)
        {
            int y = scene.Top + scene.Height * (6 + line) / 11;
            graphics.DrawLine(grid, scene.Left, y, scene.Right, y);
        }
        for (int line = 1; line <= 12; line++)
        {
            int x = scene.Left + scene.Width * line / 13;
            graphics.DrawLine(grid, x, scene.Top + scene.Height * 6 / 11, x, scene.Bottom);
        }
        graphics.Restore(state);
    }

    private static void DrawFloatingSlide(Graphics graphics, float x, float y, float bob, float angle, float scale)
    {
        GraphicsState state = graphics.Save();
        graphics.TranslateTransform(x, y + bob);
        graphics.RotateTransform(angle);
        graphics.ScaleTransform(scale, scale);

        using SolidBrush shadow = new(Color.FromArgb(72, 0, 0, 0));
        using SolidBrush edge = new(Color.FromArgb(210, 224, 75, 12));
        using LinearGradientBrush face = new(
            new Rectangle(0, 0, 160, 100),
            Color.FromArgb(248, 49, 56, 76),
            Color.FromArgb(240, 26, 31, 45),
            LinearGradientMode.Vertical);
        using Pen outline = new(Color.FromArgb(155, AccentLightColor), 1.4f);
        Point[] shadowPoints = [new(-8, 12), new(160, 12), new(160, 112), new(-8, 112)];
        Point[] frontPoints = [new(0, 0), new(160, 0), new(160, 100), new(0, 100)];
        Point[] sidePoints = [new(160, 0), new(174, -11), new(174, 89), new(160, 100)];
        Point[] topPoints = [new(0, 0), new(14, -11), new(174, -11), new(160, 0)];
        graphics.FillPolygon(shadow, shadowPoints);
        graphics.FillPolygon(edge, sidePoints);
        graphics.FillPolygon(edge, topPoints);
        graphics.FillPolygon(face, frontPoints);
        graphics.DrawPolygon(outline, frontPoints);
        using SolidBrush marker = new(Color.FromArgb(245, AccentColor));
        using SolidBrush light = new(Color.FromArgb(104, 244, 246, 252));
        using SolidBrush accentGlow = new(Color.FromArgb(44, AccentColor));
        FillRounded(graphics, accentGlow, new Rectangle(10, 10, 140, 80), 12);
        graphics.FillRectangle(marker, 16, 17, 40, 4);
        graphics.FillRectangle(light, 16, 31, 82, 3);
        graphics.FillRectangle(light, 16, 40, 60, 3);
        graphics.FillRectangle(light, 16, 49, 96, 2);
        using LinearGradientBrush chart = new(new Rectangle(103, 23, 43, 35), Color.FromArgb(225, 255, 129, 55), Color.FromArgb(45, 255, 101, 24), LinearGradientMode.Vertical);
        Point[] chartShape = [new(104, 58), new(110, 47), new(119, 51), new(128, 33), new(137, 40), new(146, 25), new(146, 58)];
        graphics.FillPolygon(chart, chartShape);
        using Pen chartLine = new(Color.FromArgb(238, AccentLightColor), 2);
        graphics.DrawLines(chartLine, [new(104, 58), new(110, 47), new(119, 51), new(128, 33), new(137, 40), new(146, 25)]);
        graphics.Restore(state);
    }

    private sealed class ActionCard : Control
    {
        private readonly System.Windows.Forms.Timer _animationTimer = new() { Interval = 16 };
        private readonly string _description;
        private readonly string _eyebrow;
        private readonly StartupAction _action;
        private float _hover;
        private float _hoverTarget;
        private bool _pressed;

        public event EventHandler<StartupAction>? ActionRequested;

        public ActionCard(string title, string description, string eyebrow, StartupAction action)
        {
            Text = title;
            _description = description;
            _eyebrow = eyebrow;
            _action = action;
            Size = new Size(350, 226);
            SetStyle(
                ControlStyles.UserPaint
                    | ControlStyles.AllPaintingInWmPaint
                    | ControlStyles.OptimizedDoubleBuffer
                    | ControlStyles.ResizeRedraw,
                true);
            BackColor = PanelColor;
            ForeColor = PrimaryTextColor;
            Cursor = Cursors.Hand;
            TabStop = true;
            AccessibleRole = AccessibleRole.PushButton;
            AccessibleName = title;
            _animationTimer.Tick += (_, _) =>
            {
                _hover += (_hoverTarget - _hover) * 0.22f;
                if (Math.Abs(_hoverTarget - _hover) < 0.01f)
                {
                    _hover = _hoverTarget;
                    _animationTimer.Stop();
                }
                Invalidate();
            };
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            AnimateTo(1);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            AnimateTo(0);
            _pressed = false;
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button == MouseButtons.Left)
            {
                _pressed = true;
                Invalidate();
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            bool activate = _pressed && ClientRectangle.Contains(e.Location);
            _pressed = false;
            base.OnMouseUp(e);
            Invalidate();
            if (activate)
            {
                ActionRequested?.Invoke(this, _action);
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.KeyCode is Keys.Enter or Keys.Space)
            {
                ActionRequested?.Invoke(this, _action);
                e.Handled = true;
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            float lift = _hover * 7 - (_pressed ? 3 : 0);
            Rectangle cardBounds = new(5, 13 - (int)lift, Width - 12, Height - 25);
            Rectangle shadowBounds = new(cardBounds.X + 4, cardBounds.Y + 10, cardBounds.Width, cardBounds.Height);
            using SolidBrush shadow = new(Color.FromArgb((int)(38 + _hover * 24), 0, 0, 0));
            FillRounded(e.Graphics, shadow, shadowBounds, 18);

            Color baseColor = Blend(PanelColor, UiPalette.CardHover, _hover);
            using LinearGradientBrush background = new(
                cardBounds,
                Color.FromArgb(Math.Min(255, baseColor.R + 8), Math.Min(255, baseColor.G + 8), Math.Min(255, baseColor.B + 10)),
                baseColor,
                LinearGradientMode.Vertical);
            using Pen border = new(Blend(BorderColor, Color.FromArgb(255, 145, 84), _hover), _hover > 0.1f ? 1.5f : 1f);
            FillRounded(e.Graphics, background, cardBounds, 18);
            DrawRounded(e.Graphics, border, cardBounds, 18);

            GraphicsState state = e.Graphics.Save();
            Rectangle iconArea = new(cardBounds.Left + 22, cardBounds.Top + 18, 72, 54);
            e.Graphics.TranslateTransform(iconArea.Left + iconArea.Width / 2f, iconArea.Top + iconArea.Height / 2f - _hover * 2);
            e.Graphics.RotateTransform((_action == StartupAction.NewPresentation ? -1 : 1) * (3 - _hover * 2));
            e.Graphics.ScaleTransform(1 + _hover * 0.035f, 1 + _hover * 0.025f);
            DrawMiniPresentation(e.Graphics, _action == StartupAction.NewPresentation);
            e.Graphics.Restore(state);

            using SolidBrush eyebrowBrush = new(AccentLightColor);
            using Font eyebrowFont = new(UiPalette.TextFontFamily, 8, FontStyle.Bold);
            e.Graphics.DrawString(_eyebrow, eyebrowFont, eyebrowBrush, cardBounds.Left + 22, cardBounds.Top + 87);
            using Font titleFont = new(UiPalette.DisplayFontFamily, 16, FontStyle.Bold);
            using SolidBrush titleBrush = new(PrimaryTextColor);
            e.Graphics.DrawString(Text, titleFont, titleBrush, new RectangleF(cardBounds.Left + 22, cardBounds.Top + 105, cardBounds.Width - 42, 33));
            using Font descriptionFont = new(UiPalette.TextFontFamily, 9, FontStyle.Regular);
            using SolidBrush descriptionBrush = new(MutedTextColor);
            e.Graphics.DrawString(_description, descriptionFont, descriptionBrush, new RectangleF(cardBounds.Left + 22, cardBounds.Top + 139, cardBounds.Width - 44, 34));
            using Font actionFont = new(UiPalette.TextFontFamily, 9, FontStyle.Bold);
            using SolidBrush actionBrush = new(PrimaryTextColor);
            string actionLabel = _action == StartupAction.NewPresentation ? "COMMENCER  →" : "CHOISIR UN FICHIER  →";
            e.Graphics.DrawString(actionLabel, actionFont, actionBrush, cardBounds.Left + 22, cardBounds.Bottom - 22);
            if (Focused)
            {
                Rectangle focus = Rectangle.Inflate(cardBounds, -5, -5);
                using Pen focusPen = new(Color.FromArgb(180, 255, 129, 55), 1) { DashPattern = [3, 2] };
                DrawRounded(e.Graphics, focusPen, focus, 14);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _animationTimer.Dispose();
            }
            base.Dispose(disposing);
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            base.OnPaintBackground(e);
        }

        private void AnimateTo(float target)
        {
            _hoverTarget = target;
            _animationTimer.Start();
        }

        private static void DrawMiniPresentation(Graphics graphics, bool newPresentation)
        {
            using SolidBrush shadow = new(Color.FromArgb(65, 0, 0, 0));
            using SolidBrush side = new(Color.FromArgb(193, 224, 75, 12));
            using SolidBrush face = new(Color.FromArgb(255, 46, 52, 70));
            using Pen outline = new(Color.FromArgb(255, 255, 129, 55), 1);
            graphics.FillRectangle(shadow, -29, -17, 62, 42);
            Point[] depth = [new(24, -23), new(32, -30), new(32, 9), new(24, 17)];
            graphics.FillPolygon(side, depth);
            Rectangle faceBounds = new(-32, -24, 56, 38);
            FillRounded(graphics, face, faceBounds, 5);
            DrawRounded(graphics, outline, faceBounds, 5);
            using SolidBrush accent = new(Color.FromArgb(255, 255, 101, 24));
            using SolidBrush content = new(Color.FromArgb(125, 244, 246, 252));
            graphics.FillRectangle(accent, -24, -16, newPresentation ? 23 : 15, 3);
            graphics.FillRectangle(content, -24, -8, 34, 2);
            graphics.FillRectangle(content, -24, -2, 24, 2);
            if (!newPresentation)
            {
                graphics.FillEllipse(accent, 4, -15, 10, 10);
            }
        }
    }

    private static Color Blend(Color from, Color to, float amount) =>
        Color.FromArgb(
            (int)(from.R + (to.R - from.R) * amount),
            (int)(from.G + (to.G - from.G) * amount),
            (int)(from.B + (to.B - from.B) * amount));

    private static void FillRounded(Graphics graphics, Brush brush, Rectangle bounds, int radius)
    {
        using GraphicsPath path = RoundedPath(bounds, radius);
        graphics.FillPath(brush, path);
    }

    private static void DrawRounded(Graphics graphics, Pen pen, Rectangle bounds, int radius)
    {
        using GraphicsPath path = RoundedPath(bounds, radius);
        graphics.DrawPath(pen, path);
    }

    private static GraphicsPath RoundedPath(Rectangle bounds, int radius)
    {
        GraphicsPath path = new();
        int diameter = Math.Min(radius * 2, Math.Min(bounds.Width, bounds.Height));
        Rectangle arc = new(bounds.Left, bounds.Top, diameter, diameter);
        path.AddArc(arc, 180, 90);
        arc.X = bounds.Right - diameter;
        path.AddArc(arc, 270, 90);
        arc.Y = bounds.Bottom - diameter;
        path.AddArc(arc, 0, 90);
        arc.X = bounds.Left;
        path.AddArc(arc, 90, 90);
        path.CloseFigure();
        return path;
    }
}

internal sealed class LoadingScreen : Control
{
    private static readonly Color WindowColor = UiPalette.Window;
    private static readonly Color PrimaryTextColor = UiPalette.PrimaryText;
    private static readonly Color MutedTextColor = UiPalette.MutedText;
    private static readonly Color AccentColor = UiPalette.Accent;
    private static readonly Color AccentLightColor = UiPalette.AccentPressed;
    private readonly System.Windows.Forms.Timer _timer = new() { Interval = 30 };
    private float _progress;
    private float _rotation;
    private int _lastAnnouncedProgress;

    public event EventHandler? Completed;

    public LoadingScreen()
    {
        Dock = DockStyle.Fill;
        BackColor = WindowColor;
        TabStop = true;
        AccessibleRole = AccessibleRole.ProgressBar;
        AccessibleName = "Préparation de votre espace";
        AccessibleDescription = "En attente.";
        SetStyle(
            ControlStyles.UserPaint
                | ControlStyles.AllPaintingInWmPaint
                | ControlStyles.OptimizedDoubleBuffer
                | ControlStyles.ResizeRedraw,
            true);
        _timer.Tick += (_, _) =>
        {
            _progress = Math.Min(1, _progress + 0.025f);
            _rotation = (_rotation + 9f) % 360;
            AnnounceProgress();
            Invalidate();
            if (_progress >= 1)
            {
                _timer.Stop();
                Completed?.Invoke(this, EventArgs.Empty);
            }
        };
    }

    public void Start()
    {
        _progress = 0;
        _rotation = 0;
        _lastAnnouncedProgress = 0;
        UpdateAccessibleDescription("Chargement en cours : 0 %. Veuillez patienter.");
        _timer.Start();
        Invalidate();
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        using LinearGradientBrush background = new(ClientRectangle, WindowColor, Color.FromArgb(21, 25, 38), LinearGradientMode.Vertical);
        e.Graphics.FillRectangle(background, ClientRectangle);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        Graphics graphics = e.Graphics;
        graphics.SmoothingMode = SmoothingMode.AntiAlias;

        float centerX = Width / 2f;
        float centerY = Height / 2f - 76;
        DrawLoadingObject(graphics, centerX, centerY);

        using Font titleFont = new(UiPalette.DisplayFontFamily, 17, FontStyle.Bold);
        using SolidBrush titleBrush = new(PrimaryTextColor);
        TextRenderer.DrawText(graphics, "Préparation de votre espace", titleFont,
            new Rectangle(0, (int)centerY + 68, Width, 34), PrimaryTextColor,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        using Font subtitleFont = new(UiPalette.TextFontFamily, 9.5f, FontStyle.Regular);
        TextRenderer.DrawText(graphics, "Encore un instant…", subtitleFont,
            new Rectangle(0, (int)centerY + 105, Width, 25), MutedTextColor,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);

        int progressWidth = Math.Min(320, Width - 80);
        Rectangle progressTrack = new((Width - progressWidth) / 2, (int)centerY + 153, progressWidth, 4);
        using SolidBrush trackBrush = new(Color.FromArgb(54, 64, 83));
        using SolidBrush progressBrush = new(AccentColor);
        FillRounded(graphics, trackBrush, progressTrack, 2);
        Rectangle progress = new(progressTrack.X, progressTrack.Y, (int)(progressTrack.Width * _progress), progressTrack.Height);
        FillRounded(graphics, progressBrush, progress, 2);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _timer.Dispose();
        }
        base.Dispose(disposing);
    }

    private void AnnounceProgress()
    {
        int percentage = (int)(_progress * 100);
        if (percentage >= 100)
        {
            UpdateAccessibleDescription("Chargement terminé.");
            return;
        }

        if (percentage >= _lastAnnouncedProgress + 25)
        {
            _lastAnnouncedProgress = percentage;
            UpdateAccessibleDescription($"Chargement en cours : {percentage} %.");
        }
    }

    private void UpdateAccessibleDescription(string description)
    {
        if (AccessibleDescription == description)
        {
            return;
        }

        AccessibleDescription = description;
        if (IsHandleCreated)
        {
            AccessibilityNotifyClients(AccessibleEvents.DescriptionChange, -1);
        }
    }

    private void DrawLoadingObject(Graphics graphics, float centerX, float centerY)
    {
        float phase = _rotation * MathF.PI / 180;
        float bob = MathF.Sin(phase * 2) * 9;
        float depth = 14 + (MathF.Sin(phase) + 1) * 5;
        float horizontalScale = 0.38f + MathF.Abs(MathF.Cos(phase)) * 0.62f;
        float shadowScale = 1 - bob / 45;
        RectangleF shadowBounds = new(centerX - 92 * shadowScale, centerY + 107, 184 * shadowScale, 22);
        using GraphicsPath shadowPath = new();
        shadowPath.AddEllipse(shadowBounds);
        using PathGradientBrush shadow = new(shadowPath)
        {
            CenterColor = Color.FromArgb(75, 0, 0, 0),
            SurroundColors = [Color.FromArgb(0, 0, 0, 0)]
        };
        graphics.FillPath(shadow, shadowPath);

        GraphicsState state = graphics.Save();
        graphics.TranslateTransform(centerX, centerY + bob);
        graphics.RotateTransform(MathF.Sin(phase * 0.7f) * 8);
        graphics.ScaleTransform(horizontalScale, 0.92f);

        using SolidBrush side = new(Color.FromArgb(255, 206, 67, 13));
        using SolidBrush top = new(Color.FromArgb(255, 255, 151, 70));
        using LinearGradientBrush face = new(
            new Rectangle(-72, -49, 144, 98),
            Color.FromArgb(255, 255, 139, 62),
            Color.FromArgb(255, 239, 79, 14),
            LinearGradientMode.ForwardDiagonal);
        using Pen outline = new(Color.FromArgb(255, 255, 182, 121), 1.5f);
        Point[] back = [new(-65, -38), new(-49, -52), new(65, -52), new(65, 38), new(49, 52), new(-65, 52)];
        using SolidBrush backBrush = new(Color.FromArgb(100, 0, 0, 0));
        graphics.FillPolygon(backBrush, back);
        PointF[] sideFace = [new(72, -49), new(72 + depth, -49 - depth * 0.7f), new(72 + depth, 49 - depth * 0.7f), new(72, 49)];
        PointF[] topFace = [new(-72, -49), new(-72 + depth, -49 - depth * 0.7f), new(72 + depth, -49 - depth * 0.7f), new(72, -49)];
        graphics.FillPolygon(side, sideFace);
        graphics.FillPolygon(top, topFace);
        FillRounded(graphics, face, new Rectangle(-72, -49, 144, 98), 20);
        DrawRounded(graphics, outline, new Rectangle(-72, -49, 144, 98), 20);

        using SolidBrush markShadow = new(Color.FromArgb(35, 122, 34, 3));
        FillRounded(graphics, markShadow, new Rectangle(-36, -29, 72, 58), 14);
        using Font markFont = new(UiPalette.DisplayFontFamily, 40, FontStyle.Bold);
        TextRenderer.DrawText(graphics, "S", markFont, new Rectangle(-34, -37, 68, 72), Color.White,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        graphics.Restore(state);

        DrawOrbitingTile(graphics, centerX, centerY, phase, 0);
        DrawOrbitingTile(graphics, centerX, centerY, phase, 1);
        DrawOrbitingTile(graphics, centerX, centerY, phase, 2);
    }

    private static void DrawOrbitingTile(Graphics graphics, float centerX, float centerY, float phase, int index)
    {
        float angle = phase + index * (MathF.PI * 2 / 3);
        float x = centerX + MathF.Cos(angle) * 126;
        float y = centerY + MathF.Sin(angle) * 70;
        float size = index == 1 ? 7 : 5;
        using SolidBrush glow = new(Color.FromArgb(index == 1 ? 220 : 155, AccentColor));
        using SolidBrush core = new(Color.FromArgb(245, AccentLightColor));
        graphics.FillEllipse(glow, new RectangleF(x - size, y - size, size * 2, size * 2));
        graphics.FillEllipse(core, new RectangleF(x - size * 0.45f, y - size * 0.45f, size * 0.9f, size * 0.9f));
    }

    private static void FillRounded(Graphics graphics, Brush brush, Rectangle bounds, int radius)
    {
        using GraphicsPath path = new();
        int diameter = radius * 2;
        Rectangle arc = new(bounds.Left, bounds.Top, diameter, diameter);
        path.AddArc(arc, 180, 90);
        arc.X = bounds.Right - diameter;
        path.AddArc(arc, 270, 90);
        arc.Y = bounds.Bottom - diameter;
        path.AddArc(arc, 0, 90);
        arc.X = bounds.Left;
        path.AddArc(arc, 90, 90);
        path.CloseFigure();
        graphics.FillPath(brush, path);
    }

    private static void DrawRounded(Graphics graphics, Pen pen, Rectangle bounds, int radius)
    {
        using GraphicsPath path = new();
        int diameter = radius * 2;
        Rectangle arc = new(bounds.Left, bounds.Top, diameter, diameter);
        path.AddArc(arc, 180, 90);
        arc.X = bounds.Right - diameter;
        path.AddArc(arc, 270, 90);
        arc.Y = bounds.Bottom - diameter;
        path.AddArc(arc, 0, 90);
        arc.X = bounds.Left;
        path.AddArc(arc, 90, 90);
        path.CloseFigure();
        graphics.DrawPath(pen, path);
    }
}
