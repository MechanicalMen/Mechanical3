using System;
using System.Threading;
using System.Threading.Tasks;
using Mechanical3.Core;
using Mechanical3.Misc;

namespace Mechanical3.MVVM
{
    /// <summary>
    /// UI thread and GUI related helpers.
    /// </summary>
    public static class UI
    {
        #region Private Static Fields

        private static IUIThreadHandler uiThreadHandler = null;

        #endregion

        #region Internal Static Members

        /// <summary>
        /// Initializes the <see cref="UI"/> class.
        /// </summary>
        /// <param name="uiThread">An object representing the UI thread.</param>
        internal static void Initialize( IUIThreadHandler uiThread )
        {
            if( uiThread.NullReference() )
                throw new ArgumentNullException(nameof(uiThread)).StoreFileLine();

            var previousValue = Interlocked.CompareExchange(ref uiThreadHandler, uiThread, comparand: null);
            if( previousValue.NotNullReference() )
                throw new InvalidOperationException("UI already initialized!").StoreFileLine();
        }

        #endregion

        #region Private Static Members

        private static IUIThreadHandler GetUIHandler()
        {
            if( uiThreadHandler.NullReference() )
                throw new InvalidOperationException("UI not initialized! (Call Initialize, or use the attached property of WpfDesigner.)").StoreFileLine();

            return uiThreadHandler;
        }

        #endregion

        #region Public Static Methods

        #region Invoke[Async]

        /// <summary>
        /// Executes the specified <see cref="Action"/> synchronously on the UI thread.
        /// </summary>
        /// <param name="action">The delegate to invoke.</param>
        public static void Invoke( Action action )
        {
            if( action.NullReference() )
                throw new ArgumentNullException().StoreFileLine();

            var handler = GetUIHandler();
            if( handler.IsOnUIThread() )
                action();
            else
                handler.Invoke(action);
        }

        /// <summary>
        /// Executes the specified <see cref="Func{TResult}"/> synchronously on the UI thread.
        /// </summary>
        /// <typeparam name="TResult">The return value type of the specified delegate.</typeparam>
        /// <param name="func">The delegate to invoke.</param>
        /// <returns>The return value of the delegate.</returns>
        public static TResult Invoke<TResult>( Func<TResult> func )
        {
            if( func.NullReference() )
                throw new ArgumentNullException().StoreFileLine();

            var handler = GetUIHandler();
            if( handler.IsOnUIThread() )
            {
                return func();
            }
            else
            {
                var result = default(TResult);
                handler.Invoke(() => result = func());
                return result;
            }
        }

        /// <summary>
        /// Executes the specified <see cref="Action"/> asynchronously on the UI thread.
        /// </summary>
        /// <param name="action">The delegate to invoke.</param>
        /// <returns>The <see cref="Task"/> representing the operation.</returns>
        public static Task InvokeAsync( Action action )
        {
            if( action.NullReference() )
                throw new ArgumentNullException().StoreFileLine();

            //// NOTE: we do not check IsOnUIThread: this will always run asynchronously!
            ////       (we do this so that the caller can be sure whether a method blocks or not)

            var tsc = new TaskCompletionSource<object>();
            GetUIHandler().BeginInvoke(
                () =>
                {
                    try
                    {
                        action();
                        tsc.SetResult(null);
                    }
                    catch( Exception ex )
                    {
                        ex.StoreFileLine();
                        tsc.SetException(ex);
                    }
                });
            return tsc.Task;
        }

        /// <summary>
        /// Executes the specified <see cref="Func{TResult}"/> asynchronously on the UI thread.
        /// </summary>
        /// <typeparam name="TResult">The return value type of the specified delegate.</typeparam>
        /// <param name="func">The delegate to invoke.</param>
        /// <returns>The <see cref="Task{TResult}"/> representing the operation.</returns>
        public static Task<TResult> InvokeAsync<TResult>( Func<TResult> func )
        {
            if( func.NullReference() )
                throw new ArgumentNullException().StoreFileLine();

            var result = default(TResult);
            return InvokeAsync((Action)(() => result = func()))
                .ContinueWith(prevTask => result, TaskContinuationOptions.OnlyOnRanToCompletion);
        }

        #endregion

        #region CatchAsync, ReleaseAsync

        /* NOTE: Inspirations:
         *        - ControlAwaiter from "await anything": http://blogs.msdn.com/b/pfxteam/archive/2011/01/13/10115642.aspx
         *        - http://jake.ginnivan.net/blog/2014/01/10/on-async-and-sync-contexts/
         *
         *
         * NOTE: Methods like these were part of the original async CTP, but were later removed:
         *       see: http://stackoverflow.com/questions/15363413/why-was-switchto-removed-from-async-ctp-release#15364646
         *       The issues mentioned were:
         *        - business logic should block, UI logic should use Task.Run:
         *          generally I would agree, but I would also say, that when you have to write Async methods,
         *          it's not an unreasonable expectation that they won't block the callee (hence RunOffUIAsync).
         *        - catch / finally blocks may run on unknown threads, and can not switch:
         *          The first part is true, but the latter part was resolved in C# 6
         */

        /// <summary>
        /// Awaiting this method forces the continuation to run on the UI thread.
        /// </summary>
        /// <returns>An object implementing the awaiter pattern.</returns>
        public static UIThreadAwaiter CatchAsync()
        {
            return new UIThreadAwaiter(GetUIHandler());
        }

        /// <summary>
        /// Awaiting this method forces the continuation to run on the ThreadPool.
        /// </summary>
        /// <returns>An object implementing the awaiter pattern.</returns>
        public static NonUIThreadAwaiter ReleaseAsync() // leaves the UI thread in favor of another one
        {
            return new NonUIThreadAwaiter(GetUIHandler().IsOnUIThread());
        }

        #endregion

        #endregion
    }
}
