
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace SlideStudio.UI;

/// <summary>Affiche une séquence d'images en plein écran avec navigation au clavier.</summary>
public class SlideshowForm : Form
{
    private readonly List<Image> _slides;
    private readonly PictureBox _pictureBox;
    private int _currentIndex;
    private bool _blackScreen;

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

        Text = "SlideStudio - Diaporama";
        WindowState = FormWindowState.Maximized;
        FormBorderStyle = FormBorderStyle.None;
        BackColor = Color.Black;
        KeyPreview = true;

        _pictureBox = new PictureBox
        {
            Dock = DockStyle.Fill,
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = Color.Black
        };

        Controls.Add(_pictureBox);

        KeyDown += SlideshowForm_KeyDown;

        ShowSlide();
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _pictureBox.Image = null;
            foreach (Image slide in _slides)
            {
                slide.Dispose();
            }
        }

        base.Dispose(disposing);
    }

    private void ShowSlide()
    {
        if (_slides.Count == 0)
        {
            return;
        }

        _blackScreen = false;
        _pictureBox.Image = _slides[_currentIndex];
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

        if (e.KeyCode == Keys.Right || e.KeyCode == Keys.Space)
        {
            if (_currentIndex < _slides.Count - 1)
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
