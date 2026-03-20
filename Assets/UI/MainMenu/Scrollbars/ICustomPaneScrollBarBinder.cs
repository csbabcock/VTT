using UnityEngine.UIElements;

namespace GameCore.UI.MainMenu.Scrollbars
{
    /// <summary>
    /// Wires custom vertical scroll chrome (track + thumb) for ScrollViews that opt in via UXML layout.
    /// Keeps <see cref="CharacterCreationView"/> free of scrollbar mechanics (Single Responsibility / DIP).
    /// </summary>
    public interface ICustomPaneScrollBarBinder
    {
        void BindTree(VisualElement root);
    }
}
