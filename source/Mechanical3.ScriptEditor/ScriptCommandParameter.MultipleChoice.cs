using System;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using Mechanical3.Core;

namespace Mechanical3.ScriptEditor
{
    public static partial class ScriptCommandParameter
    {
        /// <summary>
        /// A parameter that selects zero or more items from multiple options.
        /// </summary>
        public class MultipleChoice : Base
        {
            #region SelectedIndexCollection

            /// <summary>
            /// A collection of the indexes representing the currently selected options.
            /// </summary>
            public class SelectedIndexCollection : ObservableCollection<int>
            {
                private readonly MultipleChoice parent;

                internal SelectedIndexCollection( MultipleChoice mc )
                {
                    if( mc.NullReference() )
                        throw new ArgumentNullException(nameof(mc)).StoreFileLine();

                    this.parent = mc;
                }

                /// <summary>
                /// Raises the CollectionChanged event with the provided arguments.
                /// </summary>
                /// <param name="e">Arguments of the event being raised.</param>
                protected override void OnCollectionChanged( NotifyCollectionChangedEventArgs e )
                {
                    base.OnCollectionChanged(e);

                    this.parent.RaisePropertyChanged(nameof(this.parent.SelectedIndexes));
                }

                /// <summary>
                /// Inserts an item into the collection at the specified index.
                /// </summary>
                /// <param name="index">The zero-based index at which item should be inserted.</param>
                /// <param name="item">The object to insert.</param>
                protected override void InsertItem( int index, int item )
                {
                    if( item < 0
                     || item >= this.parent.Items.Length )
                        throw new ArgumentOutOfRangeException(nameof(item)).StoreFileLine();

                    base.InsertItem(index, item);
                }

                /// <summary>
                /// Replaces the element at the specified index.
                /// </summary>
                /// <param name="index">The zero-based index of the element to replace.</param>
                /// <param name="item">The new value for the element at the specified index.</param>
                protected override void SetItem( int index, int item )
                {
                    if( item < 0
                     || item >= this.parent.Items.Length )
                        throw new ArgumentOutOfRangeException(nameof(item)).StoreFileLine();

                    base.SetItem(index, item);
                }
            }

            #endregion

            /// <summary>
            /// Initializes a new instance of the <see cref="ScriptCommandParameter.MultipleChoice"/> class.
            /// </summary>
            /// <param name="displayName">The name of the parameter, displayed in the GUI.</param>
            /// <param name="items">The options to choose from.</param>
            public MultipleChoice( string displayName, params string[] items )
                : base(displayName)
            {
                if( items.NullEmptyOrSparse() )
                    throw NamedArgumentException.From(nameof(items)).StoreFileLine();

                this.Items = items.ToImmutableArray();
                this.SelectedIndexes = new SelectedIndexCollection(this);
            }

            /// <summary>
            /// Gets the options to choose from.
            /// </summary>
            /// <value>The options to choose from.</value>
            public ImmutableArray<string> Items { get; }

            /// <summary>
            /// Gets the indexes of the currently selected options.
            /// </summary>
            /// <value>The indexes of the currently selected options</value>
			public SelectedIndexCollection SelectedIndexes { get; }

            /// <summary>
            /// Selects a single option.
            /// </summary>
            /// <value>The index of the single selected option.</value>
            public int SelectedIndex
            {
                set
                {
                    if( value < 0
                     || value >= this.Items.Length )
                        throw new ArgumentOutOfRangeException().Store(nameof(value), value).Store("numItems", this.Items.Length);

                    // remove other selections
                    foreach( var selectedIndex in this.SelectedIndexes.ToArray() )
                    {
                        if( selectedIndex != value )
                            this.SelectedIndexes.Remove(selectedIndex);
                    }

                    // add specified selection
                    if( !this.SelectedIndexes.Contains(value) )
                        this.SelectedIndexes.Add(value);
                }
            }

            /// <summary>
            /// Selects a single option.
            /// </summary>
            /// <value>The single selected option.</value>
            public string SelectedItem
            {
                set { this.SelectedIndex = this.Items.IndexOf(value, startIndex: 0, equalityComparer: StringComparer.Ordinal); }
            }

            /// <summary>
            /// Gets the currently selected options separated by commas.
            /// </summary>
            /// <returns>The currently selected options separated by commas.</returns>
            public string CommaSeparatedSelectedItems
            {
                get { return string.Join(", ", this.SelectedIndexes.Select(index => this.Items[index]).ToArray()); }
            }
        }
    }
}
