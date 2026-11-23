using System.Globalization;
using System.Text;
using SlideStudio.Core.Editing;
using SlideStudio.Core.Model;

namespace SlideStudio.Core.Tests;

/// <summary>Fabriques et doublures de test partagées (aucun test ici).</summary>
internal static class TestSupport
{
    public static ElementFrame ValidFrame() => new(100, 100, 300, 200);

    public static TextElement NewText(string text = "Bonjour") => new(ValidFrame(), text);

    public static ShapeElement NewShape(ShapeKind kind = ShapeKind.Rectangle) => new(kind, ValidFrame());

    /// <summary>
    /// Décrit TOUT le contenu d'une présentation dans une chaîne : deux états identiques donnent la même chaîne.
    /// Sert à prouver qu'annuler ramène exactement à l'état d'avant (identifiants, ordre, valeurs).
    /// </summary>
    public static string Snapshot(Presentation presentation)
    {
        var builder = new StringBuilder();
        builder.Append(CultureInfo.InvariantCulture, $"{presentation.Title}|");

        foreach (Slide slide in presentation.Slides)
        {
            builder.Append(CultureInfo.InvariantCulture, $"S:{slide.Id}:{slide.Background}[");
            foreach (SlideElement element in slide.Elements)
            {
                Frame(builder, element);
                switch (element)
                {
                    case TextElement text:
                        builder.Append(CultureInfo.InvariantCulture,
                            $"T:{text.Text}:{text.FontFamily}:{text.FontSizePoints}:{text.IsBold}:{text.IsItalic}:{text.Color}:{text.Alignment}");
                        break;
                    case ShapeElement shape:
                        builder.Append(CultureInfo.InvariantCulture,
                            $"H:{shape.Kind}:{shape.Fill}:{shape.Stroke}:{shape.StrokeWidth}");
                        break;
                    case ImageElement image:
                        builder.Append(CultureInfo.InvariantCulture, $"I:{image.MediaName}:{image.AltText}");
                        break;
                    default:
                        break;
                }

                builder.Append(';');
            }

            builder.Append("],");
        }

        return builder.ToString();
    }

    private static void Frame(StringBuilder builder, SlideElement element)
    {
        ElementFrame f = element.Frame;
        builder.Append(CultureInfo.InvariantCulture,
            $"{element.Id}@{f.X},{f.Y},{f.Width},{f.Height},{f.RotationDegrees}/");
    }
}

/// <summary>Fausse commande : compte les appels et peut échouer à la demande.</summary>
internal sealed class FakeCommand : IUndoableCommand
{
    public FakeCommand(string description = "Fausse commande")
    {
        Description = description;
    }

    public string Description { get; }

    public bool NoOp { get; set; }

    public bool ThrowOnExecute { get; set; }

    public bool ThrowOnUndo { get; set; }

    public int ExecuteCount { get; private set; }

    public int UndoCount { get; private set; }

    public bool IsNoOp => NoOp;

    public void Execute()
    {
        if (ThrowOnExecute)
        {
            throw new InvalidOperationException("échec simulé à l'exécution");
        }

        ExecuteCount++;
    }

    public void Undo()
    {
        if (ThrowOnUndo)
        {
            throw new InvalidOperationException("échec simulé à l'annulation");
        }

        UndoCount++;
    }
}
