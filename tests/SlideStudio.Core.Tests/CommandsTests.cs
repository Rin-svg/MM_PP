using SlideStudio.Core.Editing;
using SlideStudio.Core.Editing.Commands;
using SlideStudio.Core.Model;
using Xunit;

namespace SlideStudio.Core.Tests;

/// <summary>Chaque commande : exécuter, annuler, rétablir (même objet), refus propre, et « refus = aucun effet ».</summary>
public class CommandsTests
{
    // ------------------------------------------------------------------ Diapositives

    [Fact]
    public void AddSlide_ExecuteUndoRedo_RestoresTheSameObject()
    {
        var presentation = new Presentation();
        var history = new UndoHistory();
        var slide = new Slide();

        history.Execute(new AddSlideCommand(presentation, slide, 1));
        Assert.Collection(presentation.Slides,
            _ => { },
            _ => { });

        history.Undo();
        Assert.Single(presentation.Slides);

        history.Redo();
        Assert.True(ReferenceEquals(presentation.Slides[1], slide));
    }

    [Fact]
    public void DeleteSlide_Undo_PutsSlideBackAtItsOriginalPosition()
    {
        var presentation = new Presentation();
        var history = new UndoHistory();
        var middle = new Slide();
        var last = new Slide();
        presentation.InsertSlide(1, middle);
        presentation.InsertSlide(2, last);

        history.Execute(new DeleteSlideCommand(presentation, middle));
        Assert.Collection(presentation.Slides,
            _ => { },
            _ => { });
        Assert.True(ReferenceEquals(presentation.Slides[1], last));

        history.Undo();

        Assert.Collection(presentation.Slides,
            _ => { },
            _ => { },
            _ => { });
        Assert.True(ReferenceEquals(presentation.Slides[1], middle));
    }

    [Fact]
    public void DeleteSlide_UndoRestoresSlideContentToo()
    {
        var presentation = new Presentation();
        var history = new UndoHistory();
        var slide = new Slide();
        slide.AddElement(TestSupport.NewText("précieux"));
        presentation.InsertSlide(1, slide);
        string before = TestSupport.Snapshot(presentation);

        history.Execute(new DeleteSlideCommand(presentation, slide));
        history.Undo();

        Assert.Equal(before, TestSupport.Snapshot(presentation));
    }

    [Fact]
    public void DeleteSlide_LastSlide_IsRefusedAndHistoryUnchanged()
    {
        var presentation = new Presentation();
        var history = new UndoHistory();

        Assert.Throws<DocumentValidationException>(
            () => history.Execute(new DeleteSlideCommand(presentation, presentation.Slides[0])));

        Assert.Single(presentation.Slides);
        Assert.False(history.CanUndo);
        Assert.False(history.IsModified);
    }

    [Fact]
    public void DuplicateSlide_InsertsIndependentCopyRightAfterOriginal()
    {
        var presentation = new Presentation();
        var history = new UndoHistory();
        Slide original = presentation.Slides[0];
        original.AddElement(TestSupport.NewText("x"));
        presentation.InsertSlide(1, new Slide());

        history.Execute(new AddSlideCommand(presentation, original.Clone(), 1, "Dupliquer la diapositive"));

        Assert.Collection(presentation.Slides,
            _ => { },
            _ => { },
            _ => { });
        Assert.True(presentation.Slides[1].Id != original.Id);
        Assert.Single(presentation.Slides[1].Elements);
        Assert.Equal("Dupliquer la diapositive", history.UndoDescription);
    }

    [Fact]
    public void MoveSlide_UndoRestoresOrder()
    {
        var presentation = new Presentation();
        var history = new UndoHistory();
        Slide first = presentation.Slides[0];
        presentation.InsertSlide(1, new Slide());
        presentation.InsertSlide(2, new Slide());

        history.Execute(new MoveSlideCommand(presentation, first, 2));
        Assert.True(ReferenceEquals(presentation.Slides[2], first));
        history.Undo();

        Assert.True(ReferenceEquals(presentation.Slides[0], first));
    }

    [Fact]
    public void MoveSlide_ToItsCurrentPosition_IsNoOp()
    {
        var presentation = new Presentation();
        var history = new UndoHistory();
        presentation.InsertSlide(1, new Slide());

        history.Execute(new MoveSlideCommand(presentation, presentation.Slides[0], 0));

        Assert.False(history.CanUndo);
        Assert.False(history.IsModified);
    }

    [Fact]
    public void NoOpCommand_DoesNotDiscardTheRedoBranch()
    {
        var presentation = new Presentation();
        var history = new UndoHistory();
        Slide first = presentation.Slides[0];
        presentation.InsertSlide(1, new Slide());
        history.Execute(new MoveSlideCommand(presentation, first, 1));
        history.Undo();

        // L'utilisateur lâche la diapositive là où elle était : il n'a rien fait, son « rétablir » reste disponible.
        history.Execute(new MoveSlideCommand(presentation, first, 0));

        Assert.True(history.CanRedo);
    }

    // ------------------------------------------------------------------ Éléments

    [Fact]
    public void AddElement_UndoRemovesIt_RedoBringsBackTheSameObject()
    {
        var slide = new Slide();
        var history = new UndoHistory();
        TextElement element = TestSupport.NewText();

        history.Execute(new AddElementCommand(slide, element));
        Assert.Single(slide.Elements);
        history.Undo();
        Assert.Empty(slide.Elements);
        history.Redo();

        Assert.True(ReferenceEquals(slide.Elements[0], element));
    }

    [Fact]
    public void AddElement_OnFullSlide_IsRefusedWithNoSideEffect()
    {
        var slide = new Slide();
        var history = new UndoHistory();
        for (int i = 0; i < Limits.MaxElementsPerSlide; i++)
        {
            slide.AddElement(TestSupport.NewText());
        }

        Assert.Throws<DocumentValidationException>(() => history.Execute(new AddElementCommand(slide, TestSupport.NewText())));

        Assert.True(slide.Elements.Count == Limits.MaxElementsPerSlide);
        Assert.False(history.CanUndo);
    }

    [Fact]
    public void DeleteElement_UndoRestoresStackingLevel()
    {
        var slide = new Slide();
        var history = new UndoHistory();
        TextElement a = TestSupport.NewText("a");
        TextElement b = TestSupport.NewText("b");
        TextElement c = TestSupport.NewText("c");
        slide.AddElement(a);
        slide.AddElement(b);
        slide.AddElement(c);

        history.Execute(new DeleteElementCommand(slide, b));
        history.Undo();

        Assert.True(ReferenceEquals(slide.Elements[1], b));
    }

    [Fact]
    public void ReorderElement_BringToFrontAndUndo()
    {
        var slide = new Slide();
        var history = new UndoHistory();
        TextElement back = TestSupport.NewText("fond");
        TextElement front = TestSupport.NewText("devant");
        slide.AddElement(back);
        slide.AddElement(front);

        history.Execute(new ReorderElementCommand(slide, back, 1, "Mettre au premier plan"));
        Assert.True(ReferenceEquals(slide.Elements[1], back));
        Assert.Equal("Mettre au premier plan", history.UndoDescription);

        history.Undo();
        Assert.True(ReferenceEquals(slide.Elements[0], back));
    }

    // ------------------------------------------------------------------ Modifications de propriétés

    [Fact]
    public void SetFrame_InvalidFrame_IsRefusedAndNothingChanges()
    {
        var history = new UndoHistory();
        TextElement element = TestSupport.NewText();
        ElementFrame before = element.Frame;

        Assert.Throws<DocumentValidationException>(
            () => history.Execute(EditCommands.SetFrame(element, new ElementFrame(double.NaN, 0, 10, 10))));
        Assert.Throws<DocumentValidationException>(
            () => history.Execute(EditCommands.SetFrame(element, new ElementFrame(0, 0, 0, 10))));

        Assert.Equal(before, element.Frame);
        Assert.False(history.CanUndo);
        Assert.False(history.IsModified);
    }

    [Fact]
    public void SetFrame_UndoThenRedo_RoundTrips()
    {
        var history = new UndoHistory();
        TextElement element = TestSupport.NewText();
        ElementFrame before = element.Frame;
        var after = new ElementFrame(500, 300, 640, 120, 45);

        history.Execute(EditCommands.SetFrame(element, after));
        history.Undo();
        Assert.Equal(before, element.Frame);
        history.Redo();

        Assert.Equal(after, element.Frame);
    }

    [Fact]
    public void SetText_TooLong_IsRefusedAndKeepsText()
    {
        var history = new UndoHistory();
        TextElement element = TestSupport.NewText("intact");

        Assert.Throws<DocumentValidationException>(
            () => history.Execute(EditCommands.SetText(element, new string('x', Limits.MaxTextLength + 1))));

        Assert.Equal("intact", element.Text);
        Assert.False(history.IsModified);
    }

    [Fact]
    public void SetBold_AlreadyBold_IsNoOp()
    {
        var history = new UndoHistory();
        TextElement element = TestSupport.NewText();

        history.Execute(EditCommands.SetBold(element, true));
        history.Execute(EditCommands.SetBold(element, true));

        history.Undo();
        Assert.False(element.IsBold);
        Assert.False(history.CanUndo);
    }

    [Fact]
    public void FormattingCommands_AllUndoToPreviousValues()
    {
        var history = new UndoHistory();
        TextElement text = TestSupport.NewText();
        ShapeElement shape = TestSupport.NewShape();
        var slide = new Slide();
        var presentation = new Presentation();
        ArgbColor red = ArgbColor.FromRgb(255, 0, 0);
        ArgbColor shapeFillBefore = shape.Fill;

        history.Execute(EditCommands.SetFontSize(text, 40));
        history.Execute(EditCommands.SetItalic(text, true));
        history.Execute(EditCommands.SetTextColor(text, red));
        history.Execute(EditCommands.SetShapeFill(shape, red));
        history.Execute(EditCommands.SetSlideBackground(slide, red));
        history.Execute(EditCommands.SetTitle(presentation, "Nouveau titre"));

        Assert.Equal(40.0, text.FontSizePoints);
        Assert.True(text.IsItalic);
        Assert.Equal(red, text.Color);
        Assert.Equal(red, shape.Fill);
        Assert.Equal(red, slide.Background);
        Assert.Equal("Nouveau titre", presentation.Title);

        while (history.Undo())
        {
        }

        Assert.Equal(TextElement.DefaultFontSizePoints, text.FontSizePoints);
        Assert.False(text.IsItalic);
        Assert.Equal(ArgbColor.Black, text.Color);
        Assert.Equal(shapeFillBefore, shape.Fill);
        Assert.Equal(ArgbColor.White, slide.Background);
        Assert.Equal(Presentation.DefaultTitle, presentation.Title);
    }

    [Fact]
    public void SetProperty_TwoCommandsBuiltUpFront_EachUndoesToTheValueThatReallyPrecededIt()
    {
        var history = new UndoHistory();
        TextElement element = TestSupport.NewText("A");

        // Les deux commandes sont CRÉÉES alors que le texte vaut encore « A »...
        IUndoableCommand toB = EditCommands.SetText(element, "B");
        IUndoableCommand toC = EditCommands.SetText(element, "C");

        // ...mais exécutées l'une après l'autre : pour « toC », la valeur d'avant est « B », pas « A ».
        history.Execute(toB);
        history.Execute(toC);

        history.Undo();
        Assert.Equal("B", element.Text);
        history.Undo();
        Assert.Equal("A", element.Text);
    }

    [Fact]
    public void SetFontSize_SliderDragWithOneGesture_IsOneUndoStep()
    {
        var history = new UndoHistory();
        TextElement text = TestSupport.NewText();
        Guid drag = Guid.NewGuid();

        for (double size = 25; size <= 60; size++)
        {
            history.Execute(EditCommands.SetFontSize(text, size, drag));
        }

        history.Undo();

        Assert.Equal(TextElement.DefaultFontSizePoints, text.FontSizePoints);
        Assert.False(history.CanUndo);
    }

    [Fact]
    public void Constructors_NullArguments_ThrowArgumentNull()
    {
        var presentation = new Presentation();
        var slide = new Slide();

        Assert.Throws<ArgumentNullException>(() => _ = new AddSlideCommand(null!, slide, 0));
        Assert.Throws<ArgumentNullException>(() => _ = new AddSlideCommand(presentation, null!, 0));
        Assert.Throws<ArgumentNullException>(() => _ = new DeleteSlideCommand(presentation, null!));
        Assert.Throws<ArgumentNullException>(() => _ = new MoveSlideCommand(null!, slide, 0));
        Assert.Throws<ArgumentNullException>(() => _ = new AddElementCommand(slide, null!));
        Assert.Throws<ArgumentNullException>(() => _ = new DeleteElementCommand(null!, TestSupport.NewText()));
    }
}
