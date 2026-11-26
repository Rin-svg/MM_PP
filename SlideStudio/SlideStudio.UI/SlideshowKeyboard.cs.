
using System.Windows.Forms;

namespace SlideStudio.UI;

/// <summary>
/// Représente les commandes disponibles pendant le diaporama.
/// </summary>
public enum SlideshowCommand
{
    None,
    Next,
    Previous,
    Exit,
    ToggleBlackScreen,
    ShowHelp
}

/// <summary>
/// Associe les touches du clavier aux commandes du diaporama.
/// </summary>
public static class SlideshowKeyboard
{
    /// <summary>
    /// Identifie la commande correspondant à une touche pressée.
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
