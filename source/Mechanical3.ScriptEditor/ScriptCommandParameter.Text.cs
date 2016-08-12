using System;

namespace Mechanical3.ScriptEditor
{
    public static partial class ScriptCommandParameter
    {
        /// <summary>
        /// A string parameter.
        /// </summary>
        public class Text : Base
        {
            private string text = null;
            private bool isMultiLine = false;

            /// <summary>
            /// Initializes a new instance of the <see cref="ScriptCommandParameter.Text"/> class.
            /// </summary>
            /// <param name="displayName">The name of the parameter, displayed in the GUI.</param>
            public Text( string displayName )
                : base(displayName)
            {
            }

            /// <summary>
            /// Gets or sets the current value of the parameter.
            /// </summary>
            /// <value>The current value of the parameter.</value>
			public string Value
            {
                get
                {
                    return this.text;
                }
                set
                {
                    if( !string.Equals(this.text, value, StringComparison.Ordinal) )
                    {
                        this.text = value;
                        this.RaisePropertyChanged();
                    }
                }
            }

            /// <summary>
            /// Gets or sets a value indicating whether the GUI should use a multi-line textbox.
            /// </summary>
            /// <value>Indicates whether the GUI should use a multi-line textbox.</value>
			public bool IsMultiLine
            {
                get
                {
                    return this.isMultiLine;
                }
                set
                {
                    if( this.isMultiLine != value )
                    {
                        this.isMultiLine = value;
                        this.RaisePropertyChanged();
                    }
                }
            }
        }
    }
}
