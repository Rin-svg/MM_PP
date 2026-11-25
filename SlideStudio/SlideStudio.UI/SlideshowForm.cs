
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using SlideStudio.Core;
using SlideStudio.Core.Model;
using SlideStudio.UI.Canvas;

namespace SlideStudio.UI;

/// <summary>Affiche une séquence d'images en plein écran avec navigation au clavier.</summary>
public class SlideshowForm : Form
{
    private readonly List<Image> _slides;
    private readonly Slide[]? _presentationSlides;
    private readonly SlideRenderer? _renderer;
    private readonly PictureBox _pictureBox;
    private int _currentIndex;
    private bool _blackScreen;
    private Bitmap? _renderedSlide;

    /// <summary>Crée un diaporama à partir d'images, qu'il copie et libère avec la fenêtre.</summary>
    public SlideshowForm(IEnumerable<Image> slides)
    {
        ArgumentNullException.ThrowIfNull(slides);
        _slides = [];
        try
        {
            foreach (Image slide in slides)
            {
                ArgumentNullException.ThrowIfNull(slide);
                _slides.Add(new Bitmap(slide));
            }
        }
        catch
        {
            foreach (Image slide in _slides)
            {
                slide.Dispose();
            }

            throw;
        }

        if (_slides.Count == 0)
        {
            throw new ArgumentException("Le diaporama doit contenir au moins une image.", nameof(slides));
        }

        _pictureBox = CreatePictureBox();
        InitializeWindow();
        Controls.Add(_pictureBox);
        ShowSlide();
    }

    /// <summary>Crée un diaporama directement depuis une présentation et ses médias embarqués.</summary>
    public SlideshowForm(IReadOnlyList<Slide> slides, MediaStore mediaStore)
    {
        ArgumentNullException.ThrowIfNull(slides);
        ArgumentNullException.ThrowIfNull(mediaStore);
        if (slides.Count == 0)
        {
            throw new ArgumentException("Le diaporama doit contenir au moins une diapositive.", nameof(slides));
        }

        _slides = [];
        _presentationSlides = slides.ToArray();
        _renderer = new SlideRenderer();
        _renderer.SetMediaStore(mediaStore);
        _pictureBox = CreatePictureBox();
        InitializeWindow();
        Controls.Add(_pictureBox);
        try
        {
            ShowSlide();
        }
        catch
        {
            _renderer.Dispose();
            _pictureBox.Dispose();
            throw;
        }
    }

    private static PictureBox CreatePictureBox() => new()
    {
        Dock = DockStyle.Fill,
        SizeMode = PictureBoxSizeMode.Zoom,
        BackColor = Color.Black
    };

    private void InitializeWindow()
    {
        Text = "SlideStudio - Diaporama";
        WindowState = FormWindowState.Maximized;
        FormBorderStyle = FormBorderStyle.None;
        BackColor = Color.Black;
        KeyPreview = true;
        KeyDown += SlideshowForm_KeyDown;
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _pictureBox.Image = null;
            _renderedSlide?.Dispose();
            _renderer?.Dispose();
            foreach (Image slide in _slides)
            {
                slide.Dispose();
            }
        }

        base.Dispose(disposing);
    }

    private void ShowSlide()
    {
        if (_presentationSlides is not null && _renderer is not null)
        {
            Bitmap renderedSlide = _renderer.Render(
                _presentationSlides[_currentIndex],
                new Size((int)Presentation.SlideWidth, (int)Presentation.SlideHeight));
            Bitmap? previousSlide = _renderedSlide;
            _renderedSlide = renderedSlide;
            _pictureBox.Image = renderedSlide;
            previousSlide?.Dispose();
        }
        else if (_slides.Count > 0)
        {
            _pictureBox.Image = _slides[_currentIndex];
        }

        _blackScreen = false;
        _pictureBox.Visible = true;
    }

    private void SlideshowForm_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Escape)
        {
            Close();
            return;
        }

        if (e.KeyCode == Keys.B)
        {
            _blackScreen = !_blackScreen;
            _pictureBox.Visible = !_blackScreen;
            return;
        }

        if (_blackScreen)
        {
            return;
        }

        int slideCount = _presentationSlides?.Length ?? _slides.Count;
        if (e.KeyCode == Keys.Right || e.KeyCode == Keys.Space)
        {
            if (_currentIndex < slideCount - 1)
            {
                _currentIndex++;
                ShowSlide();
            }
        }
        else if (e.KeyCode == Keys.Left)
        {
            if (_currentIndex > 0)
            {
                _currentIndex--;
                ShowSlide();
            }
        }
    }
}
