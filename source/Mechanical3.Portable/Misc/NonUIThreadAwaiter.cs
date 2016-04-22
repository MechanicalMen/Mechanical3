using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Mechanical3.MVVM;

namespace Mechanical3.Misc
{
    /// <summary>
    /// Implements a custom awaiter for <see cref="UI.ReleaseAsync"/>.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:Elements must be documented", Justification = "Only the compiler, or internal code will ever use these members.")]
    public struct NonUIThreadAwaiter : INotifyCompletion
    {
#pragma warning disable 1591 // Missing XML comment for publicly visible type or member

        public NonUIThreadAwaiter( bool isOnUI )
        {
            this.IsCompleted = !isOnUI;
        }

        public NonUIThreadAwaiter GetAwaiter()
        {
            return this;
        }

        public bool IsCompleted { get; }

        public void OnCompleted( Action continuation )
        {
            Task.Run(continuation);
        }

        public void GetResult()
        {
            //// no value to return, no exceptions to propagate
        }

#pragma warning restore 1591 // Missing XML comment for publicly visible type or member
    }
}
