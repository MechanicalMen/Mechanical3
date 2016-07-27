using System;
using System.Runtime.CompilerServices;

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
        /// Does not use Store of StoreFileLine internally.
        /// </summary>
        /// <param name="paramName">The name of the parameter that caused the exception.</param>
        /// <param name="innerException">The optional inner exception to add.</param>
        /// <returns>A new <see cref="ArgumentException"/> instance.</returns>
        public static ArgumentException From( string paramName, Exception innerException = null )
        {
            return new ArgumentException(
                message: $"Invalid parameter: \"{paramName}\"!",
                innerException: innerException);
        }

        /// <summary>
        /// Creates an <see cref="ArgumentException"/> using the specified parameter name and value.
        /// </summary>
        /// <typeparam name="TValue">The type of value to record.</typeparam>
        /// <param name="paramName">The name of the parameter that caused the exception.</param>
        /// <param name="paramValue">The value to record the string representation of.</param>
        /// <param name="innerException">The optional inner exception to add.</param>
        /// <param name="file">The source file that contains the caller.</param>
        /// <param name="member">The method or property name of the caller to this method.</param>
        /// <param name="line">The line number in the source file at which this method is called.</param>
        /// <returns>A new <see cref="ArgumentException"/> instance.</returns>
        public static ArgumentException Store<TValue>(
            string paramName,
            TValue paramValue,
            Exception innerException = null,
            [CallerFilePath] string file = "",
            [CallerMemberName] string member = "",
            [CallerLineNumber] int line = 0 )
        {
            return From(paramName, innerException).Store(paramName, paramValue, file, member, line);
        }
    }
}
