using SlideStudio.Core.Editing.Commands;
using SlideStudio.Core.Model;

namespace SlideStudio.Core.Editing;

/// <summary>
/// Fabrique de commandes prêtes à l'emploi pour les modifications courantes.
/// L'interface appelle par exemple <c>history.Execute(EditCommands.SetText(element, "Bonjour"))</c>.
/// </summary>
public static class EditCommands
{
    /// <summary>Déplace, redimensionne ou pivote un élément.</summary>
    /// <param name="element">Élément à modifier.</param>
    /// <param name="frame">Nouveau cadre.</param>
    /// <param name="gestureId">Identifiant du glisser en cours (fusionne les étapes intermédiaires), ou <c>null</c>.</param>
    /// <returns>Commande à passer à <see cref="UndoHistory.Execute"/>.</returns>
    public static IUndoableCommand SetFrame(SlideElement element, ElementFrame frame, Guid? gestureId = null) =>
        new SetPropertyCommand<SlideElement, ElementFrame>(
            element, nameof(SlideElement.Frame), "Déplacer ou redimensionner", e => e.Frame, (e, v) => e.Frame = v, frame, gestureId);

    /// <summary>Remplace le contenu d'une zone de texte.</summary>
    /// <param name="element">Zone de texte.</param>
    /// <param name="text">Nouveau texte.</param>
    /// <param name="gestureId">Identifiant d'une série de frappes (une étape d'annulation par série), ou <c>null</c>.</param>
    /// <returns>Commande à passer à <see cref="UndoHistory.Execute"/>.</returns>
    public static IUndoableCommand SetText(TextElement element, string text, Guid? gestureId = null) =>
        new SetPropertyCommand<TextElement, string>(
            element, nameof(TextElement.Text), "Modifier le texte", e => e.Text, (e, v) => e.Text = v, text, gestureId);

    /// <summary>Change la taille de police.</summary>
    /// <param name="element">Zone de texte.</param>
    /// <param name="points">Taille en points.</param>
    /// <param name="gestureId">Identifiant d'un glissement de curseur, ou <c>null</c>.</param>
    /// <returns>Commande à passer à <see cref="UndoHistory.Execute"/>.</returns>
    public static IUndoableCommand SetFontSize(TextElement element, double points, Guid? gestureId = null) =>
        new SetPropertyCommand<TextElement, double>(
            element, nameof(TextElement.FontSizePoints), "Modifier la taille de police", e => e.FontSizePoints, (e, v) => e.FontSizePoints = v, points, gestureId);

    /// <summary>Active ou désactive le gras.</summary>
    /// <param name="element">Zone de texte.</param>
    /// <param name="isBold">Nouvel état.</param>
    /// <returns>Commande à passer à <see cref="UndoHistory.Execute"/>.</returns>
    public static IUndoableCommand SetBold(TextElement element, bool isBold) =>
        new SetPropertyCommand<TextElement, bool>(
            element, nameof(TextElement.IsBold), "Gras", e => e.IsBold, (e, v) => e.IsBold = v, isBold);

    /// <summary>Active ou désactive l'italique.</summary>
    /// <param name="element">Zone de texte.</param>
    /// <param name="isItalic">Nouvel état.</param>
    /// <returns>Commande à passer à <see cref="UndoHistory.Execute"/>.</returns>
    public static IUndoableCommand SetItalic(TextElement element, bool isItalic) =>
        new SetPropertyCommand<TextElement, bool>(
            element, nameof(TextElement.IsItalic), "Italique", e => e.IsItalic, (e, v) => e.IsItalic = v, isItalic);

    /// <summary>Change la couleur du texte.</summary>
    /// <param name="element">Zone de texte.</param>
    /// <param name="color">Nouvelle couleur.</param>
    /// <returns>Commande à passer à <see cref="UndoHistory.Execute"/>.</returns>
    public static IUndoableCommand SetTextColor(TextElement element, ArgbColor color) =>
        new SetPropertyCommand<TextElement, ArgbColor>(
            element, nameof(TextElement.Color), "Couleur du texte", e => e.Color, (e, v) => e.Color = v, color);

    /// <summary>Change le remplissage d'une forme.</summary>
    /// <param name="element">Forme.</param>
    /// <param name="color">Nouvelle couleur (transparente = aucun remplissage).</param>
    /// <returns>Commande à passer à <see cref="UndoHistory.Execute"/>.</returns>
    public static IUndoableCommand SetShapeFill(ShapeElement element, ArgbColor color) =>
        new SetPropertyCommand<ShapeElement, ArgbColor>(
            element, nameof(ShapeElement.Fill), "Remplissage de la forme", e => e.Fill, (e, v) => e.Fill = v, color);

    /// <summary>Change la couleur de fond d'une diapositive.</summary>
    /// <param name="slide">Diapositive.</param>
    /// <param name="color">Nouvelle couleur de fond.</param>
    /// <returns>Commande à passer à <see cref="UndoHistory.Execute"/>.</returns>
    public static IUndoableCommand SetSlideBackground(Slide slide, ArgbColor color) =>
        new SetPropertyCommand<Slide, ArgbColor>(
            slide, nameof(Slide.Background), "Fond de la diapositive", s => s.Background, (s, v) => s.Background = v, color);

    /// <summary>Renomme la présentation.</summary>
    /// <param name="presentation">Présentation.</param>
    /// <param name="title">Nouveau titre.</param>
    /// <returns>Commande à passer à <see cref="UndoHistory.Execute"/>.</returns>
    public static IUndoableCommand SetTitle(Presentation presentation, string title) =>
        new SetPropertyCommand<Presentation, string>(
            presentation, nameof(Presentation.Title), "Renommer la présentation", p => p.Title, (p, v) => p.Title = v, title);
}
