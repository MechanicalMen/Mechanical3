namespace Mechanical3.Events
{
    /// <summary>
    /// This event is raised by the <see cref="IEventQueue"/> to indicate that it is being closed.
    /// This is always the last event in the queue.
    /// </summary>
    public class EventQueueClosedEvent : EventBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EventQueueClosedEvent"/> class.
        /// </summary>
        internal EventQueueClosedEvent()
        {
        }
    }
}
