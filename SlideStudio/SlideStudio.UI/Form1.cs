namespace SlideStudio.UI;

/// <summary>Fenêtre principale de SlideStudio.</summary>
public partial class Form1 : Form
{
    private readonly ToolStripButton _startSlideshowButton;

    /// <summary>Crée la fenêtre principale.</summary>
    public Form1()
    {
        InitializeComponent();

        Text = "SlideStudio";
        ToolStrip toolStrip = new()
        {
            Dock = DockStyle.Top,
            GripStyle = ToolStripGripStyle.Hidden
        };
        _startSlideshowButton = new ToolStripButton("Diaporama depuis des images…");
        _startSlideshowButton.Click += StartSlideshowButton_Click;
        toolStrip.Items.Add(_startSlideshowButton);
        Controls.Add(toolStrip);
        toolStrip.BringToFront();
    }

    private void StartSlideshowButton_Click(object? sender, EventArgs e)
    {
        using OpenFileDialog dialog = new()
        {
            Title = "Choisir les images du diaporama",
            Filter = "Images|*.png;*.jpg;*.jpeg;*.bmp;*.gif|Tous les fichiers|*.*",
            Multiselect = true,
            CheckFileExists = true
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        List<Image> images = [];
        try
        {
            foreach (string fileName in dialog.FileNames)
            {
                using Image source = Image.FromFile(fileName);
                images.Add(new Bitmap(source));
            }

            using SlideshowForm slideshow = new(images);
            slideshow.ShowDialog(this);
        }
        catch (Exception exception) when (
            exception is IOException
                or UnauthorizedAccessException
                or ArgumentException
                or OutOfMemoryException)
        {
            MessageBox.Show(
                this,
                $"Impossible de charger les images du diaporama : {exception.Message}",
                "Erreur de diaporama",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        finally
        {
            foreach (Image image in images)
            {
                image.Dispose();
            }
        }
    }
}
