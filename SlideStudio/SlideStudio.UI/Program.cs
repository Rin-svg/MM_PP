namespace SlideStudio.UI;

/// <summary>Point d'entrée de l'application WinForms.</summary>
internal static class Program
{
    /// <summary>Lance l'application.</summary>
    [STAThread]
    static void Main()
    {
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += (_, eventArgs) =>
        {
            MessageBox.Show(
                $"Une erreur inattendue s'est produite. L'application va se fermer.\n\n{eventArgs.Exception.Message}",
                "Erreur inattendue",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            Application.Exit();
        };
        AppDomain.CurrentDomain.UnhandledException += (_, eventArgs) =>
        {
            string message = eventArgs.ExceptionObject is Exception exception
                ? exception.Message
                : eventArgs.ExceptionObject?.ToString() ?? "Erreur inconnue";
            MessageBox.Show(
                $"Une erreur fatale s'est produite.\n\n{message}",
                "Erreur fatale",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        };

        ApplicationConfiguration.Initialize();
        Application.Run(new Form1());
    }
}