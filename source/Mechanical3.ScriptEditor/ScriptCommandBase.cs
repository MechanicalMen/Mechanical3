using System;
using System.Collections.Immutable;
using Mechanical3.Core;
using Mechanical3.MVVM;

namespace Mechanical3.ScriptEditor
{
    /// <summary>
    /// A GUI configurable script.
    /// </summary>
    public abstract class ScriptCommandBase : PropertyChangedBase
    {
        #region Private Fields

        private readonly ScriptCommandParameter.Base[] parameters;
        private string code;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="ScriptCommandBase"/> class.
        /// </summary>
        /// <param name="displayName">The string identifying this script command in the GUI.</param>
        /// <param name="parameters">The optional GUI parameters of the script.</param>
        protected ScriptCommandBase( string displayName, params ScriptCommandParameter.Base[] parameters )
        {
            if( displayName.NullOrWhiteSpace() )
                throw new ArgumentException().Store(nameof(displayName), displayName);

            this.DisplayName = displayName;

            this.parameters = parameters ?? new ScriptCommandParameter.Base[0];
            if( this.parameters.Length != 0 )
            {
                foreach( var p in this.parameters )
                {
                    if( p.NullReference() )
                        throw new ArgumentNullException().StoreFileLine();

                    p.PropertyChanged += this.OnParameterPropertyChanged;
                }
            }
        }

        #endregion

        #region Private Methods

        private void OnParameterPropertyChanged( object sender, System.ComponentModel.PropertyChangedEventArgs e )
        {
            this.GeneratedCode = this.GenerateScript();
        }

        #endregion

        #region Protected Members

        /// <summary>
        /// Generates the script based on the current state of the <see cref="Parameters"/>.
        /// </summary>
        /// <returns>The script generated.</returns>
        protected abstract string GenerateScript();

        #endregion

        #region Public Members

        /// <summary>
        /// Gets the string identifying this script command in the GUI.
        /// </summary>
        /// <value>The string identifying this script command in the GUI.</value>
        public string DisplayName { get; }

        /// <summary>
        /// Gets the optional GUI parameters of the script.
        /// </summary>
        /// <value>The optional GUI parameters of the script.</value>
        public ImmutableArray<ScriptCommandParameter.Base> Parameters
        {
            get { return this.parameters.ToImmutableArray(); }
        }

        /// <summary>
        /// Gets the current code generated for the command, based on it's parameters.
        /// </summary>
        /// <value>The current code generated for the command.</value>
        public string GeneratedCode
        {
            get
            {
                if( this.code.NullReference() )
                    this.OnParameterPropertyChanged(null, null); // code == null, initially

                return this.code;
            }
            private set
            {
                if( !string.Equals(this.code, value, StringComparison.Ordinal) )
                {
                    this.code = value;
                    this.RaisePropertyChanged();
                }
            }
        }

        #endregion
    }
}
