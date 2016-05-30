using System;
using Mechanical3.Core;
using Mechanical3.DataStores;

namespace Mechanical3.Misc
{
    /// <summary>
    /// Records the string representation of a value.
    /// </summary>
    public struct StringState
    {
        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="StringState"/> struct.
        /// </summary>
        /// <param name="name">The name of the value.</param>
        /// <param name="value">The string representation recorded.</param>
        /// <param name="valueType">The original type of the recorded value.</param>
        public StringState( string name, string value, string valueType )
        {
            if( name.NullOrLengthy() )
                throw new ArgumentException("Invalid name!");

            //// value may be null

            if( valueType.NullOrLengthy() )
                throw new ArgumentException("Invalid type string!");

            this.Name = name;
            this.Value = value;
            this.ValueType = valueType;
        }

        /// <summary>
        /// Records a named value.
        /// </summary>
        /// <typeparam name="T">The type of value to record.</typeparam>
        /// <param name="name">The name of the value.</param>
        /// <param name="value">The value to record the string representation of.</param>
        /// <returns>An object representing the recorded value.</returns>
        public static StringState From<T>( string name, T value )
        {
            return new StringState(
                name,
                value.NotNullReference() ? SafeString.DebugPrint(value) : null,
                value.NotNullReference() ? SafeString.DebugPrint(value.GetType()) : SafeString.DebugPrint(typeof(T)));
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets the name of the value;
        /// </summary>
        /// <value>The name of the value.</value>
        public string Name { get; }

        /// <summary>
        /// Gets the string representation recorded.
        /// </summary>
        /// <value>The string representation recorded.</value>
        public string Value { get; }

        /// <summary>
        /// Gets the original type of the recorded value.
        /// </summary>
        /// <value>The original type of the recorded value.</value>
        public string ValueType { get; }

        /// <summary>
        /// Gets the printable format of the value.
        /// It will never return <c>null</c>, and will surround string values in double quotes.
        /// </summary>
        /// <value>The printable format of the value.</value>
        public string DisplayValue
        {
            get
            {
                if( this.Value.NullReference() )
                {
                    return "null";
                }
                else
                {
                    if( string.Equals(this.ValueType, "string", StringComparison.Ordinal)
                     || string.Equals(this.ValueType, "String", StringComparison.Ordinal)
                     || string.Equals(this.ValueType, "System.String", StringComparison.Ordinal) )
                        return '"' + this.Value + '"';
                    else
                        return this.Value;
                }
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Gets the string representation of this instance.
        /// </summary>
        /// <returns>The string representation of this instance.</returns>
        public override string ToString()
        {
            return $"{this.Name}={this.DisplayValue}";
        }

        #endregion

        #region Serialization

        private static class Keys
        {
            internal const string Name = "Name";
            internal const string Value = "Value";
            internal const string ValueType = "ValueType";
        }

        /// <summary>
        /// Saves the specified instance.
        /// </summary>
        /// <param name="writer">The <see cref="DataStoreTextWriter"/> to use.</param>
        public void Save( DataStoreTextWriter writer )
        {
            if( writer.NullReference() )
                throw new ArgumentNullException(nameof(writer)).StoreFileLine();

            if( this.Name.NullReference() )
                throw new ArgumentException().StoreFileLine(); // default ctor. creates invalid instances

            writer.WriteObjectStart();
            writer.WriteValue(Keys.Name, this.Name);
            writer.WriteValue(Keys.Value, this.Value);
            writer.WriteValue(Keys.ValueType, this.ValueType);
            writer.WriteEnd();
        }

        /// <summary>
        /// Loads the specified instance.
        /// </summary>
        /// <param name="reader">The <see cref="DataStoreTextReader"/> to use.</param>
        /// <returns>The instance loaded. May be <c>null</c>.</returns>
        public static StringState LoadFrom( DataStoreTextReader reader )
        {
            if( reader.NullReference() )
                throw new ArgumentNullException(nameof(reader)).StoreFileLine();

            reader.AssertObjectStart();
            var name = reader.ReadValue<string>(Keys.Name);
            var value = reader.ReadValue<string>(Keys.Value);
            var valueType = reader.ReadValue<string>(Keys.ValueType);
            reader.ReadEnd();

            return new StringState(name, value, valueType);
        }

        #endregion
    }
}
