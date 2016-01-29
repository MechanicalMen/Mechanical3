using System.Collections.Generic;

namespace Mechanical3.DataStores
{
    //// NOTE: For performance critical scenarios, you can either cache converters from the locator,
    ////       or you could try switching to a binary file format.

    /// <summary>
    /// Implements a service locator pattern for <see cref="IStringConverter{T}"/> instances.
    /// </summary>
    public interface IStringConverterLocator
    {
        /// <summary>
        /// Gets an <see cref="IStringConverter{T}"/>
        /// </summary>
        /// <typeparam name="T">The type of objects to convert.</typeparam>
        /// <returns>The string converter found.</returns>
        /// <exception cref="KeyNotFoundException">A converter could not be found for the specified type.</exception>
        IStringConverter<T> GetConverter<T>();
    }
}
