namespace Mechanical3.ScriptEditor
{
    public static partial class ScriptCommandParameter
    {
        /// <summary>
        /// A <see cref="Boolean"/> parameter.
        /// </summary>
        public class Boolean : Base
        {
            private bool value = default(bool);

            /// <summary>
            /// Initializes a new instance of the <see cref="ScriptCommandParameter.Boolean"/> class.
            /// </summary>
            /// <param name="displayName">The name of the parameter, displayed in the GUI.</param>
            public Boolean( string displayName )
                : base(displayName)
            {
            }

            /// <summary>
            /// Gets or sets the current value of the parameter.
            /// </summary>
            /// <value>The current value of the parameter.</value>
			public bool Value
            {
                get
                {
                    return this.value;
                }
                set
                {
                    if( this.value != value )
                    {
                        this.value = value;
                        this.RaisePropertyChanged();
                    }
                }
            }

            /// <summary>
            /// Gets the C# literal representing the current <see cref="Value"/>.
            /// </summary>
            /// <value>The C# literal that represents the current <see cref="Value"/>.</value>
            public string AsCSharpConstant
            {
                get { return this.Value ? "true" : "false"; }
            }
        }
    }
}
