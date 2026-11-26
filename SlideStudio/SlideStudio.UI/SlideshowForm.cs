
private void SlideshowForm_KeyDown(object? sender, KeyEventArgs e)
{
    SlideshowCommand command = SlideshowKeyboard.GetCommand(e.KeyCode);

    switch (command)
    {
        case SlideshowCommand.Exit:
            Close();
            break;

        case SlideshowCommand.ToggleBlackScreen:
            _blackScreen = !_blackScreen;
            _pictureBox.Visible = !_blackScreen;
            break;

        case SlideshowCommand.Next:
            if (!_blackScreen)
            {
                int count = _presentationSlides?.Length ?? _slides.Count;

                if (_currentIndex < count - 1)
                {
                    _currentIndex++;
                    ShowSlide();
                }
            }
            break;

        case SlideshowCommand.Previous:
            if (!_blackScreen && _currentIndex > 0)
            {
                _currentIndex--;
                ShowSlide();
            }
            break;
    }

    if (command != SlideshowCommand.None)
    {
        e.Handled = true;
        e.SuppressKeyPress = true;
    }
}

