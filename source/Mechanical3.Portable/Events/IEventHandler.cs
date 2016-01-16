namespace Mechanical3.Events
{
    /// <summary>
    /// Subscribes to, and handles events of type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The type of events to handle.</typeparam>
    public interface IEventHandler<in T>
        where T : EventBase
    {
        /// <summary>
        /// Handles the specified event.
        /// </summary>
        /// <param name="evnt">The event to handle.</param>
        void Handle( T evnt );
    }

    /* NOTE: There are two main ways you can write an asynchronous event handler:
     *
     *       The simplest way is to just block the calling method, until you finish.
     *       You can do this via "task.GetAwaiter().GetResult()" (or if you prefer: "task.Wait()")
     *
     *       The above method has the drawback, that other events/event handlers are not processed
     *       until it finishes. The other thing you could do, is to do something like this:
     *
     *           eventQueue.Enqueue(new MyAsyncOperationStartingEvent());
     *           MyOperationAsync().ContinueWith(t => eventQueue.Enqueue(new MyAsyncOperationEndedEvent()));
     *           //// NOTE: You should handle exceptions, and any return value of the operation.
     *           ////       Also, depending on the use case, you may have no need of the first event
     */
}
