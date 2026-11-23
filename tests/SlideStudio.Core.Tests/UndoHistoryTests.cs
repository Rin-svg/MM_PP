using SlideStudio.Core.Editing;
using SlideStudio.Core.Model;
using Xunit;

namespace SlideStudio.Core.Tests;

/// <summary>Historique annuler/rétablir : ordre, branches, suivi « modifié », limite de profondeur, fusion de gestes, échecs.</summary>
public class UndoHistoryTests
{
    [Fact]
    public void NewHistory_IsEmptyAndNotModified()
    {
        var history = new UndoHistory();

        Assert.False(history.CanUndo);
        Assert.False(history.CanRedo);
        Assert.False(history.IsModified);
        Assert.Null(history.UndoDescription);
        Assert.Null(history.RedoDescription);
    }

    [Fact]
    public void Execute_ThenUndoThenRedo_CallsCommandInOrder()
    {
        var history = new UndoHistory();
        var command = new FakeCommand("Ajouter");

        history.Execute(command);
        Assert.Equal(1, command.ExecuteCount);
        Assert.Equal("Ajouter", history.UndoDescription);

        Assert.True(history.Undo());
        Assert.Equal(1, command.UndoCount);
        Assert.False(history.CanUndo);
        Assert.Equal("Ajouter", history.RedoDescription);

        Assert.True(history.Redo());
        Assert.Equal(2, command.ExecuteCount);
        Assert.False(history.CanRedo);
    }

    [Fact]
    public void UndoAndRedo_WhenNothingToDo_ReturnFalseAndAreHarmless()
    {
        var history = new UndoHistory();

        // UTILISATEUR : taper Ctrl+Z dix fois de suite sur un document vierge ne doit rien casser.
        for (int i = 0; i < 10; i++)
        {
            Assert.False(history.Undo());
            Assert.False(history.Redo());
        }

        Assert.False(history.IsModified);
    }

    [Fact]
    public void Execute_AfterUndo_DiscardsTheRedoBranch()
    {
        var history = new UndoHistory();
        history.Execute(new FakeCommand("A"));
        history.Execute(new FakeCommand("B"));
        history.Undo();
        Assert.True(history.CanRedo);

        history.Execute(new FakeCommand("C"));

        Assert.False(history.CanRedo);
        Assert.Equal("C", history.UndoDescription);
    }

    [Fact]
    public void Execute_NullCommand_ThrowsArgumentNull()
    {
        Assert.Throws<ArgumentNullException>(() => new UndoHistory().Execute(null!));
    }

    [Fact]
    public void Constructor_InvalidDepth_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => _ = new UndoHistory(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => _ = new UndoHistory(-5));
        Assert.Throws<ArgumentOutOfRangeException>(() => _ = new UndoHistory(10_001));
    }

    // ------------------------------------------------------------------ Suivi des modifications non enregistrées

    [Fact]
    public void IsModified_FollowsExecuteSaveUndoRedo()
    {
        var history = new UndoHistory();

        history.Execute(new FakeCommand());
        Assert.True(history.IsModified);

        history.MarkSaved();
        Assert.False(history.IsModified);

        history.Undo();
        Assert.True(history.IsModified);

        // Rétablir ramène exactement à l'état enregistré : l'astérisque disparaît.
        history.Redo();
        Assert.False(history.IsModified);
    }

    [Fact]
    public void IsModified_UndoingBackToSavedState_ClearsTheFlag()
    {
        var history = new UndoHistory();
        history.Execute(new FakeCommand("A"));
        history.MarkSaved();
        history.Execute(new FakeCommand("B"));
        history.Execute(new FakeCommand("C"));
        Assert.True(history.IsModified);

        history.Undo();
        history.Undo();

        Assert.False(history.IsModified);
    }

    [Fact]
    public void IsModified_NewBranchAfterSavedStateNeverMatchesSavedStateAgain()
    {
        var history = new UndoHistory();
        history.Execute(new FakeCommand("A"));
        history.Execute(new FakeCommand("B"));
        history.MarkSaved();

        // On annule B puis on fait autre chose (D) : même nombre d'actions qu'à l'enregistrement, mais contenu différent.
        history.Undo();
        history.Execute(new FakeCommand("D"));

        Assert.True(history.IsModified);
        history.Undo();
        Assert.True(history.IsModified);
        history.Redo();
        Assert.True(history.IsModified);
    }

    [Fact]
    public void IsModified_SavedStateTrimmedOutOfHistory_StaysModified()
    {
        var history = new UndoHistory(maxDepth: 3);
        history.Execute(new FakeCommand("1"));
        history.MarkSaved();

        for (int i = 0; i < 5; i++)
        {
            history.Execute(new FakeCommand("n"));
        }

        while (history.Undo())
        {
        }

        // L'état enregistré est plus ancien que ce que l'historique conserve : on ne peut plus y revenir.
        Assert.True(history.IsModified);
    }

    [Fact]
    public void Clear_EmptiesHistoryAndMarksDocumentSaved()
    {
        var history = new UndoHistory();
        history.Execute(new FakeCommand());
        history.Execute(new FakeCommand());
        history.Undo();

        history.Clear();

        Assert.False(history.CanUndo);
        Assert.False(history.CanRedo);
        Assert.False(history.IsModified);
    }

    // ------------------------------------------------------------------ Limite de profondeur

    [Fact]
    public void Depth_OldestActionsAreForgotten()
    {
        var history = new UndoHistory(maxDepth: 5);
        for (int i = 0; i < 12; i++)
        {
            history.Execute(new FakeCommand());
        }

        int undone = 0;
        while (history.Undo())
        {
            undone++;
        }

        Assert.Equal(5, undone);
    }

    [Fact]
    public void Depth_DefaultLimitMatchesLimitsConstant()
    {
        var history = new UndoHistory();
        for (int i = 0; i < Limits.MaxHistoryDepth + 10; i++)
        {
            history.Execute(new FakeCommand());
        }

        int undone = 0;
        while (history.Undo())
        {
            undone++;
        }

        Assert.Equal(Limits.MaxHistoryDepth, undone);
    }

    // ------------------------------------------------------------------ Commandes sans effet et commandes en échec

    [Fact]
    public void Execute_NoOpCommand_IsIgnored()
    {
        var history = new UndoHistory();
        var command = new FakeCommand { NoOp = true };

        history.Execute(command);

        Assert.Equal(0, command.ExecuteCount);
        Assert.False(history.CanUndo);
        Assert.False(history.IsModified);
    }

    [Fact]
    public void Execute_CommandThatThrows_LeavesHistoryUntouchedAndPropagates()
    {
        var history = new UndoHistory();
        history.Execute(new FakeCommand("bonne"));
        var failing = new FakeCommand("mauvaise") { ThrowOnExecute = true };

        Assert.Throws<InvalidOperationException>(() => history.Execute(failing));

        Assert.Equal("bonne", history.UndoDescription);
        Assert.False(history.CanRedo);
    }

    [Fact]
    public void Execute_FailingCommandAfterUndo_DoesNotDestroyTheRedoBranch()
    {
        var history = new UndoHistory();
        history.Execute(new FakeCommand("A"));
        history.Undo();

        Assert.Throws<InvalidOperationException>(() => history.Execute(new FakeCommand { ThrowOnExecute = true }));

        // L'utilisateur peut toujours rétablir « A » : une action refusée n'a rien détruit.
        Assert.True(history.CanRedo);
    }

    [Fact]
    public void Undo_CommandThatThrows_KeepsEntryInPlace()
    {
        var history = new UndoHistory();
        var command = new FakeCommand("fragile") { ThrowOnUndo = true };
        history.Execute(command);

        Assert.Throws<InvalidOperationException>(() => history.Undo());

        Assert.True(history.CanUndo);
        Assert.False(history.CanRedo);
    }

    // ------------------------------------------------------------------ Fusion des gestes (glisser = une étape)

    [Fact]
    public void Merge_SameGesture_CollapsesIntoOneUndoStep()
    {
        var history = new UndoHistory();
        TextElement element = TestSupport.NewText();
        ElementFrame start = element.Frame;
        Guid drag = Guid.NewGuid();

        for (int x = 1; x <= 50; x++)
        {
            history.Execute(EditCommands.SetFrame(element, start.Moved(x, 0), drag));
        }

        Assert.Equal(start.Moved(50, 0), element.Frame);
        history.Undo();
        Assert.Equal(start, element.Frame);
        Assert.False(history.CanUndo);
        history.Redo();
        Assert.Equal(start.Moved(50, 0), element.Frame);
    }

    [Fact]
    public void Merge_DifferentGestures_StaySeparateSteps()
    {
        var history = new UndoHistory();
        TextElement element = TestSupport.NewText();
        ElementFrame start = element.Frame;

        history.Execute(EditCommands.SetFrame(element, start.Moved(10, 0), Guid.NewGuid()));
        history.Execute(EditCommands.SetFrame(element, start.Moved(20, 0), Guid.NewGuid()));

        history.Undo();
        Assert.Equal(start.Moved(10, 0), element.Frame);
        history.Undo();
        Assert.Equal(start, element.Frame);
    }

    [Fact]
    public void Merge_WithoutGestureId_NeverMerges()
    {
        var history = new UndoHistory();
        TextElement element = TestSupport.NewText();
        ElementFrame start = element.Frame;

        history.Execute(EditCommands.SetFrame(element, start.Moved(10, 0)));
        history.Execute(EditCommands.SetFrame(element, start.Moved(20, 0)));

        history.Undo();
        Assert.Equal(start.Moved(10, 0), element.Frame);
    }

    [Fact]
    public void Merge_DifferentElementsSameGesture_DoNotMerge()
    {
        var history = new UndoHistory();
        TextElement first = TestSupport.NewText("1");
        TextElement second = TestSupport.NewText("2");
        Guid gesture = Guid.NewGuid();

        history.Execute(EditCommands.SetFrame(first, first.Frame.Moved(5, 5), gesture));
        history.Execute(EditCommands.SetFrame(second, second.Frame.Moved(7, 7), gesture));

        history.Undo();
        Assert.Equal(TestSupport.ValidFrame(), second.Frame);
        Assert.Equal(TestSupport.ValidFrame().Moved(5, 5), first.Frame);
    }

    [Fact]
    public void Merge_NeverAltersTheSavedState()
    {
        var history = new UndoHistory();
        TextElement element = TestSupport.NewText();
        ElementFrame start = element.Frame;
        Guid gesture = Guid.NewGuid();

        history.Execute(EditCommands.SetFrame(element, start.Moved(10, 0), gesture));
        history.MarkSaved();

        // Même identifiant de geste, mais l'état précédent est celui du disque : pas de fusion.
        history.Execute(EditCommands.SetFrame(element, start.Moved(20, 0), gesture));
        Assert.True(history.IsModified);

        history.Undo();

        Assert.Equal(start.Moved(10, 0), element.Frame);
        Assert.False(history.IsModified);
    }

    // ------------------------------------------------------------------ Événement Changed

    [Fact]
    public void Changed_IsRaisedForEveryStateChange()
    {
        var history = new UndoHistory();
        int raised = 0;
        history.Changed += (_, _) => raised++;

        history.Execute(new FakeCommand());
        history.Undo();
        history.Redo();
        history.MarkSaved();
        history.Clear();

        Assert.Equal(5, raised);
    }

    [Fact]
    public void Changed_NotRaisedWhenNothingHappens()
    {
        var history = new UndoHistory();
        int raised = 0;
        history.Changed += (_, _) => raised++;

        history.Undo();
        history.Redo();
        history.Execute(new FakeCommand { NoOp = true });

        Assert.Equal(0, raised);
    }
}
