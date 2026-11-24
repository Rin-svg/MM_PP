using SlideStudio.Core;
using SlideStudio.Core.Editing;
using SlideStudio.Core.Model;
using Xunit;

namespace SlideStudio.Presenters.Tests;

public sealed class MainPresenterTests
{
    [Fact]
    public void New_After_modified_document_cancels_when_confirmation_is_cancelled()
    {
        FakeView view = new();
        FakeDialogs dialogs = new() { UnsavedChoice = UnsavedChangesChoice.Cancel };
        FakeStorage storage = new();
        MainPresenter presenter = new(view, dialogs, storage);
        presenter.History.Execute(EditCommands.SetTitle(presenter.Presentation, "Modifié"));

        bool result = presenter.New();

        Assert.False(result);
        Assert.Equal("Modifié", presenter.Presentation.Title);
        Assert.Equal(1, dialogs.UnsavedConfirmationCount);
    }

    [Fact]
    public void Save_blocks_double_click_and_marks_document_saved()
    {
        FakeView view = new();
        FakeDialogs dialogs = new();
        FakeStorage storage = new();
        MainPresenter presenter = new(view, dialogs, storage);
        presenter.History.Execute(EditCommands.SetTitle(presenter.Presentation, "Modifié"));

        Assert.True(presenter.Save("test.slds"));
        Assert.False(presenter.IsModified);
        Assert.Equal(1, storage.SaveCount);

        storage.SaveCount = 0;
        Assert.True(presenter.Save("test.slds"));
        Assert.Equal(1, storage.SaveCount);
    }

    [Fact]
    public void Open_replaces_document_and_clears_history()
    {
        FakeView view = new();
        FakeDialogs dialogs = new();
        FakeStorage storage = new();
        Presentation opened = new("Ouverte", [new Slide()]);
        storage.DocumentToOpen = opened;
        MainPresenter presenter = new(view, dialogs, storage);

        Assert.True(presenter.Open("ouverte.slds"));
        Assert.Same(opened, presenter.Presentation);
        Assert.False(presenter.History.IsModified);
        Assert.False(presenter.History.CanUndo);
    }

    [Fact]
    public void Delete_selected_slide_refuses_when_it_is_the_last_slide()
    {
        FakeView view = new();
        MainPresenter presenter = new(view, new FakeDialogs(), new FakeStorage());

        presenter.DeleteSelectedSlide();

        Assert.Single(presenter.Presentation.Slides);
    }

    [Fact]
    public void Duplicate_selected_slide_inserts_an_independent_slide()
    {
        FakeView view = new();
        MainPresenter presenter = new(view, new FakeDialogs(), new FakeStorage());
        Slide original = presenter.SelectedSlide!;

        Assert.True(presenter.DuplicateSelectedSlide());

        Assert.Equal(2, presenter.Presentation.Slides.Count);
        Assert.NotSame(original, presenter.SelectedSlide);
        Assert.Same(presenter.SelectedSlide, presenter.Presentation.Slides[1]);
        Assert.True(presenter.History.CanUndo);
    }

    [Fact]
    public void New_discard_choice_allows_replacing_the_document()
    {
        FakeDialogs dialogs = new() { UnsavedChoice = UnsavedChangesChoice.Discard };
        MainPresenter presenter = new(
            new FakeView(),
            dialogs,
            new FakeStorage());
        presenter.History.Execute(EditCommands.SetTitle(presenter.Presentation, "Modifié"));

        Assert.True(presenter.New());
        Assert.Equal(Presentation.DefaultTitle, presenter.Presentation.Title);
        Assert.False(presenter.History.IsModified);
        Assert.Equal(1, dialogs.UnsavedConfirmationCount);
    }

    [Fact]
    public void New_save_choice_saves_then_replaces_the_document()
    {
        FakeDialogs dialogs = new()
        {
            UnsavedChoice = UnsavedChangesChoice.Save,
            SaveFileName = "presentation.slds"
        };
        FakeStorage storage = new();
        MainPresenter presenter = new(
            new FakeView(),
            dialogs,
            storage);
        presenter.History.Execute(EditCommands.SetTitle(presenter.Presentation, "Modifié"));

        Assert.True(presenter.New());
        Assert.Equal(1, storage.SaveCount);
        Assert.Equal(Presentation.DefaultTitle, presenter.Presentation.Title);
    }

    [Fact]
    public void Add_and_move_slide_keep_the_presentation_consistent()
    {
        MainPresenter presenter = new(
            new FakeView(),
            new FakeDialogs(),
            new FakeStorage());

        presenter.AddSlide();
        Slide added = presenter.SelectedSlide!;
        Assert.Equal(2, presenter.Presentation.Slides.Count);
        Assert.Same(added, presenter.SelectedSlide);

        presenter.MoveSlide(added, 0);
        Assert.Same(added, presenter.Presentation.Slides[0]);
    }

    private sealed class FakeView : IMainView
    {
        public void SetDocument(Presentation presentation, Slide? selectedSlide) { }
        public void SetDocumentTitle(string title) { }
        public void SetModified(bool isModified) { }
        public void SetCommandState(bool canUndo, bool canRedo, bool canSave, bool canDeleteSlide) { }
    }

    private sealed class FakeDialogs : IDialogService
    {
        public UnsavedChangesChoice UnsavedChoice { get; set; } = UnsavedChangesChoice.Discard;
        public int UnsavedConfirmationCount { get; private set; }
        public string? OpenFileName { get; set; }
        public string? SaveFileName { get; set; }

        public string? ChooseOpenFile() => OpenFileName;
        public string? ChooseSaveFile(string suggestedName) => SaveFileName;

        public UnsavedChangesChoice ConfirmUnsavedChanges(string documentTitle)
        {
            UnsavedConfirmationCount++;
            return UnsavedChoice;
        }

        public void ShowError(string title, string message) { }
    }

    private sealed class FakeStorage : IDocumentStorage
    {
        public Presentation? DocumentToOpen { get; set; }
        public int SaveCount { get; set; }

        public DocumentPackage OpenPackage(string fileName) =>
            new(DocumentToOpen ?? new Presentation());

        public void SavePackage(DocumentPackage package, string fileName) => SaveCount++;
    }
}
