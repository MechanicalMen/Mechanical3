using System.Runtime.CompilerServices;

namespace Mechanical3.Core
{
    /// <content>
    /// Methods extending the <see cref="object"/> type.
    /// </content>
    public static partial class CoreExtensions
    {
        //// NOTE: we don't want to "litter" intellisense with rarely used extension methods (especially on System.Object!), so think twice, before adding something here

        #region object

        /// <summary>
        /// Determines whether the object is <c>null</c>.
        /// </summary>
        /// <param name="value">The object to check.</param>
        /// <returns><c>true</c> if the specified object is <c>null</c>; otherwise, <c>false</c>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool NullReference( this object value )
        {
            return object.ReferenceEquals(value, null);
        }

        /// <summary>
        /// Determines whether the object is not <c>null</c>.
        /// </summary>
        /// <param name="value">The object to check.</param>
        /// <returns><c>true</c> if the specified object is not <c>null</c>; otherwise, <c>false</c>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool NotNullReference( this object value )
        {
            return !object.ReferenceEquals(value, null);
        }

        #endregion

        #region string

        /// <summary>
        /// Determines whether the string is <c>null</c> or empty.
        /// </summary>
        /// <param name="str">The string to check.</param>
        /// <returns><c>true</c> if the specified string is <c>null</c> or empty; otherwise, <c>false</c>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool NullOrEmpty( this string str )
        {
            return string.IsNullOrEmpty(str);
        }

        /// <summary>
        /// Determines whether the string is <c>null</c>, empty, or if it has leading or trailing white-space characters.
        /// </summary>
        /// <param name="str">The string to check.</param>
        /// <returns><c>true</c> if the specified string is <c>null</c>, empty or lengthy; otherwise, <c>false</c>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool NullOrLengthy( this string str )
        {
            return string.IsNullOrEmpty(str)
                || char.IsWhiteSpace(str, 0)
                || char.IsWhiteSpace(str, str.Length - 1);
        }

        /// <summary>
        /// Determines whether the string is <c>null</c>, empty, or if it consists only of white-space characters.
        /// </summary>
        /// <param name="str">The string to check.</param>
        /// <returns><c>true</c> if the specified string is <c>null</c>, empty, or whitespace; otherwise, <c>false</c>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool NullOrWhiteSpace( this string str )
        {
            return string.IsNullOrWhiteSpace(str);
        }

        #endregion
    }
}
