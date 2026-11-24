
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace SlideStudio.UI;

public class SlideshowForm : Form
{
    private readonly List<Image> _slides;
    private readonly PictureBox _pictureBox;
    private int _currentIndex;
    private bool _blackScreen;

    public SlideshowForm(IEnumerable<Image> slides)
    {
        _slides = new List<Image>(slides);

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

        if (_slides.Count > 0)
            ShowSlide();
    }

    private void ShowSlide()
    {
        if (_slides.Count == 0)
            return;

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
            return;

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
