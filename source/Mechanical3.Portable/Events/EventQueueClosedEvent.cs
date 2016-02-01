namespace Mechanical3.Events
{
    /// <summary>
    /// This event is raised by the <see cref="IEventQueue"/> to indicate that it is being closed.
    /// This is always the last event in the queue.
    /// Only the most fundamental resources should be released here (like logging),
    /// anything else should be released at or after the Closing event.
    /// </summary>
    public class EventQueueClosedEvent : EventBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EventQueueClosedEvent"/> class.
        /// </summary>
        /// <param name="timeLimit">Indicates to handlers that closing the application must happen as soon as possible.</param>
        internal EventQueueClosedEvent( bool timeLimit )
        {
            this.LimitedTimeAvailable = timeLimit;
        }

        /// <summary>
        /// Gets a value indicating whether closing the application must happen as soon as possible.
        /// If <c>true</c>, then likely there is an uninterruptable system event (like shutdown or logoff)
        /// that forces the application to finish in only a few seconds.
        /// </summary>
        /// <value><c>false</c> to implement the standard closing procedure; <c>true</c> to skip steps that could take more than one or two seconds.</value>
        public bool LimitedTimeAvailable { get; }
    }
}
