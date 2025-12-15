using System;

namespace GameCore.UI
{
    /// <summary>
    /// Marker interface for UI models.
    /// Extend this for specific UI screens to hold state and raise events.
    /// </summary>
    public interface IUIModel
    {
    }

    /// <summary>
    /// Optional generic base interface for strongly typed model state.
    /// </summary>
    /// <typeparam name="TState">Struct or class representing the model's state.</typeparam>
    public interface IUIModel<TState> : IUIModel
    {
        TState State { get; }

        /// <summary>
        /// Raised when the model state changes.
        /// </summary>
        event Action<TState> StateChanged;
    }
}

