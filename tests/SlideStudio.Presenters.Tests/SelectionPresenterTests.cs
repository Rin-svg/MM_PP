using SlideStudio.Core.Editing;
using SlideStudio.Core.Model;
using Xunit;

namespace SlideStudio.Presenters.Tests;

public sealed class SelectionPresenterTests
{
    [Fact]
    public void Copy_cut_and_paste_preserves_a_copy_and_the_undo_history()
    {
        Slide slide = new();
        TextElement original = new(new ElementFrame(10, 10, 100, 30), "Bonjour");
        slide.AddElement(original);
        UndoHistory history = new();
        FakeSelectionView view = new();
        SelectionPresenter presenter = new(view, history, slide);
        presenter.SetSelection([original]);

        presenter.Copy();
        Assert.True(presenter.Cut());
        Assert.Empty(slide.Elements);
        Assert.True(presenter.Paste());
        Assert.Single(slide.Elements);
        Assert.NotSame(original, slide.Elements[0]);
        Assert.True(history.CanUndo);
    }

    [Fact]
    public void Bring_to_front_changes_the_element_stack()
    {
        Slide slide = new();
        TextElement first = new(new ElementFrame(10, 10, 100, 30), "Premier");
        TextElement second = new(new ElementFrame(20, 20, 100, 30), "Deuxième");
        slide.AddElement(first);
        slide.AddElement(second);
        SelectionPresenter presenter = new(
            new FakeSelectionView(),
            new UndoHistory(),
            slide);
        presenter.SetSelection([first]);

        Assert.True(presenter.BringToFront());

        Assert.Same(first, slide.Elements[1]);
        Assert.Same(second, slide.Elements[0]);
    }

    [Fact]
    public void Formatting_commands_update_all_selected_text_elements()
    {
        Slide slide = new();
        TextElement first = new(new ElementFrame(10, 10, 100, 30), "Premier");
        TextElement second = new(new ElementFrame(20, 20, 100, 30), "Deuxième");
        slide.AddElement(first);
        slide.AddElement(second);
        UndoHistory history = new();
        SelectionPresenter presenter = new(
            new FakeSelectionView(),
            history,
            slide);
        presenter.SetSelection([first, second]);

        presenter.SetFontFamily("Arial");
        presenter.SetFontSize(32);
        presenter.SetTextColor(ArgbColor.FromRgb(10, 20, 30));
        presenter.SetTextAlignment(TextAlignment.Center);

        Assert.Equal("Arial", first.FontFamily);
        Assert.Equal(32, second.FontSizePoints);
        Assert.Equal(ArgbColor.FromRgb(10, 20, 30), first.Color);
        Assert.Equal(TextAlignment.Center, second.Alignment);
    }

    [Fact]
    public void Duplicate_creates_new_elements_and_keeps_them_selected()
    {
        Slide slide = new();
        TextElement original = new(new ElementFrame(10, 10, 100, 30), "Original");
        slide.AddElement(original);
        SelectionPresenter presenter = new(
            new FakeSelectionView(),
            new UndoHistory(),
            slide);
        presenter.SetSelection([original]);

        Assert.True(presenter.Duplicate());

        Assert.Equal(2, slide.Elements.Count);
        Assert.Single(presenter.Selection);
        Assert.NotSame(original, presenter.Selection[0]);
    }

    private sealed class FakeSelectionView : ISelectionView
    {
        public void SetSelection(IReadOnlyList<SlideElement> selection) { }
        public void SetSelectionActions(bool canCopy, bool canCut, bool canPaste, bool canDelete, bool canReorder) { }
    }
}
