using System;
using System.Threading.Tasks;
using Mechanical3.Core;

namespace Mechanical3.Events
{
    /// <summary>
    /// Provides an optional <see cref="Task"/> to represent the event handling process.
    /// Takes care of exceptions thrown by listening <see cref="IEventHandler{T}"/> instances.
    /// </summary>
    internal class EnqueuedEvent
    {
#pragma warning disable SA1600 // Elements must be documented

        #region Private Fields

        private readonly EventBase evnt;
        private readonly TaskCompletionSource<object> tsc;

        #endregion

        #region Constructor

        internal EnqueuedEvent( EventBase evnt, bool createTask )
        {
            if( evnt.NullReference() )
                throw new ArgumentNullException(nameof(evnt)).StoreFileLine();

            this.evnt = evnt;
            this.tsc = createTask ? new TaskCompletionSource<object>() : null;
        }

        #endregion

        #region Internal Members

        internal EventBase Event
        {
            get { return this.evnt; }
        }

        internal Task Task
        {
            get { return this.tsc.NullReference() ? null : this.tsc.Task; }
        }

        internal bool CanHandleUnhandledExceptions
        {
            get { return this.tsc.NotNullReference(); } // if event handlers throw exceptions, they can be handled through a task
        }

        internal void SetHandled( Exception unhandledException )
        {
            if( this.tsc.NotNullReference() )
            {
                if( unhandledException.NullReference() )
                    this.tsc.SetResult(null);
                else
                    this.tsc.SetException(unhandledException); // at least one of the handlers threw an exception, and there is someone to listen to it
            }
        }

        #endregion

#pragma warning restore SA1600 // Elements must be documented
    }
}
