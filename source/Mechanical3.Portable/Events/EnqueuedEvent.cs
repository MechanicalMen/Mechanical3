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

        internal void SetHandled( Exception unhandledException, out bool exceptionHandled )
        {
            if( unhandledException.NullReference() )
            {
                // no handlers threw exceptions: finish up normally
                if( this.tsc.NotNullReference() )
                    this.tsc.SetResult(null);

                exceptionHandled = true;
            }
            else
            {
                // at least one of the handlers threw an exception
                if( this.tsc.NotNullReference() )
                {
                    this.tsc.SetException(unhandledException);
                    exceptionHandled = true;
                }
                else
                {
                    exceptionHandled = false;
                }
            }
        }

        #endregion

#pragma warning restore SA1600 // Elements must be documented
    }
}
