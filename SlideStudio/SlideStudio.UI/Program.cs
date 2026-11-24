namespace SlideStudio.UI;

/// <summary>Point d'entrée de l'application WinForms.</summary>
internal static class Program
{
    /// <summary>Lance l'application.</summary>
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new Form1());
    }
}