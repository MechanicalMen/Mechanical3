using System;
using System.Collections.Generic;
using System.Globalization;
using Mechanical3.Collections;
using Mechanical3.Core;

namespace Mechanical3.Misc
{
    /// <summary>
    /// Represents a collection of <see cref="StringState"/> instances.
    /// </summary>
    public class StringStateCollection : EnumerableBase<StringState>
    {
        #region Private Fiekds

        private const string PartialStackTrace = "PartialStackTrace";
        private static readonly FileLineInfo[] EmptyPartialStackTrace = new FileLineInfo[0];

        private readonly List<StringState> states;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="StringStateCollection"/> class.
        /// </summary>
        public StringStateCollection()
        {
            this.states = new List<StringState>();
        }

        #endregion

        #region Private Methods

        private int IndexOf( string name )
        {
            for( int i = 0; i < this.states.Count; ++i )
            {
                if( string.Equals(this.states[i].Name, name, StringComparison.Ordinal) )
                    return i;
            }

            return -1;
        }

        #endregion

        #region EnumerableBase

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>An <see cref="IEnumerator{T}"/> that can be used to iterate through the collection.</returns>
        public override IEnumerator<StringState> GetEnumerator()
        {
            return this.states.GetEnumerator();
        }

        #endregion

        #region Public Members

        /// <summary>
        /// Gets the number of items in the collection.
        /// </summary>
        /// <value>The number of items in the collection.</value>
        public int Count
        {
            get { return this.states.Count; }
        }

        /// <summary>
        /// Determines whether a <see cref="StringState"/> instance with the specified name already exists in the collection.
        /// </summary>
        /// <param name="name">The string to search for.</param>
        /// <returns><c>true</c> if the name is in use in this collection; otherwise, <c>false</c>.</returns>
        public bool ContainsKey( string name )
        {
            return this.IndexOf(name) != -1;
        }

        /// <summary>
        /// Adds a new <see cref="StringState"/> instance to this collection.
        /// May change the name slightly, if there is a key conflict.
        /// </summary>
        /// <param name="state">The object to add to this collection.</param>
        public void Add( StringState state )
        {
            if( state.Value.NullReference() )
                throw new ArgumentNullException(nameof(state)); // created by default struct constructor

            // we do not allow overwriting of earlier data
            string name = state.Name;
            int i = 2;
            while( this.ContainsKey(name) )
            {
                name = state.Name + i.ToString("D", CultureInfo.InvariantCulture);
                ++i;
            }

            this.states.Add(new StringState(name, state.Value, state.ValueType));
        }

        /// <summary>
        /// Gets a value indicating whether at least one line of partial stack trace has been added.
        /// </summary>
        /// <value>Indicates whether at least one line of partial stack trace has been added.</value>
        public bool HasPartialStackTrace
        {
            get { return this.ContainsKey(PartialStackTrace); }
        }

        /// <summary>
        /// Appends the specified <see cref="FileLineInfo"/> instance to the partial stack trace
        /// </summary>
        /// <param name="sourcePos">The source position to append to the partial stack trace.</param>
        public void AddPartialStackTrace( FileLineInfo sourcePos )
        {
            // get collection
            StringState state;
            FileLineInfoCollection collection;
            int index = this.IndexOf(PartialStackTrace);
            if( index != -1 )
            {
                state = this.states[index];
                collection = FileLineInfoCollection.Parse(state.Value);
            }
            else
            {
                collection = new FileLineInfoCollection();
            }

            // add new item
            collection.Add(sourcePos);

            // update partial stack trace
            state = new StringState(
                    name: PartialStackTrace,
                    value: collection.ToString(),
                    valueType: SafeString.DebugPrint(typeof(FileLineInfoCollection)));

            if( index != -1 )
                this.states[index] = state;
            else
                this.states.Add(state);
        }

        /// <summary>
        /// Gets the (accumulated) partial stack trace.
        /// </summary>
        /// <returns>The (accumulated) partial stack trace found.</returns>
        public FileLineInfoCollection GetPartialStackTrace()
        {
            int index = this.IndexOf(PartialStackTrace);
            if( index == -1 )
                return null;

            var state = this.states[index];
            return FileLineInfoCollection.Parse(state.Value);
        }

        #endregion
    }
}
