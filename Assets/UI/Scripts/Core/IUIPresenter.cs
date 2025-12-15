namespace GameCore.UI
{
    /// <summary>
    /// Non-generic base presenter interface.
    /// </summary>
    public interface IUIPresenter
    {
        /// <summary>
        /// Initialize the presenter and subscribe to model / view events.
        /// </summary>
        void Initialize();

        /// <summary>
        /// Clean up subscriptions.
        /// </summary>
        void Dispose();
    }

    /// <summary>
    /// Generic presenter base interface that connects a model and a view.
    /// </summary>
    public interface IUIPresenter<TModel, TView> : IUIPresenter
        where TModel : class, IUIModel
        where TView : class, IUIView
    {
        TModel Model { get; }
        TView View { get; }
    }
}

