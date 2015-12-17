using System;
using System.Runtime.CompilerServices;
using Mechanical3.Misc;

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

        #region Exception

        private static StringStateCollection GetStateCollection( Exception exception )
        {
            if( exception.NullReference() )
                throw new ArgumentNullException(nameof(exception));

            if( !exception.Data.Contains(nameof(StringStateCollection)) )
                exception.Data.Add(nameof(StringStateCollection), new StringStateCollection());

            return (StringStateCollection)exception.Data[nameof(StringStateCollection)];
        }

        /// <summary>
        /// Gets the data stored in the exception.
        /// </summary>
        /// <param name="exception">The exception data was stored in.</param>
        /// <returns>The <see cref="StringStateCollection"/> instance in use by the exception, if there is one.</returns>
        public static StringStateCollection GetStoredData( this Exception exception )
        {
            if( exception.NullReference() )
                throw new ArgumentNullException(nameof(exception));

            if( exception.Data.Contains(nameof(StringStateCollection)) )
                return (StringStateCollection)exception.Data[nameof(StringStateCollection)];
            else
                return null;
        }

        private static TException AddState<TException>( TException e, StringState state )
            where TException : Exception
        {
            var collection = GetStateCollection(e);
            collection.Add(state);
            return e;
        }

        /// <summary>
        /// Stores the specified source file position.
        /// </summary>
        /// <typeparam name="TException">The type of the exception.</typeparam>
        /// <param name="e">The exception to store data in.</param>
        /// <param name="sourcePos">The source file position to add.</param>
        /// <returns>The exception the data was stored in.</returns>
        public static TException StoreFileLine<TException>( this TException e, FileLineInfo sourcePos )
            where TException : Exception
        {
            var collection = GetStateCollection(e);
            collection.AddPartialStackTrace(sourcePos);
            return e;
        }

        /// <summary>
        /// Stores the current source file position.
        /// </summary>
        /// <typeparam name="TException">The type of the exception.</typeparam>
        /// <param name="e">The exception to store data in.</param>
        /// <param name="file">The source file that contains the caller.</param>
        /// <param name="member">The method or property name of the caller to this method.</param>
        /// <param name="line">The line number in the source file at which this method is called.</param>
        /// <returns>The exception the data was stored in.</returns>
        public static TException StoreFileLine<TException>(
            this TException e,
            [CallerFilePath] string file = "",
            [CallerMemberName] string member = "",
            [CallerLineNumber] int line = 0 )
            where TException : Exception
        {
            return StoreFileLine(e, new FileLineInfo(file, member, line));
        }

        private static TException StoreFileLine_OnFirstCall<TException>( TException e, string file, string member, int line )
            where TException : Exception
        {
            var collection = GetStateCollection(e);
            if( collection.HasPartialStackTrace )
            {
                return StoreFileLine(e, file, member, line);
            }
            else
            {
                return e;
            }
        }

        /// <summary>
        /// Stores the specified data in the exception.
        /// </summary>
        /// <typeparam name="TException">The type of the exception.</typeparam>
        /// <param name="e">The exception to store data in.</param>
        /// <param name="state">The <see cref="StringState"/> instance to store.</param>
        /// <param name="file">The source file that contains the caller.</param>
        /// <param name="member">The method or property name of the caller to this method.</param>
        /// <param name="line">The line number in the source file at which this method is called.</param>
        /// <returns>The exception data was stored in.</returns>
        public static TException Store<TException>(
            this TException e,
            StringState state,
            [CallerFilePath] string file = "",
            [CallerMemberName] string member = "",
            [CallerLineNumber] int line = 0 )
            where TException : Exception
        {
            StoreFileLine_OnFirstCall(e, file, member, line);
            AddState(e, state);
            return e;
        }

        /// <summary>
        /// Stores the specified data in the exception.
        /// </summary>
        /// <typeparam name="TException">The type of the exception.</typeparam>
        /// <typeparam name="TValue">The type of value to record.</typeparam>
        /// <param name="e">The exception to store data in.</param>
        /// <param name="name">The name of the value.</param>
        /// <param name="value">The value to record the string representation of.</param>
        /// <param name="file">The source file that contains the caller.</param>
        /// <param name="member">The method or property name of the caller to this method.</param>
        /// <param name="line">The line number in the source file at which this method is called.</param>
        /// <returns>The exception data was stored in.</returns>
        public static TException Store<TException, TValue>(
            this TException e,
            string name,
            TValue value,
            [CallerFilePath] string file = "",
            [CallerMemberName] string member = "",
            [CallerLineNumber] int line = 0 )
            where TException : Exception
        {
            return Store(e, StringState.From(name, value), file, member, line);
        }

        #endregion
    }
}
