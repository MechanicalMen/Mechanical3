using System;
using System.Threading.Tasks;
using System.Windows.Input;
using Mechanical3.Core;

namespace Mechanical3.MVVM
{
    /// <summary>
    /// An <see cref="ICommand"/> that is implemented through delegates.
    /// </summary>
    public class DelegateCommand : ICommand
    {
        #region Private Fields

        private readonly Action<object> execute;
        private readonly Func<object, bool> canExecute;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="DelegateCommand"/> class.
        /// </summary>
        /// <param name="execute">The delegate invoked by Execute.</param>
        /// <param name="canExecute">The delegate invoked by CanExecute.</param>
        public DelegateCommand( Action execute, Func<bool> canExecute = null )
            : this(
                execute.NullReference() ? (Action<object>)null : new Action<object>(param => execute()),
                canExecute.NullReference() ? (Func<object, bool>)null : new Func<object, bool>(param => canExecute()))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DelegateCommand"/> class.
        /// </summary>
        /// <param name="execute">The delegate invoked by Execute.</param>
        /// <param name="canExecute">The delegate invoked by CanExecute.</param>
        public DelegateCommand( Action<object> execute, Func<object, bool> canExecute = null )
        {
            if( execute.NullReference() )
                throw new ArgumentNullException(nameof(execute)).StoreFileLine();

            this.execute = execute;
            this.canExecute = canExecute;
        }

        #endregion

        #region ICommand

        /// <summary>
        /// Occurs when changes occur that affect whether or not the command should execute.
        /// </summary>
        public event EventHandler CanExecuteChanged;

        /// <summary>
        /// Determines whether the command can execute in its current state.
        /// </summary>
        /// <param name="parameter">Data used by the command. If the command does not require data to be passed, this object can be set to <c>null</c>.</param>
        /// <returns><c>true</c> if this command can be executed; otherwise, <c>false</c>.</returns>
        public bool CanExecute( object parameter )
        {
            if( this.canExecute.NullReference() )
                return true;
            else
                return this.canExecute(parameter);
        }

        /// <summary>
        /// The method called when the command is invoked.
        /// </summary>
        /// <param name="parameter">Data used by the command. If the command does not require data to be passed, this object can be set to <c>null</c>.</param>
        public void Execute( object parameter )
        {
            this.execute(parameter);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Raises the <see cref="CanExecuteChanged"/> event synchronously on the UI thread.
        /// </summary>
        public void RaiseCanExecuteChanged()
        {
            UI.Invoke(() =>
            {
                var handlers = this.CanExecuteChanged;
                if( handlers.NotNullReference() )
                    handlers(this, EventArgs.Empty);
            });
        }

        /// <summary>
        /// Raises the <see cref="CanExecuteChanged"/> event asynchronously on the UI thread.
        /// </summary>
        /// <returns>The <see cref="Task"/> representing the operation.</returns>
        public Task RaiseCanExecuteChangedAsync()
        {
            return UI.InvokeAsync(() =>
            {
                var handlers = this.CanExecuteChanged;
                if( handlers.NotNullReference() )
                    handlers(this, EventArgs.Empty);
            });
        }

        #endregion
    }
}
