using Mechanical3.Misc;

namespace Mechanical3.Events
{
    /// <summary>
    /// Represents an event, that subscribers can listen to using an event queue.
    /// </summary>
    public abstract class EventBase
    {
        /// <summary>
        /// Gets the source code position of the last time this event was enqueued.
        /// </summary>
        /// <value>The source code position of the last time this event was enqueued.</value>
        public FileLineInfo? EnqueueSource { get; internal set; } = null;
    }
}
