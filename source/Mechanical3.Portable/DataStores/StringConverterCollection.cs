using System;
using System.Collections.Generic;
using Mechanical3.Core;

namespace Mechanical3.DataStores
{
    /// <summary>
    /// Implements <see cref="IStringConverterLocator"/> using a collection of converters.
    /// Returns exact matches. Not thread-safe.
    /// </summary>
    public class StringConverterCollection : IStringConverterLocator
    {
        #region Private Fields

        private readonly Dictionary<Type, object> converters;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="StringConverterCollection"/> class.
        /// </summary>
        public StringConverterCollection()
        {
            this.converters = new Dictionary<Type, object>();
        }

        #endregion

        #region IStringConverterLocator

        /// <summary>
        /// Gets an <see cref="IStringConverter{T}"/>
        /// </summary>
        /// <typeparam name="T">The type of objects to convert.</typeparam>
        /// <returns>The string converter found.</returns>
        /// <exception cref="KeyNotFoundException">A converter could not be found for the specified type.</exception>
        public IStringConverter<T> GetConverter<T>()
        {
            object result;
            if( this.converters.TryGetValue(typeof(T), out result) )
                return (IStringConverter<T>)result;
            else
                throw new KeyNotFoundException("Could not locate a converter for the specified type!").Store("type", typeof(T));
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Adds the specified converter to the collection.
        /// </summary>
        /// <typeparam name="T">The types to convert.</typeparam>
        /// <param name="converter">The <see cref="IStringConverter{T}"/> to add to the collection.</param>
        public void Add<T>( IStringConverter<T> converter )
        {
            if( converter.NullReference() )
                throw new ArgumentNullException(nameof(converter)).StoreFileLine();

            this.converters.Add(typeof(T), converter); // throws if type is already registered.
        }

        #endregion
    }
}
