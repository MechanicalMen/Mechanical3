namespace Mechanical3.Events
{
    /// <summary>
    /// This event is raised by the <see cref="IEventQueue"/> to indicate a request to close it.
    /// The request may be cancelled via event handlers.
    /// More than one request can be enqueued, but once one is not cancelled,
    /// no more will be raised.
    /// </summary>
    public class EventQueueCloseRequestEvent : EventBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EventQueueCloseRequestEvent"/> class.
        /// </summary>
        internal EventQueueCloseRequestEvent()
        {
            this.CanBeginClose = true;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the event queue can be closed (see <see cref="IEventQueue.BeginClose"/>).
        /// It may be ignored, if the event queue already begun shutting down.
        /// </summary>
        /// <value><c>true</c> if the event queue can begin shutting down; otherwise, <c>false</c>.</value>
        public bool CanBeginClose
        {
            get;
            set;
        }
    }
}
