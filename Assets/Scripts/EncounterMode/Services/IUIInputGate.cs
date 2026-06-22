namespace GameCore.EncounterMode.Services
{
    /// <summary>
    /// Abstraction over UI state that gates world input (grid clicks, camera rotation) while
    /// the player is interacting with on-screen UI. Replaces direct singleton access so
    /// consumers depend on an injectable seam that can be faked in tests.
    /// </summary>
    public interface IUIInputGate
    {
        /// <summary>Whether the character sheet panel is currently open.</summary>
        bool IsCharacterSheetOpen();

        /// <summary>Whether the mouse is currently over the character sheet UI.</summary>
        bool IsMouseOverCharacterSheet();

        /// <summary>Whether world input should be blocked because the pointer is over HUD UI.</summary>
        bool ShouldBlockInput();
    }
}
