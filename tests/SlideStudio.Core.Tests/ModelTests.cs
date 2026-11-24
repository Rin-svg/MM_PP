using SlideStudio.Core.Model;
using Xunit;

namespace SlideStudio.Core.Tests;

/// <summary>Invariants de Presentation et Slide : limites, doublons, copie profonde, vues en lecture seule.</summary>
public class ModelTests
{
    // ------------------------------------------------------------------ Presentation

    [Fact]
    public void NewPresentation_HasDefaultTitleAndOneBlankSlide()
    {
        var presentation = new Presentation();

        Assert.Equal(Presentation.DefaultTitle, presentation.Title);
        Assert.Single(presentation.Slides);
        Assert.Empty(presentation.Slides[0].Elements);
    }

    [Fact]
    public void Presentation_WithoutAnySlide_IsRejected()
    {
        Assert.Throws<DocumentValidationException>(() => _ = new Presentation("Titre", []));
    }

    [Fact]
    public void Presentation_NullSlides_ThrowsArgumentNull()
    {
        Assert.Throws<ArgumentNullException>(() => _ = new Presentation("Titre", null!));
    }

    [Fact]
    public void Presentation_AtMaxSlides_IsAcceptedAndOneMoreIsRejected()
    {
        var presentation = new Presentation();

        while (presentation.Slides.Count < Limits.MaxSlides)
        {
            presentation.InsertSlide(presentation.Slides.Count, new Slide());
        }

        Assert.True(presentation.Slides.Count == Limits.MaxSlides);
        DocumentValidationException error = Assert.Throws<DocumentValidationException>(
            () => presentation.InsertSlide(0, new Slide()));
        Assert.Contains("500", error.Message);
        Assert.True(presentation.Slides.Count == Limits.MaxSlides);
    }

    [Fact]
    public void Presentation_ConstructedWithTooManySlides_IsRejected()
    {
        var slides = new List<Slide>();
        for (int i = 0; i <= Limits.MaxSlides; i++)
        {
            slides.Add(new Slide());
        }

        Assert.Throws<DocumentValidationException>(() => _ = new Presentation("Titre", slides));
    }

    [Fact]
    public void Presentation_TwoSlidesWithSameId_AreRejected()
    {
        Guid id = Guid.NewGuid();
        var first = new Slide(id, ArgbColor.White, []);
        var second = new Slide(id, ArgbColor.Black, []);

        Assert.Throws<DocumentValidationException>(() => _ = new Presentation("Titre", [first, second]));
    }

    [Fact]
    public void RemoveSlide_LastRemainingSlide_IsRejectedWithFriendlyMessage()
    {
        var presentation = new Presentation();

        DocumentValidationException error = Assert.Throws<DocumentValidationException>(
            () => presentation.RemoveSlide(presentation.Slides[0]));

        Assert.Contains("dernière diapositive", error.Message);
        Assert.Single(presentation.Slides);
    }

    [Fact]
    public void RemoveSlide_NotAMember_ThrowsInvalidOperation()
    {
        var presentation = new Presentation();
        presentation.InsertSlide(1, new Slide());

        Assert.Throws<InvalidOperationException>(() => presentation.RemoveSlide(new Slide()));
    }

    [Fact]
    public void InsertSlide_IndexOutOfRange_Throws()
    {
        var presentation = new Presentation();

        Assert.Throws<ArgumentOutOfRangeException>(() => presentation.InsertSlide(-1, new Slide()));
        Assert.Throws<ArgumentOutOfRangeException>(() => presentation.InsertSlide(2, new Slide()));
        presentation.InsertSlide(1, new Slide());
        Assert.Collection(presentation.Slides,
            _ => { },
            _ => { });
    }

    [Fact]
    public void MoveSlide_ReordersAndRejectsBadIndex()
    {
        var presentation = new Presentation();
        Slide first = presentation.Slides[0];
        var second = new Slide();
        var third = new Slide();
        presentation.InsertSlide(1, second);
        presentation.InsertSlide(2, third);

        presentation.MoveSlide(first, 2);

        Assert.True(ReferenceEquals(presentation.Slides[0], second));
        Assert.True(ReferenceEquals(presentation.Slides[1], third));
        Assert.True(ReferenceEquals(presentation.Slides[2], first));
        Assert.Throws<ArgumentOutOfRangeException>(() => presentation.MoveSlide(first, 3));
        Assert.Throws<ArgumentOutOfRangeException>(() => presentation.MoveSlide(first, -1));
    }

    [Fact]
    public void Slides_ExposedList_CannotBeUsedToBypassValidation()
    {
        var presentation = new Presentation();

        object exposed = presentation.Slides;

        // La liste exposée n'est PAS un List<Slide> : on ne peut pas la caster pour y ajouter une diapositive en douce.
        Assert.False(exposed is List<Slide>);
        Assert.False(exposed is ICollection<Slide> { IsReadOnly: false });
    }

    // ------------------------------------------------------------------ Slide

    [Fact]
    public void Slide_AtMaxElements_IsAcceptedAndOneMoreIsRejected()
    {
        var slide = new Slide();

        for (int i = 0; i < Limits.MaxElementsPerSlide; i++)
        {
            slide.AddElement(TestSupport.NewText());
        }

        Assert.Throws<DocumentValidationException>(() => slide.AddElement(TestSupport.NewText()));
        Assert.True(slide.Elements.Count == Limits.MaxElementsPerSlide);
    }

    [Fact]
    public void Slide_SameElementTwice_IsRejected()
    {
        var slide = new Slide();
        TextElement element = TestSupport.NewText();
        slide.AddElement(element);

        Assert.Throws<DocumentValidationException>(() => slide.AddElement(element));
        Assert.Single(slide.Elements);
    }

    [Fact]
    public void Slide_DifferentObjectsWithSameId_AreRejected()
    {
        // Cas d'un fichier piégé : deux éléments déclarent le même identifiant.
        Guid id = Guid.NewGuid();
        var first = new TextElement(id, TestSupport.ValidFrame(), "a", "Arial", 12, false, false, ArgbColor.Black, TextAlignment.Left);
        var second = new TextElement(id, TestSupport.ValidFrame(), "b", "Arial", 12, false, false, ArgbColor.Black, TextAlignment.Left);

        Assert.Throws<DocumentValidationException>(() => _ = new Slide(Guid.NewGuid(), ArgbColor.White, [first, second]));
    }

    [Fact]
    public void Slide_EmptyGuid_IsRejected()
    {
        Assert.Throws<DocumentValidationException>(() => _ = new Slide(Guid.Empty, ArgbColor.White, []));
    }

    [Fact]
    public void Slide_ElementsList_CannotBeCastToMutableList()
    {
        var slide = new Slide();

        object exposed = slide.Elements;

        Assert.False(exposed is List<SlideElement>);
        Assert.False(exposed is ICollection<SlideElement> { IsReadOnly: false });
    }

    [Fact]
    public void Slide_ReorderAndRemove_KeepStackConsistent()
    {
        var slide = new Slide();
        TextElement a = TestSupport.NewText("a");
        TextElement b = TestSupport.NewText("b");
        TextElement c = TestSupport.NewText("c");
        slide.AddElement(a);
        slide.AddElement(b);
        slide.AddElement(c);

        slide.MoveElement(c, 0);
        int removedAt = slide.RemoveElement(b);

        Assert.Equal(2, removedAt);
        Assert.True(ReferenceEquals(slide.Elements[0], c));
        Assert.True(ReferenceEquals(slide.Elements[1], a));
        Assert.Throws<InvalidOperationException>(() => slide.RemoveElement(b));
        Assert.Throws<ArgumentOutOfRangeException>(() => slide.MoveElement(a, 2));
    }

    // ------------------------------------------------------------------ Copie profonde

    [Fact]
    public void Clone_HasNewIdentifiersButSameContent()
    {
        var slide = new Slide();
        slide.AddElement(TestSupport.NewText("titre"));
        slide.AddElement(TestSupport.NewShape(ShapeKind.Ellipse));
        slide.AddElement(new ImageElement(TestSupport.ValidFrame(), "logo.png"));
        slide.Background = ArgbColor.FromRgb(10, 20, 30);

        Slide copy = slide.Clone();

        Assert.True(copy.Id != slide.Id);
        Assert.Equal(slide.Background, copy.Background);
        Assert.Collection(copy.Elements,
            _ => { },
            _ => { },
            _ => { });
        for (int i = 0; i < slide.Elements.Count; i++)
        {
            Assert.True(copy.Elements[i].Id != slide.Elements[i].Id);
            Assert.Equal(slide.Elements[i].Frame, copy.Elements[i].Frame);
            Assert.Equal(slide.Elements[i].GetType(), copy.Elements[i].GetType());
        }

        Assert.Equal("titre", ((TextElement)copy.Elements[0]).Text);
        Assert.Equal(ShapeKind.Ellipse, ((ShapeElement)copy.Elements[1]).Kind);
        Assert.Equal("logo.png", ((ImageElement)copy.Elements[2]).MediaName);
    }

    [Fact]
    public void Clone_IsIndependentOfTheOriginal()
    {
        var slide = new Slide();
        var text = TestSupport.NewText("original");
        slide.AddElement(text);

        Slide copy = slide.Clone();
        ((TextElement)copy.Elements[0]).Text = "modifié";
        copy.Elements[0].Frame = new ElementFrame(1, 2, 3, 4);
        copy.AddElement(TestSupport.NewText());

        Assert.Equal("original", text.Text);
        Assert.Equal(TestSupport.ValidFrame(), text.Frame);
        Assert.Single(slide.Elements);
    }

    [Fact]
    public void ElementClone_IsOfTheSameTypeWithNewId()
    {
        TextElement text = TestSupport.NewText("x");
        text.IsBold = true;
        text.Alignment = TextAlignment.Center;

        TextElement copy = text.Clone();

        Assert.True(copy.Id != text.Id);
        Assert.True(copy.IsBold);
        Assert.Equal(TextAlignment.Center, copy.Alignment);
    }
}
