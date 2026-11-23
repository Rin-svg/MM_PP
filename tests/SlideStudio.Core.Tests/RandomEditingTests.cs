// SÉCURITÉ : CA5394 (« aléa non sécurisé ») est désactivé pour CE fichier uniquement. Ici l'aléa sert à générer des
// scénarios de test reproductibles (graine fixe) : aucun secret, aucune décision de sécurité n'en dépend.
#pragma warning disable CA5394

using SlideStudio.Core.Editing;
using SlideStudio.Core.Editing.Commands;
using SlideStudio.Core.Model;
using Xunit;

namespace SlideStudio.Core.Tests;

/// <summary>
/// Test de propriété avec graine fixe : après des centaines de modifications aléatoires (valides ET invalides),
/// « tout annuler » doit ramener EXACTEMENT à l'état initial et « tout rétablir » EXACTEMENT à l'état final.
/// </summary>
public class RandomEditingTests
{
    [Theory]
    [InlineData(20261117)]
    [InlineData(1)]
    [InlineData(987654321)]
    public void RandomEdits_UndoAllRestoresInitial_RedoAllRestoresFinal(int seed)
    {
        var random = new Random(seed);
        var presentation = new Presentation();
        var history = new UndoHistory(maxDepth: 10_000);
        string initial = TestSupport.Snapshot(presentation);

        for (int step = 0; step < 700; step++)
        {
            ApplyRandomEdit(random, presentation, history);
        }

        string final = TestSupport.Snapshot(presentation);
        Assert.True(history.CanUndo);

        int undone = 0;
        while (history.Undo())
        {
            undone++;
        }

        Assert.True(undone > 100);
        Assert.Equal(initial, TestSupport.Snapshot(presentation));
        Assert.False(history.IsModified);

        while (history.Redo())
        {
        }

        Assert.Equal(final, TestSupport.Snapshot(presentation));
        Assert.True(history.IsModified);
    }

    private static void ApplyRandomEdit(Random random, Presentation presentation, UndoHistory history)
    {
        Slide slide = presentation.Slides[random.Next(presentation.Slides.Count)];
        int slideIndex = presentation.IndexOf(slide);

        // 1 étape sur 15 : une modification volontairement INVALIDE. Elle doit être refusée sans laisser aucune trace.
        if (random.Next(15) == 0 && slide.Elements.Count > 0)
        {
            string before = TestSupport.Snapshot(presentation);
            SlideElement target = slide.Elements[random.Next(slide.Elements.Count)];

            Assert.Throws<DocumentValidationException>(
                () => history.Execute(EditCommands.SetFrame(target, new ElementFrame(double.NaN, 0, 10, 10))));
            Assert.Equal(before, TestSupport.Snapshot(presentation));
            return;
        }

        switch (random.Next(9))
        {
            case 0 when presentation.Slides.Count < Limits.MaxSlides:
                history.Execute(new AddSlideCommand(presentation, new Slide(), random.Next(presentation.Slides.Count + 1)));
                break;

            case 1 when presentation.Slides.Count > 1:
                history.Execute(new DeleteSlideCommand(presentation, slide));
                break;

            case 2:
                history.Execute(new MoveSlideCommand(presentation, slide, random.Next(presentation.Slides.Count)));
                break;

            case 3 when slide.Elements.Count < Limits.MaxElementsPerSlide:
                history.Execute(new AddElementCommand(slide, random.Next(2) == 0
                    ? TestSupport.NewText("t" + random.Next(1000))
                    : TestSupport.NewShape((ShapeKind)random.Next(3))));
                break;

            case 4 when slide.Elements.Count > 0:
                history.Execute(new DeleteElementCommand(slide, slide.Elements[random.Next(slide.Elements.Count)]));
                break;

            case 5 when slide.Elements.Count > 0:
                history.Execute(EditCommands.SetFrame(
                    slide.Elements[random.Next(slide.Elements.Count)],
                    new ElementFrame(random.Next(-100, 1300), random.Next(-100, 800), random.Next(1, 500), random.Next(1, 500), random.Next(0, 360))));
                break;

            case 6 when slide.Elements.Count > 0:
                if (slide.Elements[random.Next(slide.Elements.Count)] is TextElement text)
                {
                    history.Execute(EditCommands.SetText(text, "texte " + random.Next(10_000)));
                }

                break;

            case 7 when slide.Elements.Count > 0:
                history.Execute(new ReorderElementCommand(
                    slide, slide.Elements[random.Next(slide.Elements.Count)], random.Next(slide.Elements.Count)));
                break;

            case 8 when presentation.Slides.Count < Limits.MaxSlides:
                history.Execute(new AddSlideCommand(presentation, slide.Clone(), slideIndex + 1, "Dupliquer la diapositive"));
                break;

            default:
                break;
        }
    }
}
