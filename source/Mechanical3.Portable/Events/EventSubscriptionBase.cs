using System;
using System.Reflection;

namespace Mechanical3.Events
{
    /// <summary>
    /// Provides a non-generic interface for accessing a generic <see cref="IEventHandler{T}"/>.
    /// Uses a weak-reference internally.
    /// </summary>
    internal abstract class EventSubscriptionBase
    {
#pragma warning disable SA1600 // Elements must be documented

        internal abstract Type EventType { get; }

        internal abstract object AcquireStrongRef();

        internal abstract void ReleaseStrongRef();

        internal abstract void Handle( EventBase evnt );

        internal abstract bool CanHandle( TypeInfo eventType );

#pragma warning restore SA1600 // Elements must be documented
    }
}
