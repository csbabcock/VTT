namespace GameCore.UI.MainMenu
{
    /// <summary>
    /// View-model for a character selection card: the display strings the menu view needs,
    /// supplied by the presenter so the view does not depend on file/IO services.
    /// </summary>
    public readonly struct CharacterCardDisplay
    {
        public readonly string Title;
        public readonly string Subtitle;

        public CharacterCardDisplay(string title, string subtitle)
        {
            Title = title;
            Subtitle = subtitle;
        }
    }
}
