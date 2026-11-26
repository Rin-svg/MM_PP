
/// <summary>
/// Gère les commandes clavier pendant la présentation.
/// </summary>
private void SlideshowForm_KeyDown(object? sender, KeyEventArgs e)
{
    // Identifier la commande correspondant à la touche pressée.
    SlideshowCommand command = SlideshowKeyboard.GetCommand(e.KeyCode);

    // Traiter la commande demandée.
    switch (command)
    {
        case SlideshowCommand.Exit:
        {
            // Fermer la fenêtre du diaporama.
            Close();
            break;
        }

        case SlideshowCommand.ToggleBlackScreen:
        {
            // Activer ou désactiver l'écran noir.
            _blackScreen = !_blackScreen;

            // Masquer ou afficher la diapositive.
            _pictureBox.Visible = !_blackScreen;
            break;
        }

        case SlideshowCommand.Next:
        {
            // Ne pas changer de diapositive pendant l'écran noir.
            if (_blackScreen)
            {
                break;
            }

            // Déterminer le nombre total de diapositives.
            int slideCount = _presentationSlides?.Length ?? _slides.Count;

            // Vérifier qu'il existe une diapositive suivante.
            if (_currentIndex < slideCount - 1)
            {
                _currentIndex++;

                // Afficher la nouvelle diapositive.
                ShowSlide();
            }

            break;
        }

        case SlideshowCommand.Previous:
        {
            // Ne pas changer de diapositive pendant l'écran noir.
            if (_blackScreen)
            {
                break;
            }

            // Vérifier qu'il existe une diapositive précédente.
            if (_currentIndex > 0)
            {
                _currentIndex--;

                // Afficher la diapositive précédente.
                ShowSlide();
            }

            break;
        }

        case SlideshowCommand.None:
        default:
        {
            // Ne rien faire pour les touches non reconnues.
            break;
        }
    }

    // Empêcher le traitement supplémentaire des commandes reconnues.
    if (command != SlideshowCommand.None)
    {
        e.Handled = true;
        e.SuppressKeyPress = true;
    }
}

