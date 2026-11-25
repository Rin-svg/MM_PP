using SlideStudio.Core;
using SlideStudio.Core.Editing;
using SlideStudio.Core.Model;
using SlideStudio.Infrastructure;
using SlideStudio.Presenters;

namespace SlideStudio.UI;

/// <summary>Fenêtre principale de SlideStudio, reliée aux services et au présentateur M3.</summary>
public partial class Form1 : Form, IMainView
{
    private readonly ListBox _slideList = new();
    private readonly Panel _slideSurface = new();
    private readonly Label _previewMessage = new();
    private readonly Label _slideCount = new();
    private readonly ToolStripStatusLabel _modifiedStatus = new();
    private readonly TextBox _titleEditor = new();
    private readonly Button _backgroundButton = new();
    private readonly Button _addSlideButton = new();
    private readonly Button _duplicateSlideButton = new();
    private readonly Button _deleteSlideButton = new();
    private readonly ToolStripButton _saveButton = new("Enregistrer");
    private readonly ToolStripButton _undoButton = new("Annuler");
    private readonly ToolStripButton _redoButton = new("Rétablir");
    private readonly Panel _topPanel = new();
    private readonly ToolStripMenuItem _saveMenuItem = new("Enregistrer");
    private readonly ToolStripMenuItem _undoMenuItem = new("Annuler");
    private readonly ToolStripMenuItem _redoMenuItem = new("Rétablir");
    private readonly ToolStripMenuItem _deleteMenuItem = new("Supprimer la diapositive");
    private readonly ToolStrip _toolStrip = new();
    private readonly StatusStrip _statusStrip = new();
    private readonly MainPresenter _presenter;
    private bool _updatingView;
    private Slide? _draggedSlide;
    private Point _dragStartPoint;

    /// <summary>Crée la fenêtre et connecte l'interface aux Presenters et au stockage .slds.</summary>
    public Form1()
    {
        InitializeComponent();
        BuildInterface();
        _presenter = new MainPresenter(this, new WinFormsDialogService(this), new SldsDocumentStorage());
    }

    /// <inheritdoc />
    public void SetDocument(Presentation presentation, Slide? selectedSlide)
    {
        _updatingView = true;
        try
        {
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
            _previewMessage.Text = selectedSlide is null
                ? "Aucune diapositive sélectionnée"
                : $"Diapositive {_slideList.SelectedIndex + 1}\n{selectedSlide.Elements.Count} élément(s)\n\n"
                    + "Le canevas et le rendu des objets arrivent avec le jalon M5.";
            _slideSurface.BackColor = selectedSlide is null
                ? Color.White
                : ToDrawingColor(selectedSlide.Background);
            _backgroundButton.BackColor = _slideSurface.BackColor;
            _backgroundButton.Enabled = selectedSlide is not null;
            _previewMessage.ForeColor = ContrastTextColor(_slideSurface.BackColor);
            _slideSurface.Invalidate();
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
        if (!_titleEditor.Focused)
        {
            _titleEditor.Text = title.EndsWith(" *", StringComparison.Ordinal)
                ? title[..^2]
                : title;
        }
    }

    /// <inheritdoc />
    public void SetModified(bool isModified)
    {
        _modifiedStatus.Text = isModified ? "Modifications non enregistrées" : "Enregistré";
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
        _topPanel.Height = 54;
        Controls.Add(_topPanel);
        Controls.SetChildIndex(_topPanel, 0);
        Controls.SetChildIndex(_statusStrip, 1);
        KeyDown += Form1_KeyDown;
        FormClosing += Form1_FormClosing;
    }

    private void CreateMenus()
    {
        MenuStrip menu = new();
        menu.Dock = DockStyle.Top;
        ToolStripMenuItem fileMenu = new("Fichier");
        fileMenu.DropDownItems.Add(CreateMenuItem("Nouveau", Keys.Control | Keys.N, (_, _) => _presenter.New()));
        fileMenu.DropDownItems.Add(CreateMenuItem("Ouvrir…", Keys.Control | Keys.O, (_, _) => _presenter.Open()));
        fileMenu.DropDownItems.Add(new ToolStripSeparator());
        fileMenu.DropDownItems.Add(_saveMenuItem);
        _saveMenuItem.ShortcutKeys = Keys.Control | Keys.S;
        _saveMenuItem.Click += (_, _) => _presenter.Save();
        fileMenu.DropDownItems.Add(CreateMenuItem("Enregistrer sous…", Keys.Control | Keys.Shift | Keys.S, (_, _) => _presenter.SaveAs()));
        fileMenu.DropDownItems.Add(new ToolStripSeparator());
        fileMenu.DropDownItems.Add(CreateMenuItem("Quitter", Keys.Alt | Keys.F4, (_, _) => Close()));

        ToolStripMenuItem editMenu = new("Édition");
        _undoMenuItem.ShortcutKeys = Keys.Control | Keys.Z;
        _undoMenuItem.Click += (_, _) => _presenter.Undo();
        editMenu.DropDownItems.Add(_undoMenuItem);
        _redoMenuItem.ShortcutKeys = Keys.Control | Keys.Y;
        _redoMenuItem.Click += (_, _) => _presenter.Redo();
        editMenu.DropDownItems.Add(_redoMenuItem);

        ToolStripMenuItem slideMenu = new("Diapositive");
        slideMenu.DropDownItems.Add(CreateMenuItem("Nouvelle diapositive", Keys.Insert, (_, _) => _presenter.AddSlide()));
        slideMenu.DropDownItems.Add(CreateMenuItem("Dupliquer", Keys.Control | Keys.D, (_, _) => _presenter.DuplicateSelectedSlide()));
        _deleteMenuItem.ShortcutKeys = Keys.Delete;
        _deleteMenuItem.Click += (_, _) => _presenter.DeleteSelectedSlide();
        slideMenu.DropDownItems.Add(_deleteMenuItem);

        ToolStripMenuItem slideshowMenu = new("Diaporama");
        slideshowMenu.DropDownItems.Add(CreateMenuItem("Depuis des images…", Keys.None, StartSlideshowButton_Click));

        menu.Items.AddRange([fileMenu, editMenu, slideMenu, slideshowMenu]);
        MainMenuStrip = menu;
        _topPanel.Controls.Add(menu);
    }

    private void CreateToolbar()
    {
        _toolStrip.Dock = DockStyle.Bottom;
        _toolStrip.GripStyle = ToolStripGripStyle.Hidden;
        _toolStrip.Items.Add(CreateToolButton("Nouveau", (_, _) => _presenter.New()));
        _toolStrip.Items.Add(CreateToolButton("Ouvrir", (_, _) => _presenter.Open()));
        _saveButton.Click += (_, _) => _presenter.Save();
        _toolStrip.Items.Add(_saveButton);
        _toolStrip.Items.Add(new ToolStripSeparator());
        _undoButton.Click += (_, _) => _presenter.Undo();
        _toolStrip.Items.Add(_undoButton);
        _redoButton.Click += (_, _) => _presenter.Redo();
        _toolStrip.Items.Add(_redoButton);
        _toolStrip.Items.Add(new ToolStripSeparator());
        _addSlideButton.Text = "Nouvelle diapositive";
        _addSlideButton.AutoSize = true;
        _addSlideButton.Click += (_, _) => _presenter.AddSlide();
        _toolStrip.Items.Add(new ToolStripControlHost(_addSlideButton));
        _duplicateSlideButton.Text = "Dupliquer";
        _duplicateSlideButton.AutoSize = true;
        _duplicateSlideButton.Click += (_, _) => _presenter.DuplicateSelectedSlide();
        _toolStrip.Items.Add(new ToolStripControlHost(_duplicateSlideButton));
        _deleteSlideButton.Text = "Supprimer";
        _deleteSlideButton.AutoSize = true;
        _deleteSlideButton.Click += (_, _) => _presenter.DeleteSelectedSlide();
        _toolStrip.Items.Add(new ToolStripControlHost(_deleteSlideButton));
        _toolStrip.Items.Add(new ToolStripSeparator());
        _toolStrip.Items.Add(CreateToolButton("Diaporama images", StartSlideshowButton_Click));
        _topPanel.Controls.Add(_toolStrip);
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
            SplitterDistance = 210
        };
        _slideList.Dock = DockStyle.Fill;
        _slideList.DrawMode = DrawMode.OwnerDrawFixed;
        _slideList.ItemHeight = 102;
        _slideList.BorderStyle = BorderStyle.None;
        _slideList.IntegralHeight = false;
        _slideList.DrawItem += SlideList_DrawItem;
        _slideList.SelectedIndexChanged += SlideList_SelectedIndexChanged;
        _slideList.MouseDown += SlideList_MouseDown;
        _slideList.MouseMove += SlideList_MouseMove;
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
            BackColor = Color.FromArgb(238, 240, 243),
            Padding = new Padding(36)
        };
        _slideSurface.Dock = DockStyle.Fill;
        _slideSurface.BackColor = Color.White;
        _slideSurface.BorderStyle = BorderStyle.FixedSingle;
        _previewMessage.Dock = DockStyle.Fill;
        _previewMessage.TextAlign = ContentAlignment.MiddleCenter;
        _previewMessage.Font = new Font("Segoe UI", 13, FontStyle.Regular);
        _previewMessage.ForeColor = Color.FromArgb(90, 98, 110);
        _slideSurface.Controls.Add(_previewMessage);
        previewHost.Controls.Add(_slideSurface);
        centerSplit.Panel1.Controls.Add(previewHost);

        Panel properties = new()
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(14),
            BackColor = Color.White
        };
        Label propertiesHeader = new()
        {
            Text = "Propriétés",
            Font = new Font("Segoe UI", 12, FontStyle.Bold),
            AutoSize = true,
            Dock = DockStyle.Top
        };
        Label titleLabel = new()
        {
            Text = "Nom de la présentation",
            AutoSize = true,
            Padding = new Padding(0, 18, 0, 4),
            Dock = DockStyle.Top
        };
        _titleEditor.Dock = DockStyle.Top;
        _titleEditor.MaxLength = Limits.MaxTitleLength;
        _titleEditor.KeyDown += TitleEditor_KeyDown;
        _titleEditor.Leave += TitleEditor_Leave;
        Label slideBackgroundLabel = new()
        {
            Text = "Fond de la diapositive",
            AutoSize = true,
            Padding = new Padding(0, 18, 0, 4),
            Dock = DockStyle.Top
        };
        _backgroundButton.Text = "Choisir une couleur…";
        _backgroundButton.TextAlign = ContentAlignment.MiddleLeft;
        _backgroundButton.Dock = DockStyle.Top;
        _backgroundButton.Height = 36;
        _backgroundButton.Click += BackgroundButton_Click;
        _slideCount.Dock = DockStyle.Bottom;
        _slideCount.AutoSize = true;
        _slideCount.Padding = new Padding(0, 0, 0, 10);
        properties.Controls.Add(_backgroundButton);
        properties.Controls.Add(slideBackgroundLabel);
        properties.Controls.Add(_titleEditor);
        properties.Controls.Add(titleLabel);
        properties.Controls.Add(propertiesHeader);
        properties.Controls.Add(_slideCount);
        centerSplit.Panel2.Controls.Add(properties);
        outerSplit.Panel2.Controls.Add(centerSplit);
        Controls.Add(outerSplit);
    }

    private void CreateStatusBar()
    {
        _statusStrip.Items.Add(_modifiedStatus);
        ToolStripStatusLabel spacer = new() { Spring = true };
        _statusStrip.Items.Add(spacer);
        _statusStrip.Items.Add(new ToolStripStatusLabel("Glisser-déposer les diapositives pour les réordonner"));
        Controls.Add(_statusStrip);
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
        e.DrawBackground();
        if (e.Index >= 0 && _slideList.Items[e.Index] is SlideListItem item)
        {
            Rectangle thumbnail = new(e.Bounds.Left + 12, e.Bounds.Top + 8, e.Bounds.Width - 24, 70);
            using SolidBrush whiteBrush = new(Color.White);
            using Pen borderPen = new(_slideList.SelectedIndex == e.Index
                ? Color.FromArgb(38, 118, 211)
                : Color.FromArgb(195, 199, 205), 2);
            e.Graphics.FillRectangle(whiteBrush, thumbnail);
            e.Graphics.DrawRectangle(borderPen, thumbnail);
            using SolidBrush labelBrush = new(ForeColor);
            e.Graphics.DrawString(
                $"Diapositive {item.Number}   ·   {item.Slide.Elements.Count} élément(s)",
                Font,
                labelBrush,
                e.Bounds.Left + 14,
                e.Bounds.Top + 81);
        }

        e.DrawFocusRectangle();
    }

    private void SlideList_MouseDown(object? sender, MouseEventArgs e)
    {
        _dragStartPoint = e.Location;
        int index = _slideList.IndexFromPoint(e.Location);
        _draggedSlide = index >= 0 && _slideList.Items[index] is SlideListItem item
            ? item.Slide
            : null;
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
        if (e.KeyCode == Keys.F5)
        {
            StartSlideshowButton_Click(sender, EventArgs.Empty);
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

    private void ShowUiError(string message) =>
        MessageBox.Show(this, message, "SlideStudio", MessageBoxButtons.OK, MessageBoxIcon.Error);

    private static Color ToDrawingColor(ArgbColor color) => Color.FromArgb(color.A, color.R, color.G, color.B);

    private static Color ContrastTextColor(Color color) =>
        (0.299 * color.R) + (0.587 * color.G) + (0.114 * color.B) >= 145
            ? Color.FromArgb(75, 82, 91)
            : Color.White;

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
        ToolStripButton button = new(text);
        button.Click += handler;
        return button;
    }

    private sealed record SlideListItem(Slide Slide, int Number)
    {
        public override string ToString() => $"Diapositive {Number}";
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
