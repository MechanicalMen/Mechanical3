using Mechanical3.Core;
using Mechanical3.MVVM;

namespace Mechanical3.ScriptEditor
{
    /// <summary>
    /// The view-model of script commands.
    /// </summary>
    public class ScriptCommandViewModel : PropertyChangedBase.Disposable
    {
        #region Private Fields

        private PropertyChangedActions propertyChange;
        private ScriptCommandBase scriptCommand;
        private ScriptEditorViewModel scriptEditorVM;
        private bool isEditable = false;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ScriptCommandViewModel"/> class.
        /// </summary>
        public ScriptCommandViewModel()
            : base()
        {
            this.propertyChange = new PropertyChangedActions();
            this.propertyChange.Register(this, nameof(this.IsCodeEditable), this.UpdateGeneratedCode); // update when first switching views
            this.propertyChange.Register(
                this,
                new string[] {
                    nameof(this.ScriptCommand),
                    nameof(this.ScriptCommand.GeneratedCode)
                },
                () =>
                {
                    if( !this.IsCodeEditable )
                        this.UpdateGeneratedCode(); // update when parameters changed, and not currently editable
                });
            this.propertyChange.Register(this, nameof(this.ScriptCommand), () => this.RaisePropertyChanged(nameof(this.IsCodeEditable)));

            this.scriptEditorVM = new ScriptEditorViewModel();
            this.RunCodeCommand = new DelegateCommand(async () =>
            {
                await this.ScriptEditorViewModel.RunCodeAsync();
            },
            canExecute: () => !this.ScriptEditorViewModel.IsRunningScript);
            this.propertyChange.Register(this.ScriptEditorViewModel, nameof(this.ScriptEditorViewModel.IsRunningScript), () => this.RunCodeCommand.RaiseCanExecuteChanged());
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

                if( this.propertyChange.NotNullReference() )
                {
                    this.propertyChange.Dispose();
                    this.propertyChange = null;
                }

                if( this.scriptEditorVM.NotNullReference() )
                {
                    this.scriptEditorVM.Dispose();
                    this.scriptEditorVM = null;
                }
            }

            //// shared cleanup logic
            //// (unmanaged resources)


            base.OnDispose(disposing);
        }

        #endregion

        #region Private Methods

        private void UpdateGeneratedCode()
        {
            this.ScriptEditorViewModel.Code = this.ScriptCommand?.GeneratedCode;
        }

        #endregion

        #region Public Members

        /// <summary>
        /// Gets or sets the script command to display in the GUI.
        /// </summary>
        /// <value>The script command to display in the GUI.</value>
        public ScriptCommandBase ScriptCommand
        {
            get
            {
                this.ThrowIfDisposed();

                return this.scriptCommand;
            }
            set
            {
                this.ThrowIfDisposed();

                if( !object.ReferenceEquals(this.scriptCommand, value) )
                {
                    this.scriptCommand = value;
                    this.RaisePropertyChanged();

                    if( value.NotNullReference() )
                        this.IsCodeEditable = false;
                }
            }
        }

        /// <summary>
        /// Gets or sets the script code currently displayed.
        /// </summary>
        /// <value>The script code currently displayed.</value>
        public ScriptEditorViewModel ScriptEditorViewModel
        {
            get
            {
                this.ThrowIfDisposed();

                return this.scriptEditorVM;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the script displayed is generated based on it's parameters, or manually edited.
        /// </summary>
        /// <value>Indicates whether the script displayed is generated based on it's parameters, or manually edited.</value>
        public bool IsCodeEditable
        {
            get
            {
                this.ThrowIfDisposed();

                if( this.ScriptCommand.NotNullReference() )
                    return this.isEditable;
                else
                    return true; // always editable if no command is selected
            }
            set
            {
                this.ThrowIfDisposed();

                if( this.ScriptCommand.NotNullReference() )
                {
                    if( this.isEditable != value )
                    {
                        this.isEditable = value;
                        this.RaisePropertyChanged();
                    }
                }
                else
                {
                    if( !value )
                        this.RaisePropertyChanged(); // notify listeners that we didn't change our value
                }
            }
        }

        /// <summary>
        /// Gets a command that executes the current script.
        /// </summary>
        /// <value>A command that executes the current script.</value>
        public DelegateCommand RunCodeCommand { get; }

        #endregion
    }
}
