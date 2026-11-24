using SlideStudio.Core.Editing;
using SlideStudio.Core.Editing.Commands;
using SlideStudio.Core.Model;

namespace SlideStudio.Presenters;

/// <summary>Logique de sélection, presse-papiers et superposition des éléments.</summary>
public sealed class SelectionPresenter
{
    private readonly ISelectionView _view;
    private readonly UndoHistory _history;
    private Slide _slide;
    private readonly List<SlideElement> _selection = [];
    private readonly List<SlideElement> _clipboard = [];

    /// <summary>Crée le présentateur de sélection.</summary>
    public SelectionPresenter(ISelectionView view, UndoHistory history, Slide initialSlide)
    {
        ArgumentNullException.ThrowIfNull(view);
        ArgumentNullException.ThrowIfNull(history);
        ArgumentNullException.ThrowIfNull(initialSlide);

        _view = view;
        _history = history;
        _slide = initialSlide;
        Refresh();
    }

    /// <summary>Éléments actuellement sélectionnés.</summary>
    public IReadOnlyList<SlideElement> Selection => _selection;
    /// <summary>Éléments actuellement contenus dans le presse-papiers interne.</summary>
    public IReadOnlyList<SlideElement> Clipboard => _clipboard;

    /// <summary>Change la diapositive dans laquelle la sélection est effectuée.</summary>
    public void SetSlide(Slide slide)
    {
        ArgumentNullException.ThrowIfNull(slide);
        _slide = slide;
        _selection.Clear();
        Refresh();
    }

    /// <summary>Remplace la sélection courante.</summary>
    public void SetSelection(IEnumerable<SlideElement> elements)
    {
        ArgumentNullException.ThrowIfNull(elements);
        _selection.Clear();
        foreach (SlideElement element in elements)
        {
            if (_slide.Elements.Contains(element) && !_selection.Contains(element))
            {
                _selection.Add(element);
            }
        }
        Refresh();
    }

    /// <summary>Copie la sélection.</summary>
    public void Copy()
    {
        _clipboard.Clear();
        foreach (SlideElement element in _selection)
        {
            _clipboard.Add(element.Clone());
        }
        Refresh();
    }

    /// <summary>Coupe la sélection.</summary>
    public bool Cut()
    {
        Copy();
        if (_selection.Count == 0)
        {
            return false;
        }

        foreach (SlideElement element in _selection.ToArray())
        {
            _history.Execute(new DeleteElementCommand(_slide, element));
        }
        _selection.Clear();
        Refresh();
        return true;
    }

    /// <summary>Colle une copie des éléments du presse-papiers.</summary>
    public bool Paste()
    {
        if (_clipboard.Count == 0)
        {
            return false;
        }

        foreach (SlideElement element in _clipboard.Select(item => item.Clone()).ToArray())
        {
            _history.Execute(new AddElementCommand(_slide, element, "Coller un élément"));
        }
        Refresh();
        return true;
    }

    /// <summary>Supprime les éléments sélectionnés.</summary>
    public bool Delete()
    {
        if (_selection.Count == 0)
        {
            return false;
        }

        foreach (SlideElement element in _selection.ToArray())
        {
            _history.Execute(new DeleteElementCommand(_slide, element));
        }
        _selection.Clear();
        Refresh();
        return true;
    }

    /// <summary>Duplique les éléments sélectionnés sur la diapositive courante.</summary>
    public bool Duplicate()
    {
        if (_selection.Count == 0)
        {
            return false;
        }

        SlideElement[] duplicates = _selection.Select(element => element.Clone()).ToArray();
        foreach (SlideElement duplicate in duplicates)
        {
            _history.Execute(new AddElementCommand(_slide, duplicate, "Dupliquer l'élément"));
        }

        _selection.Clear();
        foreach (SlideElement duplicate in duplicates)
        {
            _selection.Add(duplicate);
        }
        Refresh();
        return true;
    }

    /// <summary>Place l'unique élément sélectionné au premier plan.</summary>
    public bool BringToFront()
    {
        if (_selection.Count != 1)
        {
            return false;
        }

        SlideElement element = _selection[0];
        _history.Execute(new ReorderElementCommand(
            _slide, element, _slide.Elements.Count - 1, "Placer au premier plan"));
        Refresh();
        return true;
    }

    /// <summary>Place l'unique élément sélectionné à l'arrière-plan.</summary>
    public bool SendToBack()
    {
        if (_selection.Count != 1)
        {
            return false;
        }

        SlideElement element = _selection[0];
        _history.Execute(new ReorderElementCommand(_slide, element, 0, "Placer à l'arrière-plan"));
        Refresh();
        return true;
    }

    /// <summary>Avance l'unique élément sélectionné d'un plan.</summary>
    public bool MoveForward() => MoveByOne(1, "Avancer d'un plan");
    /// <summary>Recule l'unique élément sélectionné d'un plan.</summary>
    public bool MoveBackward() => MoveByOne(-1, "Reculer d'un plan");

    /// <summary>Applique le gras aux zones de texte sélectionnées.</summary>
    public void SetBold(bool isBold)
    {
        foreach (TextElement element in _selection.OfType<TextElement>())
        {
            _history.Execute(EditCommands.SetBold(element, isBold));
        }
    }

    /// <summary>Applique l'italique aux zones de texte sélectionnées.</summary>
    public void SetItalic(bool isItalic)
    {
        foreach (TextElement element in _selection.OfType<TextElement>())
        {
            _history.Execute(EditCommands.SetItalic(element, isItalic));
        }
    }

    /// <summary>Change la police des zones de texte sélectionnées.</summary>
    public void SetFontFamily(string fontFamily)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fontFamily);
        foreach (TextElement element in _selection.OfType<TextElement>())
        {
            _history.Execute(EditCommands.SetFontFamily(element, fontFamily));
        }
    }

    /// <summary>Change la taille de police des zones de texte sélectionnées.</summary>
    public void SetFontSize(double points)
    {
        foreach (TextElement element in _selection.OfType<TextElement>())
        {
            _history.Execute(EditCommands.SetFontSize(element, points));
        }
    }

    /// <summary>Change la couleur du texte des zones sélectionnées.</summary>
    public void SetTextColor(ArgbColor color)
    {
        foreach (TextElement element in _selection.OfType<TextElement>())
        {
            _history.Execute(EditCommands.SetTextColor(element, color));
        }
    }

    /// <summary>Change l'alignement des zones de texte sélectionnées.</summary>
    public void SetTextAlignment(TextAlignment alignment)
    {
        foreach (TextElement element in _selection.OfType<TextElement>())
        {
            _history.Execute(EditCommands.SetTextAlignment(element, alignment));
        }
    }

    /// <summary>Change le remplissage des formes sélectionnées.</summary>
    public void SetShapeFill(ArgbColor color)
    {
        foreach (ShapeElement element in _selection.OfType<ShapeElement>())
        {
            _history.Execute(EditCommands.SetShapeFill(element, color));
        }
    }

    /// <summary>Change le contour des formes sélectionnées.</summary>
    public void SetShapeStroke(ArgbColor color)
    {
        foreach (ShapeElement element in _selection.OfType<ShapeElement>())
        {
            _history.Execute(EditCommands.SetShapeStroke(element, color));
        }
    }

    /// <summary>Change l'épaisseur du contour des formes sélectionnées.</summary>
    public void SetShapeStrokeWidth(double strokeWidth)
    {
        foreach (ShapeElement element in _selection.OfType<ShapeElement>())
        {
            _history.Execute(EditCommands.SetShapeStrokeWidth(element, strokeWidth));
        }
    }

    private bool MoveByOne(int offset, string description)
    {
        if (_selection.Count != 1)
        {
            return false;
        }

        SlideElement element = _selection[0];
        int index = _slide.IndexOf(element);
        int target = Math.Clamp(index + offset, 0, _slide.Elements.Count - 1);
        if (target == index)
        {
            return false;
        }

        _history.Execute(new ReorderElementCommand(_slide, element, target, description));
        Refresh();
        return true;
    }

    private void Refresh()
    {
        _view.SetSelection(_selection);
        _view.SetSelectionActions(
            _selection.Count > 0,
            _selection.Count > 0,
            _clipboard.Count > 0,
            _selection.Count > 0,
            _selection.Count == 1);
    }
}
