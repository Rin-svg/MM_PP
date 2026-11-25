using SlideStudio.Core.Model;
using SlideStudio.Presenters;

namespace SlideStudio.UI;

/// <summary>Fenêtre principale de SlideStudio.</summary>
public partial class Form1 : Form, IMainView
{
    private string _documentTitle = "Sans titre";
    private bool _isModified;

    /// <summary>Crée la fenêtre principale.</summary>
    public Form1()
    {
        InitializeComponent();
        MettreAJourTitreFenetre();
    }

    /// <summary>Présentation actuellement affichée.</summary>
    public Presentation? CurrentPresentation { get; private set; }

    /// <summary>Diapositive actuellement sélectionnée.</summary>
    public Slide? CurrentSlide { get; private set; }

    /// <summary>Indique si l'action Annuler est disponible.</summary>
    public bool CanUndo { get; private set; }

    /// <summary>Indique si l'action Rétablir est disponible.</summary>
    public bool CanRedo { get; private set; }

    /// <summary>Indique si l'action Enregistrer est disponible.</summary>
    public bool CanSave { get; private set; }

    /// <summary>Indique si la suppression de la diapositive est disponible.</summary>
    public bool CanDeleteSlide { get; private set; }

    /// <inheritdoc />
    public void SetDocument(Presentation presentation, Slide? selectedSlide)
    {
        CurrentPresentation = presentation;
        CurrentSlide = selectedSlide;
    }

    /// <inheritdoc />
    public void SetDocumentTitle(string title)
    {
        _documentTitle = title;
        MettreAJourTitreFenetre();
    }

    /// <inheritdoc />
    public void SetModified(bool isModified)
    {
        _isModified = isModified;
        MettreAJourTitreFenetre();
    }

    /// <inheritdoc />
    public void SetCommandState(bool canUndo, bool canRedo, bool canSave, bool canDeleteSlide)
    {
        // POURQUOI : la fenêtre ne décide rien, elle note seulement ce que le Presenter
        // demande ; les menus et boutons liront ces valeurs pour se griser.
        CanUndo = canUndo;
        CanRedo = canRedo;
        CanSave = canSave;
        CanDeleteSlide = canDeleteSlide;
    }

    /// <summary>Recompose le titre de la fenêtre avec l'astérisque « modifié ».</summary>
    private void MettreAJourTitreFenetre()
    {
        // UTILISATEUR : l'astérisque devant le nom signale un travail non enregistré.
        Text = (_isModified ? "*" : string.Empty) + _documentTitle + " - SlideStudio";
    }
}