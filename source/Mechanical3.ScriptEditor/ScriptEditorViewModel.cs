using System;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Mechanical3.Core;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;

namespace Mechanical3.ScriptEditor
{
    /// <summary>
    /// The view-model to use with the <see cref="ScriptEditorControl"/>.
    /// </summary>
    public class ScriptEditorViewModel : DisposableObject, INotifyPropertyChanged
    {
        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ScriptEditorViewModel"/> class.
        /// </summary>
        public ScriptEditorViewModel()
        {
            this.InitializeScripting();
        }

        #endregion

        #region IDisposableObject

        /// <summary>
        /// Called when the object is being disposed of. Inheritors must call base.OnDispose to be properly disposed.
        /// </summary>
        /// <param name="disposing">If set to <c>true</c>, release both managed and unmanaged resources; otherwise release only the unmanaged resources.</param>
        protected override void OnDispose( bool disposing )
        {
            if( disposing )
            {
                //// dispose-only (i.e. non-finalizable) logic
                //// (managed, disposable resources you own)

                this.ReleaseScripting();
            }

            //// shared cleanup logic
            //// (unmanaged resources)


            base.OnDispose(disposing);
        }

        #endregion

        #region INotifyPropertyChanged

        /// <summary>
        /// Occurs when a property value changes.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged( [CallerMemberName] string member = "" )
        {
            var handlers = this.PropertyChanged;
            if( handlers.NotNullReference() )
                handlers(this, new PropertyChangedEventArgs(member));
        }

        #endregion

        #region Text editor

        private string code = null;

        /// <summary>
        /// Gets or sets the C# code to run.
        /// </summary>
        /// <value>The C# code to run.</value>
        public string Code
        {
            get
            {
                return this.code;
            }
            set
            {
                if( !string.Equals(this.code, value, StringComparison.Ordinal) )
                {
                    this.code = value;
                    this.NotifyPropertyChanged();
                }
            }
        }

        #endregion

        #region Script session handling

        //// based on: https://github.com/dotnet/roslyn/wiki/Scripting-API-Samples

        private SemaphoreSlim scriptSemaphore;
        private ScriptState scriptState;
        private ScriptOptions scriptOptions;

        private void InitializeScripting()
        {
            this.scriptSemaphore = new SemaphoreSlim(initialCount: 1, maxCount: 1); // on request at a time
            this.ResetCoreAsync().GetAwaiter().GetResult();
        }

        private void ReleaseScripting()
        {
            if( this.scriptSemaphore.NotNullReference() )
            {
                this.scriptSemaphore.Dispose();
                this.scriptSemaphore = null;
            }

            this.scriptState = null;
        }

        /// <summary>
        /// Runs the C# script in the <see cref="Code"/> property asynchronously.
        /// </summary>
        /// <returns>An object representing the asynchronous operation.</returns>
        public async Task RunCodeAsync()
        {
            this.ThrowIfDisposed();

            string code = this.Code;
            if( code.NullOrWhiteSpace() )
                return;

            // only execute one script at a time
            await this.scriptSemaphore.WaitAsync();

            // do not block the current thread (using await still blocks it! see: https://github.com/dotnet/roslyn/issues/6928)
            await Task.Run(() =>
            {
                try
                {
                    if( this.scriptState.NullReference() )
                        this.scriptState = CSharpScript.RunAsync(code, this.scriptOptions).Result;
                    else
                        this.scriptState = this.scriptState.ContinueWithAsync(code, this.scriptOptions).Result;
                }
                catch( CompilationErrorException ex )
                {
                    var diagnostics = string.Join(Environment.NewLine, ex.Diagnostics);
                    Log.Debug("Failed to run script!", ex.Store(nameof(this.Code), code).Store(nameof(ex.Diagnostics), diagnostics));

                    MessageBox.Show(
                        diagnostics,
                        "Script error!",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning,
                        MessageBoxResult.OK);
                }
                finally
                {
                    this.scriptSemaphore.Release();
                }
            });
        }

        /// <summary>
        /// Resets the state of the script session.
        /// </summary>
        /// <returns>An object representing the asynchronous operation.</returns>
        public async Task ResetAsync()
        {
            this.ThrowIfDisposed();

            await this.scriptSemaphore.WaitAsync();
            try
            {
                await this.ResetCoreAsync();
            }
            finally
            {
                this.scriptSemaphore.Release();
            }
        }

        private async Task ResetCoreAsync()
        {
            // reset
            this.scriptState = null;
            this.scriptOptions = ScriptOptions.Default;

            // add common assemblies
            var mscorlib = typeof(object).Assembly;
            var systemCore = typeof(System.Linq.Enumerable).Assembly;
            this.scriptOptions = this.scriptOptions.AddReferences(mscorlib, systemCore);

            // add common namespaces
            this.scriptState = await CSharpScript.RunAsync("using System;", this.scriptOptions);
            this.scriptState = await this.scriptState.ContinueWithAsync("using System.Linq;", this.scriptOptions);
            this.scriptState = await this.scriptState.ContinueWithAsync("using System.Collections.Generic;", this.scriptOptions);
        }

        /// <summary>
        /// Adds the specified assembly to the script session.
        /// </summary>
        /// <param name="assembly">The <see cref="Assembly"/> to add to the current session.</param>
        public void AddAssembly( Assembly assembly )
        {
            this.ThrowIfDisposed();

            if( assembly.NullReference() )
                throw new ArgumentNullException(nameof(assembly)).StoreFileLine();

            Interlocked.Exchange(ref this.scriptOptions, this.scriptOptions.AddReferences(assembly));
        }

        #endregion
    }
}
