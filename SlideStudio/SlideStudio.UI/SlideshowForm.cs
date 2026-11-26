
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using SlideStudio.Core;
using SlideStudio.Core.Model;
using SlideStudio.UI.Canvas;

namespace SlideStudio.UI;

/// <summary>
/// Affiche une présentation en plein écran avec navigation au clavier.
/// </summary>
public class SlideshowForm : Form
{
    private readonly List<Image> _slides;

    private readonly Slide[]? _presentationSlides;

    private readonly SlideRenderer? _renderer;

    private readonly PictureBox _pictureBox;

    private readonly Label _slideCounterLabel;

    private int _currentIndex;

    private bool _blackScreen;

    private Bitmap? _renderedSlide;

    /// <summary>
    /// Crée un diaporama à partir d'une collection d'images.
    /// </summary>
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

            if (_slides.Count == 0)
            {
                throw new ArgumentException(
                    "Le diaporama doit contenir au moins une image.",
                    nameof(slides));
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

        _pictureBox = CreatePictureBox();
        _slideCounterLabel = CreateSlideCounter();

        InitializeWindow();

        Controls.Add(_pictureBox);
        Controls.Add(_slideCounterLabel);

        _slideCounterLabel.BringToFront();

        ShowSlide();
    }

    /// <summary>
    /// Crée un diaporama depuis une présentation et ses médias.
    /// </summary>
    public SlideshowForm(
        IReadOnlyList<Slide> slides,
        MediaStore mediaStore)
    {
        ArgumentNullException.ThrowIfNull(slides);
        ArgumentNullException.ThrowIfNull(mediaStore);

        if (slides.Count == 0)
        {
            throw new ArgumentException(
                "Le diaporama doit contenir au moins une diapositive.",
                nameof(slides));
        }

        _slides = [];
        _presentationSlides = slides.ToArray();

        _renderer = new SlideRenderer();
        _renderer.SetMediaStore(mediaStore);

        _pictureBox = CreatePictureBox();
        _slideCounterLabel = CreateSlideCounter();

        InitializeWindow();

        Controls.Add(_pictureBox);
        Controls.Add(_slideCounterLabel);

        _slideCounterLabel.BringToFront();

        try
        {
            ShowSlide();
        }
        catch
        {
            _renderer.Dispose();
            _pictureBox.Dispose();
            _slideCounterLabel.Dispose();
            throw;
        }
    }

    /// <summary>
    /// Crée la zone d'affichage des diapositives.
    /// </summary>
    private static PictureBox CreatePictureBox()
    {
        return new PictureBox
        {
            Dock = DockStyle.Fill,
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = Color.Black
        };
    }

    /// <summary>
    /// Crée le compteur de diapositives.
    /// </summary>
    private static Label CreateSlideCounter()
    {
        return new Label
        {
            AutoSize = true,
            ForeColor = Color.White,
            BackColor = Color.FromArgb(45, 45, 45),
            Font = new Font("Segoe UI", 12, FontStyle.Bold),
            Padding = new Padding(12, 8, 12, 8),
            Anchor = AnchorStyles.Bottom | AnchorStyles.Right
        };
    }

    /// <summary>
    /// Initialise les paramètres de la fenêtre.
    /// </summary>
    private void InitializeWindow()
    {
        Text = "SlideStudio - Diaporama";
        WindowState = FormWindowState.Maximized;
        FormBorderStyle = FormBorderStyle.None;
        BackColor = Color.Black;
        KeyPreview = true;

        KeyDown += SlideshowForm_KeyDown;
        Resize += SlideshowForm_Resize;
    }

    /// <summary>
    /// Libère les ressources utilisées par le diaporama.
    /// </summary>
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

            _slideCounterLabel.Dispose();
        }

        base.Dispose(disposing);
    }

    /// <summary>
    /// Affiche la diapositive actuellement sélectionnée.
    /// </summary>
    private void ShowSlide()
    {
        if (_presentationSlides is not null && _renderer is not null)
        {
            Bitmap renderedSlide = _renderer.Render(
                _presentationSlides[_currentIndex],
                new Size(
                    (int)Presentation.SlideWidth,
                    (int)Presentation.SlideHeight));

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
        _slideCounterLabel.Visible = true;

        int totalSlides =
            _presentationSlides?.Length ?? _slides.Count;

        _slideCounterLabel.Text =
            $"Diapositive {_currentIndex + 1} / {totalSlides}";

        PositionSlideCounter();
        _slideCounterLabel.BringToFront();
    }

    /// <summary>
    /// Positionne le compteur en bas à droite.
    /// </summary>
    private void PositionSlideCounter()
    {
        int x = Math.Max(
            0,
            ClientSize.Width - _slideCounterLabel.Width - 25);

        int y = Math.Max(
            0,
            ClientSize.Height - _slideCounterLabel.Height - 25);

        _slideCounterLabel.Location = new Point(x, y);
    }

    /// <summary>
    /// Actualise la position du compteur au redimensionnement.
    /// </summary>
    private void SlideshowForm_Resize(object? sender, EventArgs e)
    {
        PositionSlideCounter();
    }

    /// <summary>
    /// Affiche les raccourcis clavier disponibles.
    /// </summary>
    private void ShowKeyboardHelp()
    {
        string helpMessage =
            "RACCOURCIS DU DIAPORAMA\n\n" +
            "Flèche droite : diapositive suivante\n" +
            "Espace : diapositive suivante\n" +
            "Flèche gauche : diapositive précédente\n" +
            "B : activer ou désactiver l'écran noir\n" +
            "Échap : quitter la présentation\n" +
            "F1 : afficher cette aide";

        MessageBox.Show(
            this,
            helpMessage,
            "Aide - SlideStudio",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }

    /// <summary>
    /// Gère les commandes clavier du diaporama.
    /// </summary>
    private void SlideshowForm_KeyDown(
        object? sender,
        KeyEventArgs e)
    {
        SlideshowCommand command =
            SlideshowKeyboard.GetCommand(e.KeyCode);

        switch (command)
        {
            case SlideshowCommand.Exit:
                Close();
                break;

            case SlideshowCommand.ToggleBlackScreen:
                _blackScreen = !_blackScreen;
                _pictureBox.Visible = !_blackScreen;
                _slideCounterLabel.Visible = !_blackScreen;
                break;

            case SlideshowCommand.Next:
                if (!_blackScreen)
                {
                    int totalSlides =
                        _presentationSlides?.Length ?? _slides.Count;

                    if (_currentIndex < totalSlides - 1)
                    {
                        _currentIndex++;
                        ShowSlide();
                    }
                }
                break;

            case SlideshowCommand.Previous:
                if (!_blackScreen && _currentIndex > 0)
                {
                    _currentIndex--;
                    ShowSlide();
                }
                break;

            case SlideshowCommand.ShowHelp:
                ShowKeyboardHelp();
                break;

            case SlideshowCommand.None:
            default:
                break;
        }

        if (command != SlideshowCommand.None)
        {
            e.Handled = true;
            e.SuppressKeyPress = true;
        }
    }
}
