namespace SlideStudio.UI;

/// <summary>Fenêtre principale de SlideStudio.</summary>
public partial class Form1 : Form
{
    public Form1()
    {
        InitializeComponent();
        ApplyTheme();
    }

    /// <summary>Applique le thème graphique à la fenêtre.</summary>
    private void ApplyTheme()
    {
        Text = "SlideStudio";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(1000, 650);
        Size = new Size(1200, 750);

        BackColor = SlideStudioTheme.Background;
        ForeColor = SlideStudioTheme.Text;
        Font = SlideStudioTheme.CreateBodyFont();
    }
}
