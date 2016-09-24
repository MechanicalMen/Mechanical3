using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Mechanical3.Misc;

namespace Mechanical3.Core
{
    /// <summary>
    /// Extension methods of the <see cref="Mechanical3.Core"/> namespace.
    /// </summary>
    public static class CoreExtensions
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

        #region Collections

        //// NOTE: we extend IEnumerable<T>, because overloading can easily lead to ambiguity.
        //// NOTE: arrays implement ICollection<T>, IList<T> as well.

        /// <summary>
        /// Determines whether the specified sequence is <c>null</c> or empty.
        /// </summary>
        /// <typeparam name="T">The type of the elelements of <paramref name="sequence"/>.</typeparam>
        /// <param name="sequence">A sequence to test.</param>
        /// <returns><c>true</c> if <paramref name="sequence"/> is <c>null</c> or empty; otherwise <c>false</c>.</returns>
        public static bool NullOrEmpty<T>( this IEnumerable<T> sequence )
        {
            if( sequence.NullReference() )
                return true;

            var collection = sequence as ICollection<T>;
            if( collection.NotNullReference() )
                return collection.Count == 0;

            var readOnlyCollection = sequence as IReadOnlyCollection<T>;
            if( readOnlyCollection.NotNullReference() )
                return readOnlyCollection.Count == 0;

            var enumerator = sequence.GetEnumerator();
            return !enumerator.MoveNext();
        }

        /// <summary>
        /// Determines whether the specified sequence is <c>null</c>, empty or whether it contains any <c>null</c> references.
        /// </summary>
        /// <typeparam name="T">The type of the elelements of <paramref name="sequence"/>.</typeparam>
        /// <param name="sequence">A sequence to test.</param>
        /// <returns><c>true</c> if <paramref name="sequence"/> is <c>null</c>, empty or contains at least one <c>null</c> reference; otherwise <c>false</c>.</returns>
        public static bool NullEmptyOrSparse<T>( this IEnumerable<T> sequence )
            where T : class
        {
            return sequence.NullOrEmpty()
                || sequence.Contains(item => item.NullReference());
        }

        /// <summary>
        /// Determines whether the specified sequence is <v>null</v> or whether it contains any <c>null</c> references.
        /// </summary>
        /// <typeparam name="T">The type of the elelements of <paramref name="sequence"/>.</typeparam>
        /// <param name="sequence">A sequence to test.</param>
        /// <returns><c>true</c> if <paramref name="sequence"/> is <c>null</c> or contains at least one <c>null</c> reference; otherwise <c>false</c>.</returns>
        public static bool NullOrSparse<T>( this IEnumerable<T> sequence )
            where T : class
        {
            return sequence.NullReference()
                || sequence.Contains(item => item.NullReference());
        }

        /// <summary>
        /// Determines whether a sequence contains a specified element by using the specified predicate.
        /// </summary>
        /// <typeparam name="T">The type of the elelements of <paramref name="sequence"/>.</typeparam>
        /// <param name="sequence">A sequence to search the elements of.</param>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <returns><c>true</c> if an element in the sequence passed the test in the specified predicate function; otherwise, <c>false</c>.</returns>
        public static bool Contains<T>( this IEnumerable<T> sequence, Func<T, bool> predicate )
        {
            if( sequence.NullReference() )
                throw new ArgumentNullException(nameof(sequence)).StoreFileLine();

            if( predicate.NullReference() )
                throw new ArgumentNullException(nameof(predicate)).StoreFileLine();

            var list = sequence as IList<T>;
            if( list.NotNullReference() )
            {
                if( list.Count == 0 )
                    return false;

                for( int i = 0; i < list.Count; ++i )
                {
                    if( predicate(list[i]) )
                        return true;
                }

                return false;
            }

            var readOnlyList = sequence as IReadOnlyList<T>;
            if( readOnlyList.NotNullReference() )
            {
                if( readOnlyList.Count == 0 )
                    return false;

                for( int i = 0; i < readOnlyList.Count; ++i )
                {
                    if( predicate(readOnlyList[i]) )
                        return true;
                }

                return false;
            }

            var enumerator = sequence.GetEnumerator();
            if( !enumerator.MoveNext() )
            {
                return false;
            }
            else
            {
                do
                {
                    if( predicate(enumerator.Current) )
                        return true;
                }
                while( enumerator.MoveNext() );

                return false;
            }
        }

        /// <summary>
        /// Returns the first element in a sequence, or <c>null</c> if the sequence contains no elements.
        /// </summary>
        /// <typeparam name="T">The type of the elelements of <paramref name="sequence"/>.</typeparam>
        /// <param name="sequence">A sequence to get the first element of.</param>
        /// <returns>The first element in the sequence; or <c>null</c> if no such element was found.</returns>
        public static T? FirstOrNullable<T>( this IEnumerable<T> sequence )
            where T : struct
        {
            if( sequence.NullReference() )
                throw new ArgumentNullException(nameof(sequence)).StoreFileLine();

            var list = sequence as IList<T>;
            if( list.NotNullReference() )
            {
                if( list.Count != 0 )
                    return list[0];
                else
                    return null;
            }

            var readOnlyList = sequence as IReadOnlyList<T>;
            if( readOnlyList.NotNullReference() )
            {
                if( readOnlyList.Count != 0 )
                    return readOnlyList[0];
                else
                    return null;
            }

            var enumerator = sequence.GetEnumerator();
            if( enumerator.MoveNext() )
                return enumerator.Current;
            else
                return null;
        }

        /// <summary>
        /// Returns the first element in a sequence that satisfies a specified condition,
        /// or <c>null</c> if the sequence contains no elements.
        /// </summary>
        /// <typeparam name="T">The type of the elelements of <paramref name="sequence"/>.</typeparam>
        /// <param name="sequence">A sequence to search the elements of.</param>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <returns>The first element in the sequence that passes the test in the specified predicate function; or <c>null</c> if no such element was found.</returns>
        public static T? FirstOrNullable<T>( this IEnumerable<T> sequence, Func<T, bool> predicate )
            where T : struct
        {
            return sequence.Where(predicate).FirstOrNullable();
        }

        #endregion

        #region Exception

        /// <summary>
        /// Gets the data stored in the exception.
        /// </summary>
        /// <param name="exception">The exception data was stored in.</param>
        /// <returns>The <see cref="StringStateCollection"/> instance in use by the exception, if there is one.</returns>
        public static StringStateCollection GetStoredData( this Exception exception )
        {
            if( exception.NullReference() )
                throw new ArgumentNullException(nameof(exception));

            return new StringStateCollection(exception.Data);
        }

        private static TException AddState<TException>( TException e, StringState state )
            where TException : Exception
        {
            var collection = GetStoredData(e);
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
            var collection = GetStoredData(e);
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

        private static TException StoreFileLine_IfSourcePositionNotFound<TException>( TException e, string file, string member, int line )
            where TException : Exception
        {
            var collection = GetStoredData(e);
            if( !collection.HasPartialStackTrace )
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
            // NOTE: We would like to have, at minimum, the source position this exception is being thrown from.
            StoreFileLine_IfSourcePositionNotFound(e, file, member, line);
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

        /// <summary>
        /// Executes the delegate, and stores it's return value in the exception.
        /// Exceptions thrown by the delegate are silently handled.
        /// </summary>
        /// <typeparam name="TException">The type of the exception.</typeparam>
        /// <typeparam name="TValue">The type of value to record.</typeparam>
        /// <param name="e">The exception to store data in.</param>
        /// <param name="name">The name of the value.</param>
        /// <param name="dlgt">The delegate returning the value to store. Exceptions thrown will be silently caught.</param>
        /// <param name="file">The source file that contains the caller.</param>
        /// <param name="member">The method or property name of the caller to this method.</param>
        /// <param name="line">The line number in the source file at which this method is called.</param>
        /// <returns>The exception data was stored in.</returns>
        public static TException Store<TException, TValue>(
            this TException e,
            string name,
            Func<TValue> dlgt,
            [CallerFilePath] string file = "",
            [CallerMemberName] string member = "",
            [CallerLineNumber] int line = 0 )
            where TException : Exception
        {
            TValue value;
            Exception exception;
            try
            {
                if( dlgt.NullReference() )
                    throw new ArgumentNullException(nameof(dlgt));

                value = dlgt();
                exception = null;
            }
            catch( Exception ex )
            {
                value = default(TValue);
                exception = ex;
            }

            if( exception.NullReference() )
                return Store(e, StringState.From(name, value), file, member, line);
            else
                return Store(e, StringState.From(name, $"{exception.GetType().Name}: {exception.Message}"), file, member, line);
        }

        #endregion
    }
}
