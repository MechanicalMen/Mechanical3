using System;
using System.Collections.Immutable;
using Mechanical3.Core;

namespace Mechanical3.ScriptEditor
{
    public static partial class ScriptCommandParameter
    {
        /// <summary>
        /// A parameter that selects exactly one item from multiple options.
        /// </summary>
        public class SingleChoice : Base
        {
            private int selectedIndex;

            /// <summary>
            /// Initializes a new instance of the <see cref="ScriptCommandParameter.SingleChoice"/> class.
            /// </summary>
            /// <param name="displayName">The name of the parameter, displayed in the GUI.</param>
            /// <param name="items">The options to choose from.</param>
            public SingleChoice( string displayName, params string[] items )
                : base(displayName)
            {
                if( items.NullEmptyOrSparse() )
                    throw NamedArgumentException.From(nameof(items)).StoreFileLine();

                this.Items = items.ToImmutableArray();
                this.selectedIndex = 0;
            }

            /// <summary>
            /// Gets the options to choose from.
            /// </summary>
            /// <value>The options to choose from.</value>
            public ImmutableArray<string> Items { get; }

            /// <summary>
            /// Gets the index of the currently selected option.
            /// </summary>
            /// <value>The index of the currently selected option.</value>
			public int SelectedIndex
            {
                get
                {
                    return this.selectedIndex;
                }
                set
                {
                    if( value >= this.Items.Length
                     || value < 0 )
                        throw new ArgumentOutOfRangeException().Store(nameof(value), value);

                    if( this.selectedIndex != value )
                    {
                        this.selectedIndex = value;
                        this.RaisePropertyChanged();
                    }
                }
            }

            /// <summary>
            /// Gets the currently selected option.
            /// </summary>
            /// <value>The currently selected option.</value>
            public string SelectedItem
            {
                get { return this.Items[this.SelectedIndex]; }
            }
        }
    }
}
