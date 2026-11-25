using SlideStudio.Core;
using SlideStudio.Core.Editing;
using SlideStudio.Core.Editing.Commands;
using SlideStudio.Core.Model;
using SlideStudio.Infrastructure;
using SlideStudio.Presenters;
using SlideStudio.UI.Canvas;
using SlideStudio.UI.Export;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

namespace SlideStudio.UI;

/// <summary>Fenêtre principale de SlideStudio, reliée aux services et au présentateur M3.</summary>
public partial class Form1 : Form, IMainView, ISelectionView
{
    private static readonly Color WindowColor = Color.FromArgb(15, 17, 26);
    private static readonly Color PanelColor = Color.FromArgb(25, 29, 42);
    private static readonly Color ElevatedColor = Color.FromArgb(34, 39, 55);
    private static readonly Color BorderColor = Color.FromArgb(57, 64, 83);
    private static readonly Color PrimaryTextColor = Color.FromArgb(244, 246, 252);
    private static readonly Color MutedTextColor = Color.FromArgb(169, 178, 198);
    private static readonly Color AccentColor = Color.FromArgb(255, 101, 24);
    private static readonly Color AccentHoverColor = Color.FromArgb(255, 129, 55);
    private static readonly Color SelectionColor = Color.FromArgb(53, 61, 82);
    private static readonly ToolStripRenderer DarkToolStripRenderer = new DarkRenderer();

    private readonly ListBox _slideList = new();
    private readonly RoundedPanel _slideSurface = new();
    private readonly SlideRenderer _slideRenderer = new();
    private readonly ThumbnailCache _thumbnailCache;
    private readonly SlideCanvas _slideCanvas;
    private readonly Label _slideCount = new();
    private readonly ToolStripStatusLabel _modifiedStatus = new();
    private readonly ToolStripStatusLabel _slidePositionStatus = new();
    private readonly ToolStripStatusLabel _selectionStatus = new();
    private readonly TextBox _titleEditor = new();
    private readonly AnimatedButton _backgroundButton = new();
    private readonly AnimatedButton _addSlideButton = new();
    private readonly AnimatedButton _duplicateSlideButton = new();
    private readonly AnimatedButton _deleteSlideButton = new();
    private readonly ListBox _elementList = new();
    private readonly ComboBox _fontFamilyEditor = new();
    private readonly ComboBox _textAlignmentEditor = new();
    private readonly NumericUpDown _fontSizeEditor = new();
    private readonly NumericUpDown _shapeStrokeWidthEditor = new();
    private readonly CheckBox _boldEditor = new();
    private readonly CheckBox _italicEditor = new();
    private readonly AnimatedButton _textColorButton = new();
    private readonly AnimatedButton _shapeFillButton = new();
    private readonly AnimatedButton _shapeStrokeButton = new();
    private readonly ToolStripButton _saveButton = new("Enregistrer");
    private readonly ToolStripButton _undoButton = new("Annuler");
    private readonly ToolStripButton _redoButton = new("Rétablir");
    private readonly ToolStripLabel _zoomStatus = new("100 %");
    private readonly Panel _topPanel = new();
    private readonly Panel _documentHeader = new();
    private readonly BrandMark _brandMark = new();
    private readonly Label _documentHeaderTitle = new();
    private readonly Label _documentHeaderSubtitle = new();
    private readonly WelcomeScreen _welcomeScreen = new();
    private readonly LoadingScreen _loadingScreen = new();
    private readonly ToolStripMenuItem _saveMenuItem = new("Enregistrer");
    private readonly ToolStripMenuItem _undoMenuItem = new("Annuler");
    private readonly ToolStripMenuItem _redoMenuItem = new("Rétablir");
    private readonly ToolStripMenuItem _copyElementMenuItem = new("Copier");
    private readonly ToolStripMenuItem _cutElementMenuItem = new("Couper");
    private readonly ToolStripMenuItem _pasteElementMenuItem = new("Coller");
    private readonly ToolStripMenuItem _duplicateElementMenuItem = new("Dupliquer les éléments");
    private readonly ToolStripMenuItem _deleteElementMenuItem = new("Supprimer les éléments");
    private readonly ToolStripMenuItem _bringToFrontMenuItem = new("Placer au premier plan");
    private readonly ToolStripMenuItem _sendToBackMenuItem = new("Placer à l'arrière-plan");
    private readonly ToolStripMenuItem _moveForwardMenuItem = new("Avancer d'un plan");
    private readonly ToolStripMenuItem _moveBackwardMenuItem = new("Reculer d'un plan");
    private readonly ToolStripMenuItem _deleteMenuItem = new("Supprimer la diapositive");
    private readonly ToolStrip _toolStrip = new();
    private readonly StatusStrip _statusStrip = new();
    private readonly MainPresenter _presenter;
    private SelectionPresenter? _selectionPresenter;
    private Slide? _selectionSlide;
    private bool _updatingView;
    private bool _updatingSelectionView;
    private Slide? _draggedSlide;
    private Point _dragStartPoint;
    private int _hoveredSlideIndex = -1;
    private StartupAction? _pendingStartupAction;

    /// <summary>Crée la fenêtre et connecte l'interface aux Presenters et au stockage .slds.</summary>
    public Form1()
    {
        _slideCanvas = new SlideCanvas(_slideRenderer);
        _thumbnailCache = new ThumbnailCache(_slideRenderer);
        InitializeComponent();
        BuildInterface();
        _presenter = new MainPresenter(this, new WinFormsDialogService(this), new SldsDocumentStorage());
        if (_presenter.SelectedSlide is { } slide)
        {
            _selectionSlide = slide;
            _selectionPresenter = new SelectionPresenter(this, _presenter.History, slide);
        }
    }

    /// <summary>Applique le thème sombre de la barre de titre native lorsque Windows le permet.</summary>
    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        int enabled = 1;
        int result = NativeMethods.DwmSetWindowAttribute(Handle, 20, ref enabled, sizeof(int));
        if (result != 0)
        {
            result = NativeMethods.DwmSetWindowAttribute(Handle, 19, ref enabled, sizeof(int));
        }
        if (result != 0)
        {
            System.Diagnostics.Debug.WriteLine(
                $"Impossible d'appliquer le thème sombre à la barre de titre (HRESULT 0x{result:X8}).");
        }
    }

    /// <inheritdoc />
    public void SetDocument(Presentation presentation, Slide? selectedSlide)
    {
        _updatingView = true;
        try
        {
            _thumbnailCache.Clear();
            _slideList.BeginUpdate();
            _slideList.Items.Clear();
            for (int index = 0; index < presentation.Slides.Count; index++)
            {
                _slideList.Items.Add(new SlideListItem(presentation.Slides[index], index + 1));
            }

            _slideList.SelectedIndex = -1;
            if (selectedSlide is not null)
            {
                for (int index = 0; index < presentation.Slides.Count; index++)
                {
                    if (ReferenceEquals(presentation.Slides[index], selectedSlide))
                    {
                        _slideList.SelectedIndex = index;
                        break;
                    }
                }
            }
            _slideCount.Text = $"{presentation.Slides.Count} diapositive(s)";
            _slidePositionStatus.Text = selectedSlide is null
                ? "Aucune diapositive"
                : $"Diapositive {_slideList.SelectedIndex + 1} sur {presentation.Slides.Count}";
            _slideRenderer.SetMediaStore(_presenter?.MediaStore);
            _slideCanvas.SetSlide(selectedSlide);
            _slideSurface.BackColor = PanelColor;
            _backgroundButton.BackColor = selectedSlide is null
                ? Color.White
                : ToDrawingColor(selectedSlide.Background);
            _backgroundButton.SetSwatchColor(_backgroundButton.BackColor);
            _backgroundButton.Enabled = selectedSlide is not null;
            _slideSurface.Invalidate();

            if (_selectionPresenter is not null && selectedSlide is not null)
            {
                if (!ReferenceEquals(_selectionSlide, selectedSlide))
                {
                    _selectionSlide = selectedSlide;
                    _selectionPresenter.SetSlide(selectedSlide);
                }
                else if (_selectionPresenter.Selection.Any(element => !selectedSlide.Elements.Contains(element)))
                {
                    _selectionPresenter.SetSelection([]);
                }
                else
                {
                    UpdateSelectionView(_selectionPresenter.Selection.ToArray());
                }
            }
            else
            {
                RefreshElementList(selectedSlide, []);
            }
        }
        finally
        {
            _slideList.EndUpdate();
            _updatingView = false;
        }
    }

    /// <inheritdoc />
    public void SetDocumentTitle(string title)
    {
        Text = $"{title} — SlideStudio";
        string displayTitle = title.EndsWith(" *", StringComparison.Ordinal)
            ? title[..^2]
            : title;
        _documentHeaderTitle.Text = displayTitle;
        _documentHeaderSubtitle.Text = "Présentation SlideStudio";
        if (!_titleEditor.Focused)
        {
            _titleEditor.Text = displayTitle;
        }
    }

    /// <inheritdoc />
    public void SetModified(bool isModified)
    {
        _modifiedStatus.Text = isModified ? "Modifications non enregistrées" : "Enregistré";
        _modifiedStatus.ForeColor = isModified ? AccentHoverColor : MutedTextColor;
        _documentHeaderSubtitle.Text = isModified ? "Modifications non enregistrées" : "Modifications enregistrées";
        _documentHeaderSubtitle.ForeColor = isModified ? AccentHoverColor : MutedTextColor;
    }

    /// <inheritdoc />
    public void SetSelection(IReadOnlyList<SlideElement> selection)
    {
        _slideCanvas.SetSelection(selection);
        UpdateSelectionView(selection);
    }

    private void UpdateSelectionView(IReadOnlyList<SlideElement> selection)
    {
        _updatingSelectionView = true;
        try
        {
            RefreshElementList(_selectionSlide, selection);
            TextElement[] selectedText = selection.OfType<TextElement>().ToArray();
            ShapeElement[] selectedShapes = selection.OfType<ShapeElement>().ToArray();
            bool isText = selection.Count > 0 && selectedText.Length == selection.Count;
            bool isShape = selection.Count > 0 && selectedShapes.Length == selection.Count;
            _fontFamilyEditor.Enabled = isText;
            _textAlignmentEditor.Enabled = isText;
            _fontSizeEditor.Enabled = isText;
            _boldEditor.Enabled = isText;
            _italicEditor.Enabled = isText;
            _textColorButton.Enabled = isText;
            _shapeFillButton.Enabled = isShape;
            _shapeStrokeButton.Enabled = isShape;
            _shapeStrokeWidthEditor.Enabled = isShape;

            if (isText)
            {
                TextElement text = selectedText[0];
                if (!_fontFamilyEditor.Items.Contains(text.FontFamily))
                {
                    _fontFamilyEditor.Items.Add(text.FontFamily);
                }
                _fontFamilyEditor.Text = selectedText.All(item => item.FontFamily == text.FontFamily)
                    ? text.FontFamily
                    : string.Empty;
                _textAlignmentEditor.SelectedItem = selectedText.All(item => item.Alignment == text.Alignment)
                    ? new AlignmentOption(text.Alignment, GetAlignmentLabel(text.Alignment))
                    : null;
                _fontSizeEditor.Value = (decimal)text.FontSizePoints;
                _boldEditor.CheckState = GetCommonCheckState(selectedText.Select(item => item.IsBold));
                _italicEditor.CheckState = GetCommonCheckState(selectedText.Select(item => item.IsItalic));
                _textColorButton.BackColor = ToDrawingColor(text.Color);
                _textColorButton.SetSwatchColor(_textColorButton.BackColor);
            }
            else if (isShape)
            {
                ShapeElement shape = selectedShapes[0];
                _shapeFillButton.BackColor = ToDrawingColor(shape.Fill);
                _shapeStrokeButton.BackColor = ToDrawingColor(shape.Stroke);
                _shapeFillButton.SetSwatchColor(_shapeFillButton.BackColor);
                _shapeStrokeButton.SetSwatchColor(_shapeStrokeButton.BackColor);
                _shapeStrokeWidthEditor.Value = (decimal)shape.StrokeWidth;
            }
        }
        finally
        {
            _updatingSelectionView = false;
        }
    }

    /// <inheritdoc />
    public void SetSelectionActions(bool canCopy, bool canCut, bool canPaste, bool canDelete, bool canReorder)
    {
        _copyElementMenuItem.Enabled = canCopy;
        _cutElementMenuItem.Enabled = canCut;
        _pasteElementMenuItem.Enabled = canPaste;
        _duplicateElementMenuItem.Enabled = canCopy;
        _deleteElementMenuItem.Enabled = canDelete;
        _bringToFrontMenuItem.Enabled = canReorder;
        _sendToBackMenuItem.Enabled = canReorder;
        _moveForwardMenuItem.Enabled = canReorder;
        _moveBackwardMenuItem.Enabled = canReorder;
        int selectedCount = _elementList.SelectedItems.Count;
        _selectionStatus.Text = selectedCount switch
        {
            1 => "1 élément sélectionné",
            > 1 => $"{selectedCount} éléments sélectionnés",
            _ => "Aucun élément sélectionné"
        };
    }

    /// <inheritdoc />
    public void SetCommandState(bool canUndo, bool canRedo, bool canSave, bool canDeleteSlide)
    {
        _saveMenuItem.Enabled = canSave;
        _undoMenuItem.Enabled = canUndo;
        _redoMenuItem.Enabled = canRedo;
        _saveButton.Enabled = canSave;
        _undoButton.Enabled = canUndo;
        _redoButton.Enabled = canRedo;
        _deleteMenuItem.Enabled = canDeleteSlide;
        _deleteSlideButton.Enabled = canDeleteSlide;
        _addSlideButton.Enabled = canSave;
        _duplicateSlideButton.Enabled = canSave;
        _slideList.Enabled = canSave;
    }

    private void BuildInterface()
    {
        Text = "SlideStudio";
        MinimumSize = new Size(960, 600);
        ClientSize = new Size(1200, 760);
        StartPosition = FormStartPosition.CenterScreen;
        KeyPreview = true;
        CreateMenus();
        CreateToolbar();
        CreateWorkspace();
        CreateStatusBar();
        _topPanel.Dock = DockStyle.Top;
        _topPanel.Height = 108;
        Controls.Add(_topPanel);
        Controls.SetChildIndex(_topPanel, 0);
        Controls.SetChildIndex(_statusStrip, 1);
        CreateDocumentHeader();
        _topPanel.Controls.SetChildIndex(_documentHeader, 0);
        _topPanel.Controls.SetChildIndex(MainMenuStrip!, 1);
        _topPanel.Controls.SetChildIndex(_toolStrip, 2);
        ApplyDarkTheme(this);
        _welcomeScreen.ActionRequested += WelcomeScreen_ActionRequested;
        _loadingScreen.Completed += LoadingScreen_Completed;
        _slideCanvas.SelectionChanged += SlideCanvas_SelectionChanged;
        _slideCanvas.FramesChangeRequested += SlideCanvas_FramesChangeRequested;
        _slideCanvas.TextChangeRequested += SlideCanvas_TextChangeRequested;
        _slideCanvas.DeletionRequested += SlideCanvas_DeletionRequested;
        _slideCanvas.ZoomChanged += (_, _) => _zoomStatus.Text = $"{_slideCanvas.ZoomFactor:P0}";
        FormClosed += (_, _) =>
        {
            _thumbnailCache.Dispose();
            _slideRenderer.Dispose();
        };
        Controls.Add(_welcomeScreen);
        Controls.Add(_loadingScreen);
        _welcomeScreen.BringToFront();
        KeyDown += Form1_KeyDown;
        FormClosing += Form1_FormClosing;
    }

    private void WelcomeScreen_ActionRequested(object? sender, StartupAction action)
    {
        _pendingStartupAction = action;
        _loadingScreen.BringToFront();
        _loadingScreen.Start();
    }

    private void LoadingScreen_Completed(object? sender, EventArgs e)
    {
        bool ready = _pendingStartupAction switch
        {
            StartupAction.NewPresentation => _presenter.New(),
            StartupAction.OpenPresentation => _presenter.Open(),
            _ => false
        };
        _pendingStartupAction = null;
        if (ready)
        {
            _loadingScreen.Hide();
            _welcomeScreen.Hide();
            return;
        }

        _loadingScreen.Hide();
        _welcomeScreen.BringToFront();
        _welcomeScreen.Show();
    }

    private void SlideCanvas_SelectionChanged(object? sender, CanvasSelectionChangedEventArgs e)
    {
        _selectionPresenter?.SetSelection(e.Selection);
    }

    private void SlideCanvas_FramesChangeRequested(object? sender, CanvasFramesChangedEventArgs e)
    {
        try
        {
            _presenter.History.Execute(new SetFramesCommand(e.Frames, e.GestureId));
        }
        catch (DocumentValidationException exception)
        {
            ShowUiError(exception.Message);
            _slideCanvas.CancelDrag();
        }
    }

    private void SlideCanvas_TextChangeRequested(object? sender, CanvasTextChangedEventArgs e)
    {
        try
        {
            _presenter.History.Execute(EditCommands.SetText(e.Element, e.Text));
        }
        catch (DocumentValidationException exception)
        {
            ShowUiError(exception.Message);
        }
    }

    private void SlideCanvas_DeletionRequested(object? sender, CanvasDeletionRequestedEventArgs e)
    {
        if (_selectionPresenter is null)
        {
            return;
        }

        _selectionPresenter.SetSelection(e.Elements);
        _selectionPresenter.Delete();
    }

    private void InsertCanvasElement(SlideElement element)
    {
        if (_presenter.SelectedSlide is not { } slide)
        {
            return;
        }

        try
        {
            _presenter.History.Execute(new AddElementCommand(slide, element));
            _selectionPresenter?.SetSelection([element]);
            _slideCanvas.Focus();
        }
        catch (DocumentValidationException exception)
        {
            ShowUiError(exception.Message);
        }
    }

    private void InsertImage()
    {
        if (_presenter.SelectedSlide is not { } slide)
        {
            return;
        }

        using OpenFileDialog dialog = new()
        {
            Title = "Insérer une image",
            Filter = "Images|*.png;*.jpg;*.jpeg;*.gif;*.bmp",
            CheckFileExists = true,
            Multiselect = false
        };
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        try
        {
            if (slide.Elements.Count >= Limits.MaxElementsPerSlide)
            {
                throw new DocumentValidationException("La diapositive contient déjà le nombre maximal d'éléments.");
            }

            FileInfo file = new(dialog.FileName);
            if (file.Length is <= 0 or > Limits.MaxMediaBytes)
            {
                throw new DocumentValidationException("La taille de l'image doit être comprise entre 1 octet et 20 Mo.");
            }

            string extension = Path.GetExtension(file.Name);
            if (!DocumentRules.AllowedImageExtensions.Contains(extension))
            {
                throw new DocumentValidationException("Format d'image non pris en charge.");
            }

            string mediaName = Guid.NewGuid().ToString("N") + extension.ToLowerInvariant();
            byte[] bytes = File.ReadAllBytes(file.FullName);
            _presenter.MediaStore.Add(mediaName, bytes);
            ImageElement image = new(new ElementFrame(320, 180, 640, 360), mediaName);
            _presenter.History.Execute(new AddElementCommand(slide, image, "Insérer une image"));
            _selectionPresenter?.SetSelection([image]);
            _slideCanvas.Focus();
        }
        catch (DocumentValidationException exception)
        {
            ShowUiError(exception.Message);
        }
        catch (IOException exception)
        {
            ShowUiError($"Impossible de lire l'image : {exception.Message}");
        }
        catch (UnauthorizedAccessException exception)
        {
            ShowUiError($"Accès refusé pour l'image : {exception.Message}");
        }
    }

    private void CreateMenus()
    {
        MenuStrip menu = new();
        menu.Dock = DockStyle.Top;
        menu.Height = 30;
        menu.Padding = new Padding(12, 3, 0, 3);
        ToolStripMenuItem fileMenu = new("Fichier");
        fileMenu.DropDownItems.Add(CreateMenuItem("Nouveau", Keys.Control | Keys.N, (_, _) => _presenter.New()));
        fileMenu.DropDownItems.Add(CreateMenuItem("Ouvrir…", Keys.Control | Keys.O, (_, _) => _presenter.Open()));
        fileMenu.DropDownItems.Add(new ToolStripSeparator());
        fileMenu.DropDownItems.Add(_saveMenuItem);
        _saveMenuItem.ShortcutKeys = Keys.Control | Keys.S;
        _saveMenuItem.Click += (_, _) => _presenter.Save();
        fileMenu.DropDownItems.Add(CreateMenuItem("Enregistrer sous…", Keys.Control | Keys.Shift | Keys.S, (_, _) => _presenter.SaveAs()));
        fileMenu.DropDownItems.Add(new ToolStripSeparator());
        fileMenu.DropDownItems.Add(CreateMenuItem("Exporter les diapositives en PNG…", Keys.None, ExportSlidesAsPng));
        fileMenu.DropDownItems.Add(CreateMenuItem("Exporter la présentation en PDF…", Keys.None, ExportPresentationAsPdf));
        fileMenu.DropDownItems.Add(new ToolStripSeparator());
        fileMenu.DropDownItems.Add(CreateMenuItem("Quitter", Keys.Alt | Keys.F4, (_, _) => Close()));

        ToolStripMenuItem editMenu = new("Édition");
        _undoMenuItem.ShortcutKeys = Keys.Control | Keys.Z;
        _undoMenuItem.Click += (_, _) => _presenter.Undo();
        editMenu.DropDownItems.Add(_undoMenuItem);
        _redoMenuItem.ShortcutKeys = Keys.Control | Keys.Y;
        _redoMenuItem.Click += (_, _) => _presenter.Redo();
        editMenu.DropDownItems.Add(_redoMenuItem);
        editMenu.DropDownItems.Add(new ToolStripSeparator());
        ConfigureSelectionMenuItem(_copyElementMenuItem, Keys.Control | Keys.C, () => _selectionPresenter?.Copy());
        ConfigureSelectionMenuItem(_cutElementMenuItem, Keys.Control | Keys.X, () => _selectionPresenter?.Cut());
        ConfigureSelectionMenuItem(_pasteElementMenuItem, Keys.Control | Keys.V, () => _selectionPresenter?.Paste());
        ConfigureSelectionMenuItem(_duplicateElementMenuItem, Keys.Control | Keys.Shift | Keys.D, () => _selectionPresenter?.Duplicate());
        ConfigureSelectionMenuItem(_deleteElementMenuItem, Keys.None, () => _selectionPresenter?.Delete());
        editMenu.DropDownItems.AddRange(
        [
            _copyElementMenuItem,
            _cutElementMenuItem,
            _pasteElementMenuItem,
            _duplicateElementMenuItem,
            _deleteElementMenuItem,
            new ToolStripSeparator()
        ]);
        ConfigureSelectionMenuItem(_bringToFrontMenuItem, Keys.None, () => _selectionPresenter?.BringToFront());
        ConfigureSelectionMenuItem(_sendToBackMenuItem, Keys.None, () => _selectionPresenter?.SendToBack());
        ConfigureSelectionMenuItem(_moveForwardMenuItem, Keys.None, () => _selectionPresenter?.MoveForward());
        ConfigureSelectionMenuItem(_moveBackwardMenuItem, Keys.None, () => _selectionPresenter?.MoveBackward());
        editMenu.DropDownItems.AddRange(
        [
            _bringToFrontMenuItem,
            _moveForwardMenuItem,
            _moveBackwardMenuItem,
            _sendToBackMenuItem
        ]);

        ToolStripMenuItem slideMenu = new("Diapositive");
        slideMenu.DropDownItems.Add(CreateMenuItem("Nouvelle diapositive", Keys.Insert, (_, _) => _presenter.AddSlide()));
        slideMenu.DropDownItems.Add(CreateMenuItem("Dupliquer", Keys.Control | Keys.D, (_, _) => _presenter.DuplicateSelectedSlide()));
        _deleteMenuItem.ShortcutKeys = Keys.Delete;
        _deleteMenuItem.Click += (_, _) => _presenter.DeleteSelectedSlide();
        slideMenu.DropDownItems.Add(_deleteMenuItem);

        ToolStripMenuItem slideshowMenu = new("Diaporama");
        slideshowMenu.DropDownItems.Add(CreateMenuItem(
            "Lancer la présentation",
            Keys.F5,
            StartPresentationSlideshow));
        slideshowMenu.DropDownItems.Add(new ToolStripSeparator());
        slideshowMenu.DropDownItems.Add(CreateMenuItem("Depuis des images…", Keys.None, StartSlideshowButton_Click));

        menu.Items.AddRange([fileMenu, editMenu, slideMenu, slideshowMenu]);
        MainMenuStrip = menu;
        _topPanel.Controls.Add(menu);
    }

    private void CreateToolbar()
    {
        _toolStrip.Dock = DockStyle.Bottom;
        _toolStrip.Height = 42;
        _toolStrip.Padding = new Padding(10, 4, 10, 4);
        _toolStrip.GripStyle = ToolStripGripStyle.Hidden;
        _toolStrip.Items.Add(CreateToolButton("Nouveau", (_, _) => _presenter.New()));
        _toolStrip.Items.Add(CreateToolButton("Ouvrir", (_, _) => _presenter.Open()));
        _saveButton.Click += (_, _) => _presenter.Save();
        _saveButton.DisplayStyle = ToolStripItemDisplayStyle.Text;
        _saveButton.BackColor = AccentColor;
        _saveButton.Margin = new Padding(6, 1, 8, 1);
        _saveButton.Padding = new Padding(10, 4, 10, 4);
        _toolStrip.Items.Add(_saveButton);
        _toolStrip.Items.Add(new ToolStripSeparator());
        _undoButton.Click += (_, _) => _presenter.Undo();
        _toolStrip.Items.Add(_undoButton);
        _redoButton.Click += (_, _) => _presenter.Redo();
        _toolStrip.Items.Add(_redoButton);
        _toolStrip.Items.Add(new ToolStripSeparator());
        _addSlideButton.Text = "Nouvelle diapositive";
        _addSlideButton.AutoSize = false;
        _addSlideButton.Size = new Size(154, 30);
        _addSlideButton.Click += (_, _) => _presenter.AddSlide();
        _toolStrip.Items.Add(new ToolStripControlHost(_addSlideButton));
        _duplicateSlideButton.Text = "Dupliquer";
        _duplicateSlideButton.AutoSize = false;
        _duplicateSlideButton.Size = new Size(100, 30);
        _duplicateSlideButton.Click += (_, _) => _presenter.DuplicateSelectedSlide();
        _toolStrip.Items.Add(new ToolStripControlHost(_duplicateSlideButton));
        _deleteSlideButton.Text = "Supprimer";
        _deleteSlideButton.AutoSize = false;
        _deleteSlideButton.Size = new Size(96, 30);
        _deleteSlideButton.Click += (_, _) => _presenter.DeleteSelectedSlide();
        _toolStrip.Items.Add(new ToolStripControlHost(_deleteSlideButton));
        _toolStrip.Items.Add(new ToolStripSeparator());
        ToolStripDropDownButton insertButton = new("Insérer");
        insertButton.DropDownItems.Add("Zone de texte", null, (_, _) => InsertCanvasElement(
            new TextElement(new ElementFrame(340, 280, 600, 140), "Double-cliquez pour modifier")));
        insertButton.DropDownItems.Add("Rectangle", null, (_, _) => InsertCanvasElement(
            new ShapeElement(ShapeKind.Rectangle, new ElementFrame(440, 250, 400, 220))));
        insertButton.DropDownItems.Add("Ellipse", null, (_, _) => InsertCanvasElement(
            new ShapeElement(ShapeKind.Ellipse, new ElementFrame(440, 250, 400, 220))));
        insertButton.DropDownItems.Add("Ligne", null, (_, _) => InsertCanvasElement(
            new ShapeElement(ShapeKind.Line, new ElementFrame(440, 250, 400, 220))));
        insertButton.DropDownItems.Add("Image…", null, (_, _) => InsertImage());
        _toolStrip.Items.Add(insertButton);
        _toolStrip.Items.Add(new ToolStripSeparator());
        ToolStripButton zoomOut = new("−")
        {
            ToolTipText = "Réduire le zoom du canevas",
            AccessibleName = "Réduire le zoom du canevas"
        };
        zoomOut.Click += (_, _) => _slideCanvas.ZoomBy(1 / 1.1f);
        _toolStrip.Items.Add(zoomOut);
        _zoomStatus.AccessibleName = "Niveau de zoom du canevas";
        _zoomStatus.ToolTipText = "Zoom du canevas (Ctrl + molette pour ajuster, bouton du milieu pour déplacer)";
        _toolStrip.Items.Add(_zoomStatus);
        ToolStripButton zoomIn = new("+")
        {
            ToolTipText = "Agrandir le zoom du canevas",
            AccessibleName = "Agrandir le zoom du canevas"
        };
        zoomIn.Click += (_, _) => _slideCanvas.ZoomBy(1.1f);
        _toolStrip.Items.Add(zoomIn);
        ToolStripButton resetZoom = new("Ajuster")
        {
            ToolTipText = "Ajuster la diapositive à la zone de travail",
            AccessibleName = "Ajuster la diapositive à la zone de travail"
        };
        resetZoom.Click += (_, _) => _slideCanvas.ResetZoom();
        _toolStrip.Items.Add(resetZoom);
        _toolStrip.Items.Add(new ToolStripSeparator());
        _toolStrip.Items.Add(CreateToolButton("Diaporama", StartPresentationSlideshow));
        _topPanel.Controls.Add(_toolStrip);
    }

    private void CreateDocumentHeader()
    {
        _documentHeader.Dock = DockStyle.Top;
        _documentHeader.Height = 36;
        _documentHeader.Padding = new Padding(12, 3, 12, 2);
        _documentHeader.Paint += (_, e) =>
        {
            using Pen divider = new(BorderColor);
            e.Graphics.DrawLine(divider, 0, _documentHeader.Height - 1, _documentHeader.Width, _documentHeader.Height - 1);
        };

        FlowLayoutPanel contents = new()
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = PanelColor,
            Margin = Padding.Empty
        };
        _brandMark.Size = new Size(26, 26);
        _brandMark.Margin = new Padding(0, 0, 8, 0);
        _documentHeaderTitle.AutoSize = false;
        _documentHeaderTitle.Size = new Size(360, 26);
        _documentHeaderTitle.AutoEllipsis = true;
        _documentHeaderTitle.Text = "Présentation sans titre";
        _documentHeaderTitle.Font = new Font("Segoe UI Semibold", 10, FontStyle.Bold);
        _documentHeaderTitle.ForeColor = PrimaryTextColor;
        _documentHeaderTitle.Margin = new Padding(0, 4, 8, 0);
        Label separator = new()
        {
            Text = "/",
            AutoSize = true,
            ForeColor = BorderColor,
            Margin = new Padding(0, 4, 8, 0)
        };
        _documentHeaderSubtitle.AutoSize = true;
        _documentHeaderSubtitle.Text = "Présentation SlideStudio";
        _documentHeaderSubtitle.ForeColor = MutedTextColor;
        _documentHeaderSubtitle.Font = new Font("Segoe UI", 9, FontStyle.Regular);
        _documentHeaderSubtitle.Margin = new Padding(0, 5, 0, 0);

        contents.Controls.Add(_brandMark);
        contents.Controls.Add(_documentHeaderTitle);
        contents.Controls.Add(separator);
        contents.Controls.Add(_documentHeaderSubtitle);
        _documentHeader.Controls.Add(contents);
        _topPanel.Controls.Add(_documentHeader);
    }

    private void CreateWorkspace()
    {
        SplitContainer outerSplit = new()
        {
            Size = new Size(1200, 700),
            Dock = DockStyle.Fill,
            FixedPanel = FixedPanel.Panel1,
            Panel1MinSize = 160,
            Panel2MinSize = 520,
            SplitterDistance = 260
        };
        _slideList.Dock = DockStyle.Fill;
        _slideList.AccessibleName = "Liste des diapositives";
        _slideList.AccessibleDescription = "Sélectionnez une diapositive avec les flèches du clavier.";
        _slideList.AccessibleRole = AccessibleRole.List;
        _slideList.DrawMode = DrawMode.OwnerDrawFixed;
        _slideList.ItemHeight = 102;
        _slideList.BorderStyle = BorderStyle.None;
        _slideList.IntegralHeight = false;
        _slideList.DrawItem += SlideList_DrawItem;
        _slideList.SelectedIndexChanged += SlideList_SelectedIndexChanged;
        _slideList.MouseMove += SlideList_Hover;
        _slideList.MouseLeave += SlideList_HoverEnd;
        _slideList.MouseDown += SlideList_MouseDown;
        _slideList.MouseMove += SlideList_MouseMove;
        _slideList.MouseUp += SlideList_MouseUp;
        _slideList.DragEnter += SlideList_DragEnter;
        _slideList.DragOver += SlideList_DragOver;
        _slideList.DragDrop += SlideList_DragDrop;
        _slideList.AllowDrop = true;
        outerSplit.Panel1.Controls.Add(_slideList);

        SplitContainer centerSplit = new()
        {
            Size = new Size(900, 700),
            Dock = DockStyle.Fill,
            FixedPanel = FixedPanel.Panel2,
            Panel1MinSize = 360,
            Panel2MinSize = 190,
            SplitterDistance = 650
        };
        Panel previewHost = new()
        {
            Dock = DockStyle.Fill,
            BackColor = WindowColor,
            Padding = new Padding(36)
        };
        _slideSurface.Dock = DockStyle.Fill;
        _slideSurface.BackColor = PanelColor;
        _slideSurface.SetStyle(12, BorderColor, 1);
        _slideSurface.Controls.Add(_slideCanvas);
        previewHost.Controls.Add(_slideSurface);
        centerSplit.Panel1.Controls.Add(previewHost);

        Panel properties = CreatePropertiesPanel();
        centerSplit.Panel2.Controls.Add(properties);
        outerSplit.Panel2.Controls.Add(centerSplit);
        Controls.Add(outerSplit);
    }

    private void CreateStatusBar()
    {
        _statusStrip.Items.Add(_modifiedStatus);
        ToolStripStatusLabel spacer = new() { Spring = true };
        _statusStrip.Items.Add(spacer);
        _statusStrip.Items.Add(_slidePositionStatus);
        _statusStrip.Items.Add(new ToolStripStatusLabel("|"));
        _statusStrip.Items.Add(_selectionStatus);
        _statusStrip.Items.Add(new ToolStripStatusLabel("|"));
        _statusStrip.Items.Add(new ToolStripStatusLabel("Glisser-déposer les diapositives pour les réordonner"));
        Controls.Add(_statusStrip);
    }

    private void ApplyDarkTheme(Control control)
    {
        control.BackColor = WindowColor;
        control.ForeColor = PrimaryTextColor;

        switch (control)
        {
            case Form:
                break;
            case MenuStrip menu:
                ApplyToolStripTheme(menu, PanelColor);
                break;
            case StatusStrip status:
                ApplyToolStripTheme(status, WindowColor);
                break;
            case ToolStrip toolStrip:
                ApplyToolStripTheme(toolStrip, PanelColor);
                break;
            case SplitContainer split:
                split.BackColor = BorderColor;
                split.Panel1.BackColor = PanelColor;
                split.Panel2.BackColor = WindowColor;
                break;
            case RoundedGroupBox group:
                group.BackColor = PanelColor;
                group.ForeColor = PrimaryTextColor;
                group.SetStyle(PanelColor, BorderColor);
                break;
            case Panel panel:
                panel.BackColor = ReferenceEquals(panel, _slideSurface)
                    ? panel.BackColor
                    : PanelColor;
                break;
            case Label label:
                label.ForeColor = MutedTextColor;
                if (label.Text == "Propriétés" || ReferenceEquals(label, _documentHeaderTitle))
                {
                    label.ForeColor = PrimaryTextColor;
                }
                break;
            case BrandMark:
                break;
            case AnimatedButton button:
                button.SetPalette(ElevatedColor, SelectionColor, AccentColor, BorderColor, PrimaryTextColor);
                if (ReferenceEquals(button, _addSlideButton)
                    || ReferenceEquals(button, _duplicateSlideButton))
                {
                    button.SetPalette(AccentColor, AccentHoverColor, Color.FromArgb(224, 75, 12), AccentColor, Color.White);
                }
                else if (ReferenceEquals(button, _deleteSlideButton))
                {
                    button.SetPalette(
                        Color.FromArgb(66, 35, 44),
                        Color.FromArgb(105, 42, 49),
                        Color.FromArgb(143, 49, 50),
                        Color.FromArgb(119, 54, 59),
                        PrimaryTextColor);
                }
                break;
            case Button button:
                button.FlatStyle = FlatStyle.Flat;
                button.FlatAppearance.BorderColor = BorderColor;
                button.FlatAppearance.MouseOverBackColor = SelectionColor;
                button.BackColor = ElevatedColor;
                button.ForeColor = PrimaryTextColor;
                break;
            case TextBox textBox:
                textBox.BackColor = ElevatedColor;
                textBox.ForeColor = PrimaryTextColor;
                textBox.BorderStyle = BorderStyle.FixedSingle;
                break;
            case ComboBox comboBox:
                comboBox.BackColor = ElevatedColor;
                comboBox.ForeColor = PrimaryTextColor;
                break;
            case NumericUpDown numeric:
                numeric.BackColor = ElevatedColor;
                numeric.ForeColor = PrimaryTextColor;
                break;
            case CheckBox checkBox:
                checkBox.BackColor = PanelColor;
                checkBox.ForeColor = PrimaryTextColor;
                checkBox.UseVisualStyleBackColor = false;
                break;
            case ListBox listBox:
                listBox.BackColor = PanelColor;
                listBox.ForeColor = PrimaryTextColor;
                break;
            default:
                control.BackColor = PanelColor;
                break;
        }

        if (control is SplitContainer container)
        {
            foreach (Control child in container.Panel1.Controls)
            {
                ApplyDarkTheme(child);
            }
            foreach (Control child in container.Panel2.Controls)
            {
                ApplyDarkTheme(child);
            }
        }
        else
        {
            foreach (Control child in control.Controls)
            {
                ApplyDarkTheme(child);
            }
        }

        if (control is ToolStrip toolStripControl)
        {
            ApplyToolStripTheme(toolStripControl, toolStripControl is StatusStrip ? WindowColor : PanelColor);
        }
    }

    private void ApplyToolStripTheme(ToolStrip strip, Color background)
    {
        strip.BackColor = background;
        strip.ForeColor = PrimaryTextColor;
        strip.Renderer = DarkToolStripRenderer;
        strip.GripStyle = ToolStripGripStyle.Hidden;
        foreach (ToolStripItem item in strip.Items)
        {
            item.BackColor = background;
            item.ForeColor = PrimaryTextColor;
            if (item is ToolStripDropDownItem dropDownItem)
            {
                dropDownItem.DropDown.BackColor = ElevatedColor;
                dropDownItem.DropDown.ForeColor = PrimaryTextColor;
                dropDownItem.DropDown.Renderer = DarkToolStripRenderer;
                foreach (ToolStripItem child in dropDownItem.DropDownItems)
                {
                    child.BackColor = ElevatedColor;
                    child.ForeColor = PrimaryTextColor;
                }
            }
            if (item is ToolStripControlHost host)
            {
                ApplyDarkTheme(host.Control);
            }
        }
    }

    private Panel CreatePropertiesPanel()
    {
        Panel properties = new()
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(10),
            BackColor = Color.White,
            AutoScroll = true
        };

        Label propertiesHeader = new()
        {
            Text = "Propriétés",
            Font = new Font("Segoe UI", 12, FontStyle.Bold),
            AutoSize = true,
            Dock = DockStyle.Top
        };
        Label titleLabel = CreatePropertyLabel("Nom de la présentation");
        _titleEditor.Dock = DockStyle.Top;
        _titleEditor.AccessibleName = "Nom de la présentation";
        _titleEditor.MaxLength = Limits.MaxTitleLength;
        _titleEditor.KeyDown += TitleEditor_KeyDown;
        _titleEditor.Leave += TitleEditor_Leave;
        Label slideBackgroundLabel = CreatePropertyLabel("Fond de la diapositive");
        _backgroundButton.Text = "Choisir une couleur…";
        _backgroundButton.TextAlign = ContentAlignment.MiddleLeft;
        _backgroundButton.Dock = DockStyle.Top;
        _backgroundButton.AccessibleName = "Choisir le fond de la diapositive";
        _backgroundButton.Height = 36;
        _backgroundButton.Click += BackgroundButton_Click;
        _slideCount.Dock = DockStyle.Top;
        _slideCount.AccessibleName = "Nombre de diapositives";
        _slideCount.AutoSize = true;
        _slideCount.Padding = new Padding(0, 10, 0, 0);

        RoundedGroupBox elementsGroup = new()
        {
            Text = "Éléments de la diapositive",
            Dock = DockStyle.Top,
            Height = 142,
            Padding = new Padding(8)
        };
        _elementList.Dock = DockStyle.Fill;
        _elementList.AccessibleName = "Éléments de la diapositive";
        _elementList.AccessibleDescription = "Sélection multiple possible avec Maj ou Ctrl.";
        _elementList.AccessibleRole = AccessibleRole.List;
        _elementList.IntegralHeight = false;
        _elementList.SelectionMode = SelectionMode.MultiExtended;
        _elementList.SelectedIndexChanged += ElementList_SelectedIndexChanged;
        _elementList.MouseDown += ElementList_MouseDown;
        _elementList.MouseUp += ElementList_MouseUp;
        elementsGroup.Controls.Add(_elementList);

        RoundedGroupBox textGroup = new()
        {
            Text = "Texte sélectionné",
            Dock = DockStyle.Top,
            Height = 214,
            Padding = new Padding(8)
        };
        TableLayoutPanel textLayout = CreatePropertyLayout(5);
        _fontFamilyEditor.DropDownStyle = ComboBoxStyle.DropDown;
        _fontFamilyEditor.AccessibleName = "Police du texte sélectionné";
        _fontFamilyEditor.Items.AddRange(["Segoe UI", "Arial", "Calibri", "Times New Roman", "Verdana"]);
        _fontFamilyEditor.SelectionChangeCommitted += FontFamilyEditor_Changed;
        _fontFamilyEditor.Leave += FontFamilyEditor_Changed;
        _textAlignmentEditor.DropDownStyle = ComboBoxStyle.DropDownList;
        _textAlignmentEditor.AccessibleName = "Alignement du texte sélectionné";
        _textAlignmentEditor.DisplayMember = nameof(AlignmentOption.Label);
        _textAlignmentEditor.Items.AddRange(Enum.GetValues<TextAlignment>()
            .Select(alignment => new AlignmentOption(alignment, GetAlignmentLabel(alignment)))
            .ToArray());
        _textAlignmentEditor.SelectionChangeCommitted += TextAlignmentEditor_Changed;
        _fontSizeEditor.Minimum = (decimal)Limits.MinFontSizePoints;
        _fontSizeEditor.AccessibleName = "Taille du texte sélectionné en points";
        _fontSizeEditor.Maximum = (decimal)Limits.MaxFontSizePoints;
        _fontSizeEditor.DecimalPlaces = 1;
        _fontSizeEditor.Increment = 1;
        _fontSizeEditor.ValueChanged += FontSizeEditor_ValueChanged;
        _boldEditor.Text = "Gras";
        _boldEditor.AccessibleName = "Mettre le texte en gras";
        _boldEditor.ThreeState = true;
        _italicEditor.Text = "Italique";
        _italicEditor.AccessibleName = "Mettre le texte en italique";
        _italicEditor.ThreeState = true;
        _boldEditor.CheckedChanged += (_, _) =>
        {
            if (!_updatingSelectionView && _boldEditor.Enabled && _boldEditor.CheckState != CheckState.Indeterminate)
            {
                _selectionPresenter?.SetBold(_boldEditor.Checked);
            }
        };
        _italicEditor.CheckedChanged += (_, _) =>
        {
            if (!_updatingSelectionView && _italicEditor.Enabled && _italicEditor.CheckState != CheckState.Indeterminate)
            {
                _selectionPresenter?.SetItalic(_italicEditor.Checked);
            }
        };
        AddPropertyRow(textLayout, 0, "Police", _fontFamilyEditor);
        AddPropertyRow(textLayout, 1, "Taille (pt)", _fontSizeEditor);
        AddPropertyRow(textLayout, 2, "Alignement", _textAlignmentEditor);
        AddPropertyRow(textLayout, 3, string.Empty, _boldEditor);
        AddPropertyRow(textLayout, 4, string.Empty, _italicEditor);
        textGroup.Controls.Add(textLayout);
        _textColorButton.Text = "Couleur du texte…";
        _textColorButton.AccessibleName = "Choisir la couleur du texte sélectionné";
        _textColorButton.Dock = DockStyle.Top;
        _textColorButton.Click += (_, _) => ChooseElementColor(_textColorButton, textSelection: true);

        RoundedGroupBox shapeGroup = new()
        {
            Text = "Forme sélectionnée",
            Dock = DockStyle.Top,
            Height = 142,
            Padding = new Padding(8)
        };
        _shapeFillButton.Text = "Remplissage…";
        _shapeFillButton.AccessibleName = "Choisir le remplissage de la forme sélectionnée";
        _shapeFillButton.Dock = DockStyle.Top;
        _shapeFillButton.Click += (_, _) => ChooseElementColor(_shapeFillButton, textSelection: false, fill: true);
        _shapeStrokeButton.Text = "Contour…";
        _shapeStrokeButton.AccessibleName = "Choisir le contour de la forme sélectionnée";
        _shapeStrokeButton.Dock = DockStyle.Top;
        _shapeStrokeButton.Click += (_, _) => ChooseElementColor(_shapeStrokeButton, textSelection: false, fill: false);
        _shapeStrokeWidthEditor.Minimum = 0;
        _shapeStrokeWidthEditor.AccessibleName = "Épaisseur du contour de la forme sélectionnée";
        _shapeStrokeWidthEditor.Maximum = (decimal)Limits.MaxStrokeWidth;
        _shapeStrokeWidthEditor.DecimalPlaces = 1;
        _shapeStrokeWidthEditor.Increment = 0.5m;
        _shapeStrokeWidthEditor.ValueChanged += (_, _) =>
        {
            if (!_updatingSelectionView && _shapeStrokeWidthEditor.Enabled)
            {
                _selectionPresenter?.SetShapeStrokeWidth((double)_shapeStrokeWidthEditor.Value);
            }
        };
        TableLayoutPanel shapeLayout = CreatePropertyLayout(3);
        AddPropertyRow(shapeLayout, 0, string.Empty, _shapeFillButton);
        AddPropertyRow(shapeLayout, 1, string.Empty, _shapeStrokeButton);
        AddPropertyRow(shapeLayout, 2, "Épaisseur", _shapeStrokeWidthEditor);
        shapeGroup.Controls.Add(shapeLayout);

        properties.Controls.Add(shapeGroup);
        properties.Controls.Add(_textColorButton);
        properties.Controls.Add(textGroup);
        properties.Controls.Add(elementsGroup);
        properties.Controls.Add(_slideCount);
        properties.Controls.Add(_backgroundButton);
        properties.Controls.Add(slideBackgroundLabel);
        properties.Controls.Add(_titleEditor);
        properties.Controls.Add(titleLabel);
        properties.Controls.Add(propertiesHeader);
        return properties;
    }

    private static Label CreatePropertyLabel(string text) => new()
    {
        Text = text,
        AutoSize = true,
        Padding = new Padding(0, 14, 0, 4),
        Dock = DockStyle.Top
    };

    private static TableLayoutPanel CreatePropertyLayout(int rows)
    {
        TableLayoutPanel layout = new()
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = rows,
            AutoSize = false
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 92));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        for (int row = 0; row < rows; row++)
        {
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f / rows));
        }
        return layout;
    }

    private static void AddPropertyRow(TableLayoutPanel layout, int row, string label, Control control)
    {
        Label caption = new() { Text = label, AutoSize = true, Anchor = AnchorStyles.Left };
        control.Dock = DockStyle.Fill;
        layout.Controls.Add(caption, 0, row);
        layout.Controls.Add(control, 1, row);
    }

    private static string GetAlignmentLabel(TextAlignment alignment) => alignment switch
    {
        TextAlignment.Left => "Gauche",
        TextAlignment.Center => "Centré",
        TextAlignment.Right => "Droite",
        _ => throw new ArgumentOutOfRangeException(nameof(alignment))
    };

    private static CheckState GetCommonCheckState(IEnumerable<bool> values)
    {
        bool[] selectedValues = values.ToArray();
        return selectedValues.All(value => value)
            ? CheckState.Checked
            : selectedValues.All(value => !value)
                ? CheckState.Unchecked
                : CheckState.Indeterminate;
    }

    private void RefreshElementList(Slide? slide, IReadOnlyList<SlideElement> selection)
    {
        bool wasUpdatingSelectionView = _updatingSelectionView;
        _updatingSelectionView = true;
        _elementList.BeginUpdate();
        try
        {
            _elementList.Items.Clear();
            if (slide is not null)
            {
                for (int index = 0; index < slide.Elements.Count; index++)
                {
                    _elementList.Items.Add(new ElementListItem(slide.Elements[index], index + 1));
                }
            }

            for (int index = 0; index < _elementList.Items.Count; index++)
            {
                if (_elementList.Items[index] is ElementListItem item
                    && selection.Contains(item.Element))
                {
                    _elementList.SetSelected(index, true);
                }
            }
        }
        finally
        {
            _elementList.EndUpdate();
            _updatingSelectionView = wasUpdatingSelectionView;
        }
    }

    private void SlideList_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (!_updatingView && _slideList.SelectedItem is SlideListItem item)
        {
            _presenter.SelectSlide(item.Slide);
        }
    }

    private void SlideList_DrawItem(object? sender, DrawItemEventArgs e)
    {
        bool isSelected = _slideList.SelectedIndex == e.Index;
        bool isHovered = _hoveredSlideIndex == e.Index;
        Rectangle row = Rectangle.Inflate(e.Bounds, -5, -2);
        using SolidBrush rowBrush = new(isSelected ? SelectionColor : isHovered ? ElevatedColor : PanelColor);
        DrawRoundedRectangle(e.Graphics, rowBrush, row, 10);
        if (e.Index >= 0 && _slideList.Items[e.Index] is SlideListItem item)
        {
            Rectangle thumbnail = new(e.Bounds.Left + 12, e.Bounds.Top + 8, e.Bounds.Width - 24, 70);
            using Pen borderPen = new(isSelected || isHovered ? AccentColor : BorderColor, isSelected ? 2.5f : 1);
            GraphicsState thumbnailState = e.Graphics.Save();
            using (GraphicsPath thumbnailClip = CreateRoundedPath(thumbnail, 7))
            {
                e.Graphics.SetClip(thumbnailClip);
                _thumbnailCache.Draw(e.Graphics, item.Slide, thumbnail);
            }
            e.Graphics.Restore(thumbnailState);
            DrawRoundedRectangle(e.Graphics, borderPen, thumbnail, 7);
            using SolidBrush labelBrush = new(PrimaryTextColor);
            e.Graphics.DrawString(
                $"Diapositive {item.Number}   ·   {item.Slide.Elements.Count} élément(s)",
                Font,
                labelBrush,
                e.Bounds.Left + 14,
                e.Bounds.Top + 81);
        }

        if (isSelected)
        {
            Rectangle marker = new(e.Bounds.Left + 2, e.Bounds.Top + 14, 3, 54);
            using SolidBrush markerBrush = new(AccentColor);
            DrawRoundedRectangle(e.Graphics, markerBrush, marker, 2);
        }
    }

    private void SlideList_Hover(object? sender, MouseEventArgs e)
    {
        int index = _slideList.IndexFromPoint(e.Location);
        if (_hoveredSlideIndex != index)
        {
            int previousIndex = _hoveredSlideIndex;
            _hoveredSlideIndex = index;
            if (previousIndex >= 0)
            {
                _slideList.Invalidate(_slideList.GetItemRectangle(previousIndex));
            }
            if (index >= 0)
            {
                _slideList.Invalidate(_slideList.GetItemRectangle(index));
            }
        }
    }

    private void SlideList_HoverEnd(object? sender, EventArgs e)
    {
        if (_hoveredSlideIndex >= 0)
        {
            int previousIndex = _hoveredSlideIndex;
            _hoveredSlideIndex = -1;
            _slideList.Invalidate(_slideList.GetItemRectangle(previousIndex));
        }
    }

    private void SlideList_MouseDown(object? sender, MouseEventArgs e)
    {
        _dragStartPoint = e.Location;
        int index = _slideList.IndexFromPoint(e.Location);
        if (e.Button == MouseButtons.Right)
        {
            _slideList.SelectedIndex = index;
        }
        _draggedSlide = index >= 0 && _slideList.Items[index] is SlideListItem item
            ? item.Slide
            : null;
    }

    private void SlideList_MouseUp(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Right || _slideList.IndexFromPoint(e.Location) < 0)
        {
            return;
        }

        ContextMenuStrip menu = new();
        menu.Renderer = DarkToolStripRenderer;
        menu.BackColor = ElevatedColor;
        menu.ForeColor = PrimaryTextColor;
        ToolStripMenuItem duplicate = new("Dupliquer");
        duplicate.Click += (_, _) => _presenter.DuplicateSelectedSlide();
        ToolStripMenuItem delete = new("Supprimer");
        delete.Click += (_, _) => _presenter.DeleteSelectedSlide();
        menu.Items.AddRange([duplicate, delete]);
        menu.Opening += (_, args) =>
        {
            duplicate.Enabled = _addSlideButton.Enabled;
            delete.Enabled = _deleteSlideButton.Enabled;
            args.Cancel = !duplicate.Enabled && !delete.Enabled;
        };
        menu.Closed += (_, _) => menu.Dispose();
        menu.Show(_slideList, e.Location);
    }

    private void SlideList_MouseMove(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left || _draggedSlide is null)
        {
            return;
        }

        Rectangle dragRectangle = new(
            _dragStartPoint.X - SystemInformation.DragSize.Width / 2,
            _dragStartPoint.Y - SystemInformation.DragSize.Height / 2,
            SystemInformation.DragSize.Width,
            SystemInformation.DragSize.Height);
        if (!dragRectangle.Contains(e.Location))
        {
            _slideList.DoDragDrop(_draggedSlide, DragDropEffects.Move);
        }
    }

    private void SlideList_DragEnter(object? sender, DragEventArgs e) =>
        e.Effect = e.Data?.GetDataPresent(typeof(Slide)) == true ? DragDropEffects.Move : DragDropEffects.None;

    private void SlideList_DragOver(object? sender, DragEventArgs e)
    {
        if (e.Data?.GetDataPresent(typeof(Slide)) != true)
        {
            e.Effect = DragDropEffects.None;
            return;
        }

        e.Effect = DragDropEffects.Move;
    }

    private void SlideList_DragDrop(object? sender, DragEventArgs e)
    {
        if (_draggedSlide is null || _slideList.Items.Count == 0)
        {
            return;
        }

        Point point = _slideList.PointToClient(new Point(e.X, e.Y));
        int targetIndex = _slideList.IndexFromPoint(point);
        if (targetIndex < 0)
        {
            targetIndex = _slideList.Items.Count - 1;
        }

        try
        {
            _presenter.MoveSlide(_draggedSlide, targetIndex);
            _presenter.SelectSlide(_draggedSlide);
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            ShowUiError(exception.Message);
        }
        finally
        {
            _draggedSlide = null;
        }
    }

    private void BackgroundButton_Click(object? sender, EventArgs e)
    {
        if (_presenter.SelectedSlide is not { } slide)
        {
            return;
        }

        using ColorDialog dialog = new()
        {
            Color = ToDrawingColor(slide.Background),
            FullOpen = true
        };
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        ArgbColor color = ArgbColor.FromRgb(dialog.Color.R, dialog.Color.G, dialog.Color.B);
        _presenter.History.Execute(EditCommands.SetSlideBackground(slide, color));
    }

    private void ElementList_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (_updatingSelectionView || _selectionPresenter is null)
        {
            return;
        }

        _selectionPresenter.SetSelection(_elementList.SelectedItems
            .OfType<ElementListItem>()
            .Select(item => item.Element));
    }

    private void ElementList_MouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Right)
        {
            return;
        }

        int index = _elementList.IndexFromPoint(e.Location);
        if (index < 0)
        {
            _elementList.ClearSelected();
        }
        else if (!_elementList.GetSelected(index))
        {
            _elementList.SetSelected(index, true);
        }
    }

    private void ElementList_MouseUp(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Right)
        {
            return;
        }

        ContextMenuStrip menu = new();
        menu.Renderer = DarkToolStripRenderer;
        menu.BackColor = ElevatedColor;
        menu.ForeColor = PrimaryTextColor;
        menu.Items.AddRange(
        [
            CreateSelectionContextItem(_copyElementMenuItem),
            CreateSelectionContextItem(_cutElementMenuItem),
            CreateSelectionContextItem(_pasteElementMenuItem),
            new ToolStripSeparator(),
            CreateSelectionContextItem(_duplicateElementMenuItem),
            CreateSelectionContextItem(_deleteElementMenuItem),
            new ToolStripSeparator(),
            CreateSelectionContextItem(_bringToFrontMenuItem),
            CreateSelectionContextItem(_moveForwardMenuItem),
            CreateSelectionContextItem(_moveBackwardMenuItem),
            CreateSelectionContextItem(_sendToBackMenuItem)
        ]);
        menu.Closed += (_, _) => menu.Dispose();
        menu.Show(_elementList, e.Location);
    }

    private void FontFamilyEditor_Changed(object? sender, EventArgs e)
    {
        if (_updatingSelectionView || !_fontFamilyEditor.Enabled || _selectionPresenter is null
            || string.IsNullOrWhiteSpace(_fontFamilyEditor.Text))
        {
            return;
        }

        try
        {
            _selectionPresenter.SetFontFamily(_fontFamilyEditor.Text.Trim());
        }
        catch (DocumentValidationException exception)
        {
            ShowUiError(exception.Message);
            SetSelection(_selectionPresenter.Selection);
        }
    }

    private void FontSizeEditor_ValueChanged(object? sender, EventArgs e)
    {
        if (!_updatingSelectionView && _fontSizeEditor.Enabled)
        {
            _selectionPresenter?.SetFontSize((double)_fontSizeEditor.Value);
        }
    }

    private void TextAlignmentEditor_Changed(object? sender, EventArgs e)
    {
        if (!_updatingSelectionView
            && _textAlignmentEditor.Enabled
            && _textAlignmentEditor.SelectedItem is AlignmentOption option)
        {
            _selectionPresenter?.SetTextAlignment(option.Alignment);
        }
    }

    private void ChooseElementColor(AnimatedButton colorButton, bool textSelection, bool? fill = null)
    {
        IReadOnlyList<SlideElement>? selection = _selectionPresenter?.Selection;
        if (selection is not { Count: > 0 }
            || (textSelection && selection.Any(element => element is not TextElement))
            || (!textSelection && selection.Any(element => element is not ShapeElement)))
        {
            return;
        }

        using ColorDialog dialog = new()
        {
            Color = colorButton.BackColor,
            FullOpen = true
        };
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        colorButton.BackColor = dialog.Color;
        ArgbColor color = ArgbColor.FromRgb(dialog.Color.R, dialog.Color.G, dialog.Color.B);
        if (textSelection)
        {
            colorButton.SetSwatchColor(dialog.Color);
            _selectionPresenter?.SetTextColor(color);
        }
        else if (fill == true)
        {
            colorButton.SetSwatchColor(dialog.Color);
            _selectionPresenter?.SetShapeFill(color);
        }
        else
        {
            colorButton.SetSwatchColor(dialog.Color);
            _selectionPresenter?.SetShapeStroke(color);
        }
    }

    private void TitleEditor_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Enter)
        {
            ApplyTitle();
            e.SuppressKeyPress = true;
        }
    }

    private void TitleEditor_Leave(object? sender, EventArgs e) => ApplyTitle();

    private void ApplyTitle()
    {
        if (_updatingView)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(_titleEditor.Text))
        {
            _titleEditor.Text = _presenter.Presentation.Title;
            return;
        }

        if (_titleEditor.Text == _presenter.Presentation.Title)
        {
            return;
        }

        try
        {
            _presenter.History.Execute(EditCommands.SetTitle(_presenter.Presentation, _titleEditor.Text));
        }
        catch (DocumentValidationException exception)
        {
            ShowUiError(exception.Message);
            _titleEditor.Text = _presenter.Presentation.Title;
        }
    }

    private void Form1_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Delete && _elementList.Focused
            && _selectionPresenter?.Selection.Count > 0)
        {
            _selectionPresenter.Delete();
            e.SuppressKeyPress = true;
            e.Handled = true;
            return;
        }

        if (e.KeyCode == Keys.F5)
        {
            StartPresentationSlideshow(sender, EventArgs.Empty);
            e.SuppressKeyPress = true;
        }
    }

    private void Form1_FormClosing(object? sender, FormClosingEventArgs e)
    {
        if (!_presenter.Close())
        {
            e.Cancel = true;
        }
    }

    private void StartSlideshowButton_Click(object? sender, EventArgs e)
    {
        using OpenFileDialog dialog = new()
        {
            Title = "Choisir les images du diaporama",
            Filter = "Images|*.png;*.jpg;*.jpeg;*.bmp;*.gif|Tous les fichiers|*.*",
            Multiselect = true,
            CheckFileExists = true
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        List<Image> images = [];
        try
        {
            foreach (string fileName in dialog.FileNames)
            {
                using Image source = Image.FromFile(fileName);
                images.Add(new Bitmap(source));
            }

            using SlideshowForm slideshow = new(images);
            slideshow.ShowDialog(this);
        }
        catch (Exception exception) when (
            exception is IOException
                or UnauthorizedAccessException
                or ArgumentException
                or OutOfMemoryException)
        {
            ShowUiError($"Impossible de charger les images du diaporama : {exception.Message}");
        }
        finally
        {
            foreach (Image image in images)
            {
                image.Dispose();
            }
        }
    }

    private void StartPresentationSlideshow(object? sender, EventArgs e)
    {
        try
        {
            using SlideshowForm slideshow = new(_presenter.Presentation.Slides, _presenter.MediaStore);
            slideshow.ShowDialog(this);
        }
        catch (Exception exception) when (
            exception is ArgumentException
                or DocumentValidationException
                or ExternalException
                or InvalidOperationException
                or KeyNotFoundException
                or OutOfMemoryException)
        {
            ShowUiError($"Impossible de lancer le diaporama : {exception.Message}");
        }
    }

    private void ExportSlidesAsPng(object? sender, EventArgs e)
    {
        using FolderBrowserDialog dialog = new()
        {
            Description = "Choisissez le dossier de destination des diapositives PNG.",
            ShowNewFolderButton = true
        };
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        try
        {
            string[] outputPaths = SlidePngExporter.GetOutputPaths(_presenter.Presentation, dialog.SelectedPath);
            bool overwrite = outputPaths.Any(File.Exists);
            if (overwrite
                && MessageBox.Show(
                    this,
                    $"Un ou plusieurs fichiers PNG existent déjà. Voulez-vous les remplacer ?",
                    "Confirmer le remplacement",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning,
                    MessageBoxDefaultButton.Button2) != DialogResult.Yes)
            {
                return;
            }

            string[] exportedPaths = SlidePngExporter.Export(
                _presenter.Presentation,
                _presenter.MediaStore,
                dialog.SelectedPath,
                overwrite);
            MessageBox.Show(
                this,
                $"{exportedPaths.Length} diapositive(s) exportée(s) en PNG dans :\n{dialog.SelectedPath}",
                "Export terminé",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
        catch (Exception exception) when (
            exception is ArgumentException
                or DocumentValidationException
                or ExternalException
                or IOException
                or InvalidOperationException
                or KeyNotFoundException
                or UnauthorizedAccessException
                or OutOfMemoryException)
        {
            ShowUiError($"Impossible d'exporter les diapositives : {exception.Message}");
        }
    }

    private void ExportPresentationAsPdf(object? sender, EventArgs e)
    {
        using SaveFileDialog dialog = new()
        {
            Title = "Exporter la présentation en PDF",
            Filter = "Document PDF|*.pdf",
            AddExtension = true,
            DefaultExt = "pdf",
            OverwritePrompt = true,
            FileName = "presentation.pdf"
        };
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        try
        {
            string directory = Path.GetDirectoryName(dialog.FileName)
                ?? throw new ArgumentException("Le chemin du fichier PDF est invalide.");
            string outputPath = SlidePdfExporter.Export(
                _presenter.Presentation,
                _presenter.MediaStore,
                directory,
                overwrite: true);
            MessageBox.Show(
                this,
                $"Présentation exportée en PDF :\n{outputPath}",
                "Export terminé",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
        catch (Exception exception) when (
            exception is ArgumentException
                or DocumentValidationException
                or ExternalException
                or IOException
                or InvalidOperationException
                or KeyNotFoundException
                or UnauthorizedAccessException
                or OutOfMemoryException)
        {
            ShowUiError($"Impossible d'exporter le PDF : {exception.Message}");
        }
    }

    private void ShowUiError(string message) =>
        MessageBox.Show(this, message, "SlideStudio", MessageBoxButtons.OK, MessageBoxIcon.Error);

    private static Color ToDrawingColor(ArgbColor color) => Color.FromArgb(color.A, color.R, color.G, color.B);

    private static void DrawRoundedRectangle(Graphics graphics, Brush brush, Rectangle bounds, int radius)
    {
        using GraphicsPath path = CreateRoundedPath(bounds, radius);
        graphics.FillPath(brush, path);
    }

    private static void DrawRoundedRectangle(Graphics graphics, Pen pen, Rectangle bounds, int radius)
    {
        using GraphicsPath path = CreateRoundedPath(bounds, radius);
        graphics.DrawPath(pen, path);
    }

    private static GraphicsPath CreateRoundedPath(Rectangle bounds, int radius)
    {
        GraphicsPath path = new();
        int diameter = Math.Min(radius * 2, Math.Min(bounds.Width, bounds.Height));
        if (diameter <= 0)
        {
            path.AddRectangle(bounds);
            return path;
        }

        Rectangle arc = new(bounds.Left, bounds.Top, diameter, diameter);
        path.AddArc(arc, 180, 90);
        arc.X = bounds.Right - diameter;
        path.AddArc(arc, 270, 90);
        arc.Y = bounds.Bottom - diameter;
        path.AddArc(arc, 0, 90);
        arc.X = bounds.Left;
        path.AddArc(arc, 90, 90);
        path.CloseFigure();
        return path;
    }

    private static ToolStripMenuItem CreateMenuItem(string text, Keys shortcut, EventHandler handler)
    {
        ToolStripMenuItem item = new(text);
        if (shortcut != Keys.None)
        {
            item.ShortcutKeys = shortcut;
        }
        item.Click += handler;
        return item;
    }

    private static ToolStripButton CreateToolButton(string text, EventHandler handler)
    {
        ToolStripButton button = new(text)
        {
            DisplayStyle = ToolStripItemDisplayStyle.Text,
            Margin = new Padding(3, 1, 3, 1),
            Padding = new Padding(8, 4, 8, 4)
        };
        button.Click += handler;
        return button;
    }

    private static void ConfigureSelectionMenuItem(
        ToolStripMenuItem menuItem,
        Keys shortcut,
        Action action)
    {
        if (shortcut != Keys.None)
        {
            menuItem.ShortcutKeys = shortcut;
        }
        menuItem.Click += (_, _) => action();
    }

    private static ToolStripMenuItem CreateSelectionContextItem(ToolStripMenuItem source)
    {
        ToolStripMenuItem item = new(source.Text) { Enabled = source.Enabled };
        item.Click += (_, _) => source.PerformClick();
        return item;
    }

    private sealed record SlideListItem(Slide Slide, int Number)
    {
        public override string ToString() => $"Diapositive {Number}";
    }

    private sealed record ElementListItem(SlideElement Element, int Number)
    {
        public override string ToString() => Element switch
        {
            TextElement text => $"{Number}. Texte : {Truncate(text.Text, 24)}",
            ShapeElement shape => $"{Number}. {shape.Kind}",
            _ => $"{Number}. {Element.GetType().Name}"
        };

        private static string Truncate(string value, int length) =>
            value.Length <= length ? value : value[..(length - 1)] + "…";
    }

    private sealed record AlignmentOption(TextAlignment Alignment, string Label);

    private sealed class BrandMark : Control
    {
        public BrandMark()
        {
            SetStyle(
                ControlStyles.UserPaint
                    | ControlStyles.AllPaintingInWmPaint
                    | ControlStyles.OptimizedDoubleBuffer
                    | ControlStyles.ResizeRedraw,
                true);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle bounds = Rectangle.Inflate(ClientRectangle, -1, -1);
            using LinearGradientBrush background = new(bounds, AccentHoverColor, AccentColor, LinearGradientMode.ForwardDiagonal);
            e.Graphics.FillEllipse(background, bounds);
            using Pen highlight = new(Color.FromArgb(110, Color.White), 1);
            e.Graphics.DrawEllipse(highlight, bounds);
            using Font markFont = new("Segoe UI", 13, FontStyle.Bold);
            TextRenderer.DrawText(
                e.Graphics,
                "S",
                markFont,
                bounds,
                Color.White,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }
    }

    private sealed class AnimatedButton : Button
    {
        private readonly System.Windows.Forms.Timer _animationTimer = new() { Interval = 15 };
        private Color _baseColor = ElevatedColor;
        private Color _hoverColor = SelectionColor;
        private Color _pressedColor = AccentColor;
        private Color _borderColor = BorderColor;
        private Color _textColor = PrimaryTextColor;
        private Color _swatchColor;
        private float _visualState;
        private float _targetState;
        private bool _showSwatch;

        public AnimatedButton()
        {
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            UseVisualStyleBackColor = false;
            SetStyle(
                ControlStyles.UserPaint
                    | ControlStyles.AllPaintingInWmPaint
                    | ControlStyles.OptimizedDoubleBuffer
                    | ControlStyles.ResizeRedraw,
                true);
            _animationTimer.Tick += AnimationTimer_Tick;
        }

        public void SetPalette(Color baseColor, Color hoverColor, Color pressedColor, Color borderColor, Color textColor)
        {
            _baseColor = baseColor;
            _hoverColor = hoverColor;
            _pressedColor = pressedColor;
            _borderColor = borderColor;
            _textColor = textColor;
            Invalidate();
        }

        public void SetSwatchColor(Color color)
        {
            _swatchColor = color;
            _showSwatch = true;
            Invalidate();
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            AnimateTo(1);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            AnimateTo(0);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button == MouseButtons.Left && Enabled)
            {
                AnimateTo(2);
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            AnimateTo(ClientRectangle.Contains(PointToClient(Cursor.Position)) ? 1 : 0);
        }

        protected override void OnEnabledChanged(EventArgs e)
        {
            base.OnEnabledChanged(e);
            Invalidate();
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle bounds = Rectangle.Inflate(ClientRectangle, -1, -1);
            if (bounds.Width < 2 || bounds.Height < 2)
            {
                return;
            }

            Color fill = GetAnimatedColor(_baseColor, _hoverColor, _pressedColor, _visualState);
            Color border = Enabled ? ColorBlend(_borderColor, _hoverColor, Math.Min(_visualState, 1) * 0.65f) : BorderColor;
            if (!Enabled)
            {
                fill = Color.FromArgb(39, 43, 55);
            }

            using GraphicsPath path = CreateRoundedPath(bounds, 9);
            using LinearGradientBrush fillBrush = new(bounds, Lighten(fill, 0.06f), fill, LinearGradientMode.Vertical);
            e.Graphics.FillPath(fillBrush, path);
            using Pen borderPen = new(border, Focused ? 1.8f : 1f);
            e.Graphics.DrawPath(borderPen, path);

            if (Focused)
            {
                Rectangle focusBounds = Rectangle.Inflate(bounds, -4, -4);
                using GraphicsPath focusPath = CreateRoundedPath(focusBounds, 6);
                using Pen focusPen = new(Color.FromArgb(110, AccentHoverColor), 1f) { DashPattern = [2, 2] };
                e.Graphics.DrawPath(focusPen, focusPath);
            }

            Rectangle textBounds = bounds;
            if (_showSwatch)
            {
                Rectangle swatch = new(bounds.Left + 10, bounds.Top + (bounds.Height - 12) / 2, 12, 12);
                using SolidBrush swatchBrush = new(_swatchColor);
                e.Graphics.FillEllipse(swatchBrush, swatch);
                using Pen swatchBorder = new(Color.FromArgb(145, Color.White), 1);
                e.Graphics.DrawEllipse(swatchBorder, swatch);
                textBounds = Rectangle.FromLTRB(swatch.Right + 4, bounds.Top, bounds.Right - 5, bounds.Bottom);
            }

            TextRenderer.DrawText(
                e.Graphics,
                Text,
                Font,
                textBounds,
                Enabled ? _textColor : MutedTextColor,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _animationTimer.Dispose();
            }
            base.Dispose(disposing);
        }

        private void AnimateTo(float state)
        {
            _targetState = state;
            _animationTimer.Start();
        }

        private void AnimationTimer_Tick(object? sender, EventArgs e)
        {
            float delta = _targetState - _visualState;
            if (Math.Abs(delta) < 0.06f)
            {
                _visualState = _targetState;
                _animationTimer.Stop();
            }
            else
            {
                _visualState += delta * 0.35f;
            }
            Invalidate();
        }

        private static Color GetAnimatedColor(Color normal, Color hover, Color pressed, float state) =>
            state <= 1
                ? ColorBlend(normal, hover, state)
                : ColorBlend(hover, pressed, state - 1);

        private static Color Lighten(Color color, float amount) =>
            Color.FromArgb(
                color.A,
                Math.Min(255, (int)(color.R + (255 - color.R) * amount)),
                Math.Min(255, (int)(color.G + (255 - color.G) * amount)),
                Math.Min(255, (int)(color.B + (255 - color.B) * amount)));

        private static Color ColorBlend(Color first, Color second, float amount)
        {
            amount = Math.Clamp(amount, 0, 1);
            return Color.FromArgb(
                (int)(first.A + ((second.A - first.A) * amount)),
                (int)(first.R + ((second.R - first.R) * amount)),
                (int)(first.G + ((second.G - first.G) * amount)),
                (int)(first.B + ((second.B - first.B) * amount)));
        }
    }

    private sealed class RoundedPanel : Panel
    {
        private int _cornerRadius = 12;
        private Color _borderColor = Form1.BorderColor;
        private float _borderThickness = 1;

        public RoundedPanel()
        {
            SetStyle(
                ControlStyles.UserPaint
                    | ControlStyles.AllPaintingInWmPaint
                    | ControlStyles.OptimizedDoubleBuffer
                    | ControlStyles.ResizeRedraw,
                true);
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            using SolidBrush parentBrush = new(Parent?.BackColor ?? WindowColor);
            e.Graphics.FillRectangle(parentBrush, ClientRectangle);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle bounds = Rectangle.Inflate(ClientRectangle, -1, -1);
            using GraphicsPath path = CreateRoundedPath(bounds, _cornerRadius);
            using SolidBrush brush = new(BackColor);
            e.Graphics.FillPath(brush, path);
            if (_borderThickness > 0)
            {
                using Pen border = new(_borderColor, _borderThickness);
                e.Graphics.DrawPath(border, path);
            }
        }

        public void SetStyle(int cornerRadius, Color borderColor, float borderThickness)
        {
            _cornerRadius = cornerRadius;
            _borderColor = borderColor;
            _borderThickness = borderThickness;
            Invalidate();
        }
    }

    private sealed class RoundedGroupBox : GroupBox
    {
        private Color _surfaceColor = PanelColor;
        private Color _borderColor = Form1.BorderColor;
        private int _cornerRadius = 10;

        public RoundedGroupBox()
        {
            SetStyle(
                ControlStyles.UserPaint
                    | ControlStyles.AllPaintingInWmPaint
                    | ControlStyles.OptimizedDoubleBuffer
                    | ControlStyles.ResizeRedraw,
                true);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle bounds = Rectangle.Inflate(ClientRectangle, -1, -1);
            using GraphicsPath path = CreateRoundedPath(bounds, _cornerRadius);
            using SolidBrush background = new(_surfaceColor);
            e.Graphics.FillPath(background, path);
            using Pen border = new(_borderColor, 1);
            e.Graphics.DrawPath(border, path);

            Size titleSize = TextRenderer.MeasureText(Text, Font);
            Rectangle titleBackground = new(14, 0, Math.Max(0, titleSize.Width + 10), titleSize.Height);
            using SolidBrush titleBrush = new(_surfaceColor);
            e.Graphics.FillRectangle(titleBrush, titleBackground);
            TextRenderer.DrawText(
                e.Graphics,
                Text,
                Font,
                new Point(19, 0),
                PrimaryTextColor,
                TextFormatFlags.NoPadding | TextFormatFlags.EndEllipsis);
        }

        public void SetStyle(Color surfaceColor, Color borderColor)
        {
            _surfaceColor = surfaceColor;
            _borderColor = borderColor;
            Invalidate();
        }
    }

    private sealed class DarkRenderer : ToolStripProfessionalRenderer
    {
        protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
        {
            using SolidBrush brush = new(e.ToolStrip.BackColor);
            e.Graphics.FillRectangle(brush, e.AffectedBounds);
        }

        protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
        {
            if (!e.Item.Selected && !e.Item.Pressed)
            {
                return;
            }

            using SolidBrush brush = new(e.Item.Pressed ? AccentColor : SelectionColor);
            Rectangle bounds = Rectangle.Inflate(new Rectangle(Point.Empty, e.Item.Size), -3, -1);
            DrawRoundedRectangle(e.Graphics, brush, bounds, 6);
        }

        protected override void OnRenderButtonBackground(ToolStripItemRenderEventArgs e)
        {
            Rectangle bounds = Rectangle.Inflate(new Rectangle(Point.Empty, e.Item.Size), -2, -2);
            if (e.Item is ToolStripButton { Text: "Enregistrer" })
            {
                using SolidBrush accentBrush = new(e.Item.Pressed ? Color.FromArgb(224, 75, 12) : AccentColor);
                DrawRoundedRectangle(e.Graphics, accentBrush, bounds, 7);
                return;
            }

            if (!e.Item.Selected && !e.Item.Pressed)
            {
                return;
            }

            using SolidBrush brush = new(e.Item.Pressed ? AccentColor : SelectionColor);
            DrawRoundedRectangle(e.Graphics, brush, bounds, 6);
        }

        protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
        {
            Rectangle bounds = e.Item.ContentRectangle;
            using Pen pen = new(BorderColor);
            if (e.Vertical)
            {
                int x = bounds.Left + bounds.Width / 2;
                e.Graphics.DrawLine(pen, x, bounds.Top + 3, x, bounds.Bottom - 3);
            }
            else
            {
                int y = bounds.Top + bounds.Height / 2;
                e.Graphics.DrawLine(pen, bounds.Left + 4, y, bounds.Right - 4, y);
            }
        }
    }

    private static class NativeMethods
    {
        [DllImport("dwmapi.dll")]
        internal static extern int DwmSetWindowAttribute(
            IntPtr window,
            int attribute,
            ref int value,
            int valueSize);
    }

    private sealed class WinFormsDialogService(Form owner) : IDialogService
    {
        public string? ChooseOpenFile()
        {
            using OpenFileDialog dialog = new()
            {
                Title = "Ouvrir une présentation",
                Filter = "Présentations SlideStudio (*.slds)|*.slds",
                CheckFileExists = true
            };
            return dialog.ShowDialog(owner) == DialogResult.OK ? dialog.FileName : null;
        }

        public string? ChooseSaveFile(string suggestedName)
        {
            using SaveFileDialog dialog = new()
            {
                Title = "Enregistrer la présentation",
                Filter = "Présentations SlideStudio (*.slds)|*.slds",
                DefaultExt = "slds",
                AddExtension = true,
                FileName = suggestedName,
                OverwritePrompt = true
            };
            return dialog.ShowDialog(owner) == DialogResult.OK ? dialog.FileName : null;
        }

        public UnsavedChangesChoice ConfirmUnsavedChanges(string documentTitle)
        {
            DialogResult result = MessageBox.Show(
                owner,
                $"Enregistrer les modifications apportées à « {documentTitle} » ?",
                "Modifications non enregistrées",
                MessageBoxButtons.YesNoCancel,
                MessageBoxIcon.Warning);
            return result switch
            {
                DialogResult.Yes => UnsavedChangesChoice.Save,
                DialogResult.No => UnsavedChangesChoice.Discard,
                _ => UnsavedChangesChoice.Cancel
            };
        }

        public void ShowError(string title, string message) =>
            MessageBox.Show(owner, message, title, MessageBoxButtons.OK, MessageBoxIcon.Error);
    }
}
