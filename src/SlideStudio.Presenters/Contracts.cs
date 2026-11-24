using SlideStudio.Core.Model;

namespace SlideStudio.Presenters;

/// <summary>Résultat d'une confirmation appliquée à un document modifié.</summary>
public enum UnsavedChangesChoice
{
    /// <summary>L'utilisateur annule l'action en cours.</summary>
    Cancel,
    /// <summary>L'utilisateur veut enregistrer avant de continuer.</summary>
    Save,
    /// <summary>L'utilisateur accepte de perdre les modifications non enregistrées.</summary>
    Discard
}

/// <summary>Boîtes de dialogue indépendantes de WinForms.</summary>
public interface IDialogService
{
    /// <summary>Choisit un fichier à ouvrir.</summary>
    string? ChooseOpenFile();
    /// <summary>Choisit un fichier pour l'enregistrement.</summary>
    string? ChooseSaveFile(string suggestedName);
    /// <summary>Demande quoi faire en cas de modifications non enregistrées.</summary>
    UnsavedChangesChoice ConfirmUnsavedChanges(string documentTitle);
    /// <summary>Affiche une erreur à l'utilisateur.</summary>
    void ShowError(string title, string message);
}

/// <summary>Prise minimale de l'écran principal, sans dépendance à WinForms.</summary>
public interface IMainView
{
    /// <summary>Affiche le document courant et la diapositive sélectionnée.</summary>
    void SetDocument(Presentation presentation, Slide? selectedSlide);
    /// <summary>Met à jour le titre de la fenêtre.</summary>
    void SetDocumentTitle(string title);
    /// <summary>Met à jour l'indicateur de modifications.</summary>
    void SetModified(bool isModified);
    /// <summary>Met à jour l'état des commandes principales.</summary>
    void SetCommandState(bool canUndo, bool canRedo, bool canSave, bool canDeleteSlide);
}

/// <summary>Prise utilisée pour afficher et griser les commandes de sélection.</summary>
public interface ISelectionView
{
    /// <summary>Affiche les éléments sélectionnés.</summary>
    void SetSelection(IReadOnlyList<SlideElement> selection);
    /// <summary>Met à jour l'état des actions de sélection.</summary>
    void SetSelectionActions(bool canCopy, bool canCut, bool canPaste, bool canDelete, bool canReorder);
}
