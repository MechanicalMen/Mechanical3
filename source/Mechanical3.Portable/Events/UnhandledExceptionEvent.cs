using System;
using Mechanical3.Core;

namespace Mechanical3.Events
{
    /// <summary>
    /// Represents an unhandled exception.
    /// </summary>
    public sealed class UnhandledExceptionEvent : EventBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UnhandledExceptionEvent"/> class.
        /// </summary>
        /// <param name="exception">The unhandled exception to wrap.</param>
        public UnhandledExceptionEvent( Exception exception )
        {
            if( exception.NotNullReference() )
                this.Exception = exception;
            else
                this.Exception = new ArgumentNullException(nameof(exception)).StoreFileLine();
        }

        /// <summary>
        /// Gets the unhandled exception.
        /// </summary>
        /// <value>The unhandled exception.</value>
        public Exception Exception { get; }
    }
}
