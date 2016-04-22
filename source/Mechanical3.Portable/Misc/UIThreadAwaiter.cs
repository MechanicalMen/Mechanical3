using System;
using System.Runtime.CompilerServices;
using Mechanical3.Core;
using Mechanical3.MVVM;

namespace Mechanical3.Misc
{
    /// <summary>
    /// Implements a custom awaiter for <see cref="UI.CatchAsync"/>.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:Elements must be documented", Justification = "Only the compiler, or internal code will ever use these members.")]
    public struct UIThreadAwaiter : INotifyCompletion
    {
#pragma warning disable 1591 // Missing XML comment for publicly visible type or member

        private readonly IUIThreadHandler uiThread;

        public UIThreadAwaiter( IUIThreadHandler uiThreadHandler )
        {
            if( uiThreadHandler.NullReference() )
                throw new ArgumentNullException(nameof(uiThreadHandler)).StoreFileLine();

            this.uiThread = uiThreadHandler;
            this.IsCompleted = this.uiThread.IsOnUIThread(); // == !invokeRequired
        }

        public UIThreadAwaiter GetAwaiter()
        {
            return this;
        }

        public bool IsCompleted { get; }

        public void OnCompleted( Action continuation )
        {
            this.uiThread.BeginInvoke(continuation);
        }

        public void GetResult()
        {
            //// no value to return, no exceptions to propagate
        }

#pragma warning restore 1591 // Missing XML comment for publicly visible type or member
    }
}
