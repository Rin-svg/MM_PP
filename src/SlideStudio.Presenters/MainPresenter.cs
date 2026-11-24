using SlideStudio.Core;
using SlideStudio.Core.Editing;
using SlideStudio.Core.Editing.Commands;
using SlideStudio.Core.Model;

namespace SlideStudio.Presenters;

/// <summary>Logique de l'écran principal : fichiers, historique et diapositives.</summary>
public sealed class MainPresenter
{
    private readonly IMainView _view;
    private readonly IDialogService _dialogs;
    private readonly IDocumentStorage _storage;
    private readonly UndoHistory _history;
    private Presentation _presentation;
    private MediaStore _mediaStore = new();
    private Slide? _selectedSlide;
    private string? _fileName;
    private bool _isBusy;

    /// <summary>Crée le présentateur principal.</summary>
    public MainPresenter(
        IMainView view,
        IDialogService dialogs,
        IDocumentStorage storage,
        UndoHistory? history = null)
    {
        ArgumentNullException.ThrowIfNull(view);
        ArgumentNullException.ThrowIfNull(dialogs);
        ArgumentNullException.ThrowIfNull(storage);

        _view = view;
        _dialogs = dialogs;
        _storage = storage;
        _history = history ?? new UndoHistory();
        _history.Changed += (_, _) => Refresh();

        _presentation = new Presentation();
        _mediaStore = new MediaStore();
        _selectedSlide = _presentation.Slides[0];
        Refresh();
    }

    /// <summary>Présentation actuellement chargée.</summary>
    public Presentation Presentation => _presentation;
    /// <summary>Historique annuler/rétablir partagé.</summary>
    public UndoHistory History => _history;
    /// <summary>Diapositive active dans l'interface.</summary>
    public Slide? SelectedSlide => _selectedSlide;
    /// <summary>Chemin du fichier courant, ou <c>null</c>.</summary>
    public string? FileName => _fileName;
    /// <summary>Indique qu'une opération de fichier est en cours.</summary>
    public bool IsBusy => _isBusy;
    /// <summary>Indique si le document contient des modifications non enregistrées.</summary>
    public bool IsModified => _history.IsModified;

    /// <summary>Crée une nouvelle présentation après confirmation si nécessaire.</summary>
    public bool New()
    {
        if (!PrepareForDocumentSwitch())
        {
            return false;
        }

        _presentation = new Presentation();
        _mediaStore = new MediaStore();
        _selectedSlide = _presentation.Slides[0];
        _fileName = null;
        _history.Clear();
        Refresh();
        return true;
    }

    /// <summary>Demande un fichier puis l'ouvre.</summary>
    public bool Open()
    {
        if (_isBusy)
        {
            return false;
        }

        string? fileName = _dialogs.ChooseOpenFile();
        if (fileName is null)
        {
            return false;
        }

        return Open(fileName);
    }

    /// <summary>Ouvre le fichier indiqué.</summary>
    public bool Open(string fileName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        if (_isBusy || !PrepareForDocumentSwitch())
        {
            return false;
        }

        _isBusy = true;
        try
        {
            DocumentPackage package = _storage.OpenPackage(fileName);
            _presentation = package.Presentation;
            _mediaStore = package.Media;
            _selectedSlide = _presentation.Slides[0];
            _fileName = fileName;
            _history.Clear();
            Refresh();
            return true;
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            _dialogs.ShowError("Impossible d'ouvrir", exception.Message);
            return false;
        }
        finally
        {
            _isBusy = false;
            Refresh();
        }
    }

    /// <summary>Enregistre dans le fichier courant ou ouvre « Enregistrer sous ».</summary>
    public bool Save() => _fileName is null ? SaveAs() : Save(_fileName);

    /// <summary>Enregistre dans le fichier indiqué.</summary>
    public bool Save(string fileName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        if (_isBusy)
        {
            return false;
        }

        _isBusy = true;
        try
        {
            _storage.SavePackage(new DocumentPackage(_presentation, _mediaStore), fileName);
            _fileName = fileName;
            _history.MarkSaved();
            Refresh();
            return true;
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            _dialogs.ShowError("Impossible d'enregistrer", exception.Message);
            return false;
        }
        finally
        {
            _isBusy = false;
            Refresh();
        }
    }

    /// <summary>Demande un fichier puis enregistre la présentation.</summary>
    public bool SaveAs()
    {
        if (_isBusy)
        {
            return false;
        }

        string suggestedName = _presentation.Title + ".slds";
        string? fileName = _dialogs.ChooseSaveFile(suggestedName);
        return fileName is null ? false : Save(fileName);
    }

    /// <summary>Ferme le document après confirmation si nécessaire.</summary>
    public bool Close()
    {
        if (_isBusy)
        {
            return false;
        }

        if (!PrepareForDocumentSwitch())
        {
            return false;
        }

        _presentation = new Presentation();
        _mediaStore = new MediaStore();
        _selectedSlide = _presentation.Slides[0];
        _fileName = null;
        _history.Clear();
        Refresh();
        return true;
    }

    /// <summary>Sélectionne une diapositive de la présentation.</summary>
    public void SelectSlide(Slide slide)
    {
        ArgumentNullException.ThrowIfNull(slide);
        if (!_presentation.Slides.Contains(slide))
        {
            throw new InvalidOperationException("La diapositive n'appartient pas à la présentation.");
        }

        _selectedSlide = slide;
        Refresh();
    }

    /// <summary>Annule la prochaine action si aucune opération de fichier n'est en cours.</summary>
    public bool Undo() => !_isBusy && _history.Undo();
    /// <summary>Rétablit la prochaine action si aucune opération de fichier n'est en cours.</summary>
    public bool Redo() => !_isBusy && _history.Redo();

    /// <summary>Ajoute une diapositive après la diapositive active.</summary>
    public void AddSlide(Slide? slide = null)
    {
        if (_isBusy)
        {
            return;
        }

        Slide selected = _selectedSlide ?? _presentation.Slides[0];
        int index = _presentation.IndexOf(selected) + 1;
        _history.Execute(new AddSlideCommand(_presentation, slide ?? new Slide(), index));
        _selectedSlide = _presentation.Slides[index];
        Refresh();
    }

    /// <summary>Supprime la diapositive active si ce n'est pas la dernière.</summary>
    public void DeleteSelectedSlide()
    {
        if (_isBusy || _selectedSlide is null)
        {
            return;
        }

        if (_presentation.Slides.Count == 1)
        {
            return;
        }

        int index = _presentation.IndexOf(_selectedSlide);
        _history.Execute(new DeleteSlideCommand(_presentation, _selectedSlide));
        _selectedSlide = _presentation.Slides[Math.Min(index, _presentation.Slides.Count - 1)];
        Refresh();
    }

    /// <summary>Duplique la diapositive active juste après son emplacement actuel.</summary>
    public bool DuplicateSelectedSlide()
    {
        if (_isBusy || _selectedSlide is null)
        {
            return false;
        }

        int index = _presentation.IndexOf(_selectedSlide);
        Slide duplicate = _selectedSlide.Clone();
        _history.Execute(new AddSlideCommand(
            _presentation,
            duplicate,
            index + 1,
            "Dupliquer la diapositive"));
        _selectedSlide = duplicate;
        Refresh();
        return true;
    }

    /// <summary>Déplace une diapositive à la position demandée.</summary>
    public void MoveSlide(Slide slide, int newIndex)
    {
        if (_isBusy)
        {
            return;
        }

        _history.Execute(new MoveSlideCommand(_presentation, slide, newIndex));
        Refresh();
    }

    private bool PrepareForDocumentSwitch()
    {
        if (_isBusy)
        {
            return false;
        }

        if (!_history.IsModified)
        {
            return true;
        }

        UnsavedChangesChoice choice = _dialogs.ConfirmUnsavedChanges(_presentation.Title);
        return choice switch
        {
            UnsavedChangesChoice.Cancel => false,
            UnsavedChangesChoice.Discard => true,
            UnsavedChangesChoice.Save => Save(),
            _ => false
        };
    }

    private void Refresh()
    {
        _view.SetDocument(_presentation, _selectedSlide);
        _view.SetDocumentTitle(_presentation.Title + (_history.IsModified ? " *" : string.Empty));
        _view.SetModified(_history.IsModified);
        _view.SetCommandState(
            _history.CanUndo,
            _history.CanRedo,
            !_isBusy,
            !_isBusy && _presentation.Slides.Count > 1);
    }
}
