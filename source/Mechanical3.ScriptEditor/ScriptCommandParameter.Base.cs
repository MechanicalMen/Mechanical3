using System;
using Mechanical3.Core;
using Mechanical3.MVVM;

namespace Mechanical3.ScriptEditor
{
    /// <summary>
    /// Encapsulates script command parameter view models.
    /// </summary>
    public static partial class ScriptCommandParameter
    {
        /// <summary>
        /// The base class of script commands.
        /// </summary>
        public abstract class Base : PropertyChangedBase
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="ScriptCommandParameter.Base"/> class.
            /// </summary>
            /// <param name="displayName">The name of the parameter, displayed in the GUI.</param>
            protected Base( string displayName )
            {
                if( displayName.NullOrWhiteSpace() )
                    throw new ArgumentException().Store(nameof(displayName), displayName);

                this.DisplayName = displayName;
            }

            /// <summary>
            /// Gets the name of the parameter, displayed in the GUI.
            /// </summary>
            /// <value>The name of the parameter, displayed in the GUI.</value>
            public string DisplayName { get; }
        }
    }
}
