using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Mechanical3.Core;
using Mechanical3.MVVM;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;

namespace Mechanical3.ScriptEditor
{
    /// <summary>
    /// The view-model to use with the <see cref="ScriptEditorControl"/>.
    /// </summary>
    public class ScriptEditorViewModel : PropertyChangedBase.Disposable
    {
        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ScriptEditorViewModel"/> class.
        /// </summary>
        public ScriptEditorViewModel()
            : base()
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
                    this.RaisePropertyChanged();
                }
            }
        }

        #endregion

        #region Script session handling

        //// based on: https://github.com/dotnet/roslyn/wiki/Scripting-API-Samples

        private SemaphoreSlim scriptSemaphore;
        private ScriptState scriptState;
        private ScriptOptions scriptOptions;
        private bool scriptIsRunning = false;

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
        /// Gets a value indicating whether a script is currently executing.
        /// </summary>
        /// <value><c>true</c> if a script is currently executing; otherwise, <c>false</c>.</value>
        public bool IsRunningScript
        {
            get
            {
                this.ThrowIfDisposed();

                return this.scriptIsRunning;
            }
            private set
            {
                this.ThrowIfDisposed();

                if( this.scriptIsRunning != value )
                {
                    this.scriptIsRunning = value;
                    this.RaisePropertyChanged();
                }
            }
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

            // do not block UI
            await UI.ReleaseAsync();

            // only execute one script at a time
            await this.scriptSemaphore.WaitAsync();
            this.IsRunningScript = true;

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

                UI.Invoke(() =>
                {
                    MessageBox.Show(
                        diagnostics,
                        "Script error!",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning,
                        MessageBoxResult.OK);
                });
            }
            finally
            {
                this.IsRunningScript = false;
                this.scriptSemaphore.Release();
            }
        }

        /// <summary>
        /// Resets the state of the script session.
        /// </summary>
        /// <returns>An object representing the asynchronous operation.</returns>
        public async Task ResetAsync()
        {
            this.ThrowIfDisposed();

            // do not block UI
            await UI.ReleaseAsync();

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
            var mechanical3 = typeof(SafeString).Assembly;
            this.scriptOptions = this.scriptOptions.AddReferences(mscorlib, systemCore, mechanical3);

            // add common namespaces
            this.scriptState = await CSharpScript.RunAsync("using System;", this.scriptOptions);
            this.scriptState = await this.scriptState.ContinueWithAsync("using System.Linq;", this.scriptOptions);
            this.scriptState = await this.scriptState.ContinueWithAsync("using System.Collections.Generic;", this.scriptOptions);
            this.scriptState = await this.scriptState.ContinueWithAsync("using Mechanical3.Core;", this.scriptOptions);
        }

        /// <summary>
        /// Adds the specified assembly to the script session.
        /// </summary>
        /// <param name="assembly">The <see cref="Assembly"/> to add to the current session.</param>
        /// <returns>An object representing the asynchronous operation.</returns>
        public async Task AddAssemblyAsync( Assembly assembly )
        {
            this.ThrowIfDisposed();

            if( assembly.NullReference() )
                throw new ArgumentNullException(nameof(assembly)).StoreFileLine();

            // do not block UI
            await UI.ReleaseAsync();

            await this.scriptSemaphore.WaitAsync();
            try
            {
                this.scriptOptions = this.scriptOptions.AddReferences(assembly);
            }
            finally
            {
                this.scriptSemaphore.Release();
            }
        }

        /// <summary>
        /// Adds the specified import to the script session (basically the "static imports" feature of C# 6).
        /// </summary>
        /// <param name="type">The type to import.</param>
        /// <returns>An object representing the asynchronous operation.</returns>
        public async Task AddImportAsync( Type type )
        {
            this.ThrowIfDisposed();

            if( type.NullReference() )
                throw new ArgumentNullException(nameof(type)).StoreFileLine();

            // do not block UI
            await UI.ReleaseAsync();

            await this.scriptSemaphore.WaitAsync();
            try
            {
                this.scriptOptions = this.scriptOptions.AddReferences(type.Assembly);
                this.scriptOptions = this.scriptOptions.AddImports(type.FullName);
            }
            finally
            {
                this.scriptSemaphore.Release();
            }
        }

        #endregion
    }
}
