
using System.Windows.Forms;

namespace SlideStudio.UI;

/// <summary>
/// Repr?sente les commandes disponibles pendant le diaporama.
/// </summary>
public enum SlideshowCommand
{
    /// <summary>No slideshow command.</summary>
    None,
    /// <summary>Advance to the next slide.</summary>
    Next,
    /// <summary>Return to the previous slide.</summary>
    Previous,
    /// <summary>Exit the slideshow.</summary>
    Exit,
    /// <summary>Toggle the black screen.</summary>
    ToggleBlackScreen,
    /// <summary>Show slideshow keyboard help.</summary>
    ShowHelp
}

/// <summary>
/// Associe les touches du clavier aux commandes du diaporama.
/// </summary>
public static class SlideshowKeyboard
{
    /// <summary>
    /// Identifie la commande correspondant ? une touche press?e.
    /// </summary>
    public static SlideshowCommand GetCommand(Keys key)
    {
        switch (key)
        {
            case Keys.Right:
            case Keys.Space:
                return SlideshowCommand.Next;

            case Keys.Left:
                return SlideshowCommand.Previous;

            case Keys.Escape:
                return SlideshowCommand.Exit;

            case Keys.B:
                return SlideshowCommand.ToggleBlackScreen;

            case Keys.F1:
                return SlideshowCommand.ShowHelp;

            default:
                return SlideshowCommand.None;
        }
    }
}
