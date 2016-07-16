using System;

namespace Mechanical3.Core
{
    /// <summary>
    /// Helps create an <see cref="ArgumentException"/> similarly to <see cref="ArgumentNullException"/>.
    /// </summary>
    public static class NamedArgumentException
    {
        //// NOTE: Unfortunately it is not currently possible to add a new constructor using extension methods.
        ////       It would be possible to inherit, but I think the exception type should indicate
        ////       the general type of error, not the method of it's construction.

        /// <summary>
        /// Creates an <see cref="ArgumentException"/> using the specified parameter name.
        /// </summary>
        /// <param name="paramName">The name of the parameter that caused the exception.</param>
        /// <param name="innerException">The optional inner exception to add.</param>
        /// <returns>A new <see cref="ArgumentException"/> instance.</returns>
        public static ArgumentException FromParameter( string paramName, Exception innerException = null )
        {
            return new ArgumentException(
                message: $"Invalid parameter: \"{paramName}\"!",
                innerException: innerException);
        }
    }
}
