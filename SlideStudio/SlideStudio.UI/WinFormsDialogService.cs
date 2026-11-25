using SlideStudio.Presenters;

namespace SlideStudio.UI;

/// <summary>Boîtes de dialogue WinForms utilisées par les Presenters.</summary>
internal sealed class WinFormsDialogService : IDialogService
{
    // UTILISATEUR : un seul format de fichier proposé, pour éviter d'ouvrir autre chose par erreur.
    private const string FileFilter = "Présentation SlideStudio (*.slds)|*.slds";

    /// <inheritdoc />
    public string? ChooseOpenFile()
    {
        using var dialog = new OpenFileDialog
        {
            Title = "Ouvrir une présentation",
            Filter = FileFilter,
            CheckFileExists = true
        };
        return dialog.ShowDialog() == DialogResult.OK ? dialog.FileName : null;
    }

    /// <inheritdoc />
    public string? ChooseSaveFile(string suggestedName)
    {
        using var dialog = new SaveFileDialog
        {
            Title = "Enregistrer la présentation",
            Filter = FileFilter,
            FileName = suggestedName,
            DefaultExt = "slds",
            AddExtension = true,
            OverwritePrompt = true
        };
        return dialog.ShowDialog() == DialogResult.OK ? dialog.FileName : null;
    }

    /// <inheritdoc />
    public UnsavedChangesChoice ConfirmUnsavedChanges(string documentTitle)
    {
        // POURQUOI : Annuler est le choix par défaut de la croix et d'Échap,
        // ce qui évite de perdre du travail par erreur.
        var result = MessageBox.Show(
            $"Voulez-vous enregistrer les modifications de « {documentTitle} » avant de continuer ?",
            "SlideStudio",
            MessageBoxButtons.YesNoCancel,
            MessageBoxIcon.Warning);

        return result switch
        {
            DialogResult.Yes => UnsavedChangesChoice.Save,
            DialogResult.No => UnsavedChangesChoice.Discard,
            _ => UnsavedChangesChoice.Cancel
        };
    }

    /// <inheritdoc />
    public void ShowError(string title, string message)
    {
        MessageBox.Show(message, title, MessageBoxButtons.OK, MessageBoxIcon.Error);
    }
}