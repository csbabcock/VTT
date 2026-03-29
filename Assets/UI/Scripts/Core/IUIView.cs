using UnityEngine.UIElements;

namespace GameCore.UI
{
    /// <summary>
    /// Base interface for UI Toolkit views.
    /// </summary>
    public interface IUIView
    {
        /// <summary>
        /// Root visual element of this view.
        /// </summary>
        VisualElement Root { get; }

        /// <summary>
        /// Called once when the view is created and bound to a runtime panel (<see cref="T:UnityEngine.UIElements.PanelRenderer"/>).
        /// </summary>
        void Initialize();

        /// <summary>
        /// Show the view (enable input / make visible).
        /// </summary>
        void Show();

        /// <summary>
        /// Hide the view (disable input / make invisible).
        /// </summary>
        void Hide();
    }

    /// <summary>
    /// Optional generic view interface for views that render a strongly-typed state.
    /// This pairs naturally with IUIModel&lt;TState&gt; and lets presenters simply
    /// forward state snapshots from the model to the view.
    /// </summary>
    /// <typeparam name="TState">Struct or class representing the view-model state.</typeparam>
    public interface IUIView<TState> : IUIView
    {
        /// <summary>
        /// Update the view using the provided state snapshot.
        /// </summary>
        /// <param name="state">Latest state from the model.</param>
        void UpdateView(TState state);
    }
}
