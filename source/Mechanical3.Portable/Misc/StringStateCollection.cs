using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using Mechanical3.Collections;
using Mechanical3.Core;

namespace Mechanical3.Misc
{
    /* NOTE: - Exception.Data requires that both keys and values are serializable.
     *       - The built-in types are serializable
     *       - SerializableAttribute and ISerializable are not available in portable libraries
     *       - DataContract is, but appearantly it's not good enough
     *
     * Options:
     *       - Wrap every single exception in a "StringStateCollectionException":
     *         this will complicate the exception hierarchy really fast
     *       - Serialize and deserialize the collection into a string each time it's accessed:
     *         apart from the performance, this requires some kind of manual serialization code
     *         (but since this is a low level class, it should only depend on the .NET Framework)
     *       - Use the IDictionary of Exception.Data directly:
     *         internal state will be accessible to everyone
     */

    //// NOTE: This type was not designed for performance. Things could be improved by
    ////       switching from a wrapper to static methods, but for now there is no need to optimize.

    /// <summary>
    /// Represents a collection of <see cref="StringState"/> instances.
    /// </summary>
    public class StringStateCollection : EnumerableBase<StringState>
    {
        #region Private Fields

        private const string StateValuePostfix = "_Value";
        private const string StateValueTypePostfix = "_ValueType";

        private const string PartialStackTrace = "PartialStackTrace";
        private static readonly FileLineInfo[] EmptyPartialStackTrace = new FileLineInfo[0];

        private readonly IDictionary states;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="StringStateCollection"/> class.
        /// </summary>
        /// <param name="dictionary">The underlying collection to use; or <c>null</c> to use an empty one.</param>
        public StringStateCollection( IDictionary dictionary = null )
        {
            this.states = dictionary.NotNullReference() ? dictionary : new Dictionary<object, object>();
        }

        #endregion

        #region Private Methods

        private IEnumerable<KeyValuePair<string, string>> GetPairs()
        {
            string key, value;
            foreach( DictionaryEntry entry in this.states )
            {
                key = entry.Key as string;
                if( key.NotNullReference() )
                {
                    value = entry.Value as string;
                    if( value.NotNullReference() )
                        yield return new KeyValuePair<string, string>(key, value);
                }
            }
        }

        private bool TryGetValue( string name, out string value )
        {
            foreach( var pair in this.GetPairs() )
            {
                if( string.Equals(name, pair.Key, StringComparison.Ordinal) )
                {
                    value = pair.Value;
                    return true;
                }
            }

            value = null;
            return false;
        }

        private bool TryGetState( string name, out StringState state )
        {
            string value;
            string valueType;
            if( this.TryGetValue(name + StateValuePostfix, out value)
             && this.TryGetValue(name + StateValueTypePostfix, out valueType) )
            {
                state = new StringState(name, value, valueType);
                return true;
            }

            state = default(StringState);
            return false;
        }

        private void AddOrSet( StringState state )
        {
            this.states[state.Name + StateValuePostfix] = state.Value;
            this.states[state.Name + StateValueTypePostfix] = state.ValueType;
        }

        #endregion

        #region EnumerableBase

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>An <see cref="IEnumerator{T}"/> that can be used to iterate through the collection.</returns>
        public override IEnumerator<StringState> GetEnumerator()
        {
            StringState state;
            foreach( var pair in this.GetPairs() )
            {
                if( pair.Key.NotNullReference()
                 && pair.Key.EndsWith(StateValuePostfix, StringComparison.Ordinal)
                 && this.TryGetState(pair.Key.Substring(0, pair.Key.Length - StateValuePostfix.Length), out state) )
                    yield return state;
            }
        }

        #endregion

        #region Public Members

        /// <summary>
        /// Determines whether a <see cref="StringState"/> instance with the specified name already exists in the collection.
        /// </summary>
        /// <param name="name">The string to search for.</param>
        /// <returns><c>true</c> if the name is in use in this collection; otherwise, <c>false</c>.</returns>
        public bool ContainsKey( string name )
        {
            string value;
            return this.TryGetValue(name + StateValuePostfix, out value);
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

            state = new StringState(name, state.Value, state.ValueType);
            this.AddOrSet(state);
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
        /// Gets the (accumulated) partial stack trace.
        /// </summary>
        /// <returns>The (accumulated) partial stack trace found.</returns>
        public FileLineInfoCollection GetPartialStackTrace()
        {
            StringState state;
            if( this.TryGetState(PartialStackTrace, out state) )
                return FileLineInfoCollection.Parse(state.Value);
            else
                return null;
        }

        /// <summary>
        /// Appends the specified <see cref="FileLineInfo"/> instance to the partial stack trace
        /// </summary>
        /// <param name="sourcePos">The source position to append to the partial stack trace.</param>
        public void AddPartialStackTrace( FileLineInfo sourcePos )
        {
            // get collection
            var collection = this.GetPartialStackTrace() ?? new FileLineInfoCollection();

            // add new item
            collection.Add(sourcePos);

            // update partial stack trace
            var state = new StringState(
                    name: PartialStackTrace,
                    value: collection.ToString(),
                    valueType: SafeString.DebugPrint(typeof(FileLineInfoCollection)));
            this.AddOrSet(state);
        }

        #endregion
    }
}
