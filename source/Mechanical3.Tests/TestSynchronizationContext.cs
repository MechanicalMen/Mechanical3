using System;
using System.Threading;
using Mechanical3.Core;

namespace Mechanical3.Tests
{
    /// <summary>
    /// A quick and dirty <see cref="SynchronizationContext"/> for testing.
    /// </summary>
    public class TestSynchronizationContext : SynchronizationContext
    {
        //// NOTE: based on: http://blog.gauffin.org/2015/10/07/using-a-synchronizationcontext-in-unit-tests/

        public override void Post( SendOrPostCallback d, object state )
        {
            RunOnCurrent(() => d(state));
        }

        public override void Send( SendOrPostCallback d, object state )
        {
            RunOnCurrent(() => d(state));
        }

        private void RunOnCurrent( Action action )
        {
            if( action.NullReference() )
                throw new ArgumentNullException(nameof(action));

            var originalContext = SynchronizationContext.Current;
            try
            {
                SynchronizationContext.SetSynchronizationContext(this);
                action();
            }
            finally
            {
                SynchronizationContext.SetSynchronizationContext(originalContext);
            }
        }

        public string Name { get; set; }

        public override string ToString()
        {
            if( this.Name.NullOrWhiteSpace() )
                return this.Name;
            else
                return base.ToString();
        }

        /// <summary>
        /// Runs the specified delegate using a new <see cref="SynchronizationContext"/>.
        /// </summary>
        /// <param name="action">The delegate to run.</param>
        public static void RunOnNew( Action action )
        {
            var newContext = new TestSynchronizationContext();
            newContext.RunOnCurrent(action);
        }

        /// <summary>
        /// Runs the specified delegate using a new <see cref="SynchronizationContext"/>.
        /// </summary>
        /// <param name="name">An optional name for the context, to differentiate between them.</param>
        /// <param name="action">The delegate to run.</param>
        public static void RunOnNew( string name, Action action )
        {
            var newContext = new TestSynchronizationContext() { Name = name };
            newContext.RunOnCurrent(action);
        }
    }
}
