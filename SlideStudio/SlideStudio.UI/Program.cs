namespace SlideStudio.UI;

/// <summary>Point d'entrée de l'application WinForms.</summary>
internal static class Program
{
    /// <summary>Lance l'application.</summary>
    [STAThread]
    static void Main()
    {
        // ROBUSTESSE : les filets sont posés avant de créer la moindre fenêtre,
        // sinon une erreur au démarrage ferait planter le programme sans explication.
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += (_, e) => AfficherErreur(e.Exception);
        AppDomain.CurrentDomain.UnhandledException +=
            (_, e) => AfficherErreur(e.ExceptionObject as Exception);

        ApplicationConfiguration.Initialize();
        Application.Run(new Form1());
    }

    /// <summary>Affiche une erreur inattendue dans un message lisible.</summary>
    private static void AfficherErreur(Exception? erreur)
    {
        // UTILISATEUR : un message simple, jamais la trace technique complète.
        MessageBox.Show(
            "Une erreur inattendue est survenue.\n\n" + (erreur?.Message ?? "Cause inconnue."),
            "SlideStudio",
            MessageBoxButtons.OK,
            MessageBoxIcon.Error);
    }
}