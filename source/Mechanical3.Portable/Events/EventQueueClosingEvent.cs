namespace Mechanical3.Events
{
    /// <summary>
    /// This event is raised by an <see cref="IEventQueue"/> to indicate that it is being closed.
    /// No more events can be enqueued, after all it's handlers have run.
    /// </summary>
    public class EventQueueClosingEvent : EventBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EventQueueClosingEvent"/> class.
        /// </summary>
        internal EventQueueClosingEvent()
        {
        }
    }
}
