using System;
using Mechanical3.Core;

namespace Mechanical3.Misc
{
    //// NOTE: we want to avoid using exception extensions here

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

            if( value.NullReference() )
                throw new ArgumentNullException(nameof(value));

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
            //// NOTE: Optional postfix on value type allows us to differentiate
            ////       between an object that is actually a null reference,
            ////       and an object that simply prints "null".

            return new StringState(
                name,
                SafeString.DebugPrint(value),
                value.NotNullReference() ? SafeString.DebugPrint(value.GetType()) : (SafeString.DebugPrint(typeof(T)) + " (null)"));
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

        #endregion

        #region Public Methods

        /// <summary>
        /// Gets the string representation of this instance.
        /// </summary>
        /// <returns>The string representation of this instance.</returns>
        public override string ToString()
        {
            return $"{this.Name}={this.Value}";
        }

        #endregion
    }
}
