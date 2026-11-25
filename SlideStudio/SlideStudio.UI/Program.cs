using SlideStudio.Infrastructure;
using SlideStudio.Presenters;

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

        // POURQUOI : c'est le seul endroit qui connaît toutes les couches ;
        // les Presenters et l'interface ne se connaissent que par leurs contrats.
        var fenetre = new Form1();
        var dialogues = new WinFormsDialogService();
        var stockage = new SldsDocumentStorage();
        var presenter = new MainPresenter(fenetre, dialogues, stockage);

        Application.Run(fenetre);

        // ATTENTION : rien d'autre ne garde le Presenter en vie ; sans cette ligne,
        // le ramasse-miettes pourrait le supprimer pendant que la fenêtre est ouverte.
        GC.KeepAlive(presenter);
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