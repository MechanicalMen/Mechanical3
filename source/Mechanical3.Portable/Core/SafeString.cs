using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using Mechanical3.Misc;

namespace Mechanical3.Core
{
    //// NOTE: code here should depend only on the .NET framework!

    /// <summary>
    /// String formatting methods, which do not throw exceptions, or return <c>null</c>.
    /// </summary>
    public static class SafeString
    {
        //// NOTE: SafeString.Format is like a "safer" version of String.Format.
        ////       SafeString.Print is like a "safer" version of 'obj.ToString(format, formatProvider)'

        #region Internal Static Methods

#pragma warning disable SA1600 // Elements must be documented

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool TryParseInt32( string str, out int value )
        {
            return int.TryParse(str, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
        }

#pragma warning restore SA1600

        #endregion

        #region TryPrint, Print

        /// <summary>
        /// Gets the string representation of an object in the specified format.
        /// Always returns a non-null string.
        /// Throws no exceptions.
        /// </summary>
        /// <param name="obj">The object to format.</param>
        /// <param name="format">The format to use; or <c>null</c>.</param>
        /// <param name="formatProvider">An object that supplies culture-specific formatting information; or <c>null</c>.</param>
        /// <param name="result">The string representation of <paramref name="obj"/> in the specified format.</param>
        /// <returns><c>false</c> if <see cref="M:String.Format"/> would have thrown an exception; otherwise <c>true</c>.</returns>
        public static bool TryPrint( object obj, string format, IFormatProvider formatProvider, out string result )
        {
            bool completeSuccess = true;

            // can we leave the job to a custom formatter?
            if( formatProvider.NotNullReference() )
            {
                // try to find one
                ICustomFormatter customFormatter;
                try
                {
                    customFormatter = formatProvider.GetFormat(typeof(ICustomFormatter)) as ICustomFormatter;
                }
                catch
                {
                    completeSuccess = false;
                    customFormatter = null;
                }

                // try to use one
                if( customFormatter.NotNullReference() )
                {
                    try
                    {
                        result = customFormatter.Format(format, obj, formatProvider);
                    }
                    catch
                    {
                        completeSuccess = false;
                        result = null;
                    }

                    // did it work?
                    if( result.NotNullReference() )
                        return completeSuccess;
                }
            }

            if( obj.NotNullReference() )
            {
                // is it perhaps formattable?
                var formattable = obj as IFormattable;
                if( formattable.NotNullReference() )
                {
                    try
                    {
                        result = formattable.ToString(format, formatProvider);
                    }
                    catch
                    {
                        completeSuccess = false;
                        result = null;
                    }

                    // did it work?
                    if( result.NotNullReference() )
                        return completeSuccess;
                }

                // nothing left but ToString
                try
                {
                    result = obj.ToString();
                }
                catch
                {
                    completeSuccess = false;
                    result = null;
                }

                // did it work?
                if( result.NotNullReference() )
                    return completeSuccess;
            }

            result = string.Empty;
            return completeSuccess; // NOT 'return false' !
        }

        /// <summary>
        /// Gets the string representation of an object in the specified format.
        /// Always returns a non-null string.
        /// Throws no exceptions.
        /// </summary>
        /// <param name="obj">The object to format.</param>
        /// <param name="format">The format to use; or <c>null</c>.</param>
        /// <param name="result">The string representation of <paramref name="obj"/> in the specified format.</param>
        /// <returns><c>false</c> if <see cref="M:String.Format"/> would have thrown an exception; otherwise <c>true</c>.</returns>
        public static bool TryPrint( object obj, string format, out string result )
        {
            return TryPrint(obj, format, CultureInfo.CurrentCulture, out result);
        }

        /// <summary>
        /// Gets the string representation of an object in the specified format.
        /// Always returns a non-null string.
        /// Throws no exceptions.
        /// </summary>
        /// <param name="obj">The object to format.</param>
        /// <param name="formatProvider">An object that supplies culture-specific formatting information; or <c>null</c>.</param>
        /// <param name="result">The string representation of <paramref name="obj"/> in the specified format.</param>
        /// <returns><c>false</c> if <see cref="M:String.Format"/> would have thrown an exception; otherwise <c>true</c>.</returns>
        public static bool TryPrint( object obj, IFormatProvider formatProvider, out string result )
        {
            return TryPrint(obj, null, formatProvider, out result);
        }

        /// <summary>
        /// Gets the string representation of an object.
        /// Always returns a non-null string.
        /// Throws no exceptions.
        /// </summary>
        /// <param name="obj">The object to format.</param>
        /// <param name="result">The string representation of <paramref name="obj"/>.</param>
        /// <returns><c>false</c> if <see cref="M:String.Format"/> would have thrown an exception; otherwise <c>true</c>.</returns>
        public static bool TryPrint( object obj, out string result )
        {
            return TryPrint(obj, null, CultureInfo.CurrentCulture, out result);
        }

        /// <summary>
        /// Gets the string representation of an object in the specified format.
        /// Always returns a non-null string.
        /// Throws no exceptions.
        /// </summary>
        /// <param name="obj">The object to format.</param>
        /// <param name="format">The format to use; or <c>null</c>.</param>
        /// <param name="formatProvider">An object that supplies culture-specific formatting information; or <c>null</c>.</param>
        /// <returns>The string representation of <paramref name="obj"/> in the specified format.</returns>
        public static string Print( object obj, string format, IFormatProvider formatProvider )
        {
            string result;
            TryPrint(obj, format, formatProvider, out result);
            return result;
        }

        /// <summary>
        /// Gets the string representation of an object in the specified format.
        /// Always returns a non-null string.
        /// Throws no exceptions.
        /// </summary>
        /// <param name="obj">The object to format.</param>
        /// <param name="format">The format to use; or <c>null</c>.</param>
        /// <returns>The string representation of <paramref name="obj"/> in the specified format.</returns>
        public static string Print( object obj, string format )
        {
            string result;
            TryPrint(obj, format, CultureInfo.CurrentCulture, out result);
            return result;
        }

        /// <summary>
        /// Gets the string representation of an object in the specified format.
        /// Always returns a non-null string.
        /// Throws no exceptions.
        /// </summary>
        /// <param name="obj">The object to format.</param>
        /// <param name="formatProvider">An object that supplies culture-specific formatting information; or <c>null</c>.</param>
        /// <returns>The string representation of <paramref name="obj"/> in the specified format.</returns>
        public static string Print( object obj, IFormatProvider formatProvider )
        {
            string result;
            TryPrint(obj, null, formatProvider, out result);
            return result;
        }

        /// <summary>
        /// Gets the string representation of an object.
        /// Always returns a non-null string.
        /// Throws no exceptions.
        /// </summary>
        /// <param name="obj">The object to format.</param>
        /// <returns>The string representation of <paramref name="obj"/>.</returns>
        public static string Print( object obj )
        {
            string result;
            TryPrint(obj, null, CultureInfo.CurrentCulture, out result);
            return result;
        }

        #endregion

        #region TryFormat, Format

        private static readonly object[] EmptyObjectArray = new object[0];
        private static readonly char[] CurlyBrackets = new char[] { '{', '}' };

        /// <summary>
        /// Replaces the format item in a specified string with the string representation of a corresponding object in a specified array. A specified parameter supplies culture-specific formatting information.
        /// Always returns a non-null string.
        /// Throws no exceptions.
        /// </summary>
        /// <param name="result">The string representation of <paramref name="args"/> in the specified format.</param>
        /// <param name="provider">An object that supplies culture-specific formatting information; or <c>null</c>.</param>
        /// <param name="format">A composite format string.</param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        /// <returns><c>false</c> if <see cref="M:String.Format"/> would have thrown an exception; otherwise <c>true</c>.</returns>
        public static bool TryFormat( out string result, IFormatProvider provider, string format, params object[] args )
        {
            bool completeSuccess = true;

            if( format.NullReference() )
            {
                format = string.Empty;
                completeSuccess = false;
            }

            if( args.NullReference() )
            {
                args = EmptyObjectArray;
                completeSuccess = false;
            }

            var sb = new StringBuilder(format.Length + (args.Length * 8));
            int numWritten = 0;
            while( numWritten < format.Length )
            {
                int at = format.IndexOfAny(CurlyBrackets, numWritten);
                if( at == -1 )
                {
                    // no more special characters, write what's left and return
                    sb.Append(format, numWritten, format.Length - numWritten);
                    break;
                }
                else
                {
                    // special character found: write everything up to it
                    int length = at - numWritten;
                    sb.Append(format, numWritten, length);
                    numWritten += length;
                }


                if( format[at] == '}' )
                {
                    sb.Append('}');
                    ++numWritten;

                    if( at + 1 < format.Length
                     && format[at + 1] == '}' )
                    {
                        // escape sequence
                        ++numWritten;
                        continue;
                    }
                    else
                    {
                        // syntax error
                        completeSuccess = false;
                        continue;
                    }
                }

                // at this point we know that format[at] == '{'
                if( at + 1 < format.Length
                 && format[at + 1] == '{' )
                {
                    // escape sequence
                    sb.Append('{');
                    numWritten += 2;
                    continue;
                }
                else
                {
                    // the start of a composite format string
                    int endAt = format.IndexOf('}', at + 1);
                    if( endAt == -1 )
                    {
                        // syntax error
                        completeSuccess = false;
                        sb.Append(format, startIndex: at, count: format.Length - at);
                        numWritten += format.Length - at;
                        continue;
                    }

                    // find out if the alignment, or format string components were defined
                    int alignmentCharAt = format.IndexOf(',', at + 1);
                    if( alignmentCharAt >= endAt )
                        alignmentCharAt = -1;
                    int formatStringCharAt = alignmentCharAt == -1 ? format.IndexOf(':', at + 1) : format.IndexOf(':', alignmentCharAt + 1);
                    if( formatStringCharAt >= endAt )
                        formatStringCharAt = -1;


                    // find the substring representing the index component
                    int nextCharAfterIndexAt = endAt;
                    if( alignmentCharAt != -1 )
                        nextCharAfterIndexAt = alignmentCharAt;
                    else if( formatStringCharAt != -1 )
                        nextCharAfterIndexAt = formatStringCharAt;

                    // try to parse the index component
                    int index;
                    if( !TryParseInt32(format.Substring(at + 1, nextCharAfterIndexAt - (at + 1)), out index)
                     || index < 0
                     || index >= args.Length )
                    {
                        // bad format, or out of range
                        completeSuccess = false;
                        sb.Append(format, startIndex: at, count: endAt - at + 1);
                        numWritten += endAt - at + 1;
                        continue;
                    }

                    int alignment = 0;
                    if( alignmentCharAt != -1 )
                    {
                        // find the substring representing the alignment component
                        int nextCharAfterAlignmentAt = endAt;
                        if( formatStringCharAt != -1 )
                            nextCharAfterAlignmentAt = formatStringCharAt;

                        // try to parse alignment component
                        if( !TryParseInt32(format.Substring(alignmentCharAt + 1, nextCharAfterAlignmentAt - (alignmentCharAt + 1)), out alignment) )
                        {
                            // bad format
                            completeSuccess = false;
                            //// NOTE: we will try to continue without the alignment component
                        }
                    }


                    // extract format string
                    string currentFormatString = null;
                    if( formatStringCharAt != -1 )
                    {
                        int formatStringStartAt = Math.Max(alignmentCharAt + 1, formatStringCharAt + 1);
                        currentFormatString = format.Substring(formatStringStartAt, endAt - formatStringStartAt);
                    }


                    // produce the formatted string representation of the argument
                    var arg = args[index];
                    string text = null; // keeps compiler from nagging
                    completeSuccess = TryPrint(arg, currentFormatString, provider, out text) && completeSuccess;


                    // apply alignment component
                    if( alignment > 0 )
                        text = text.PadLeft(alignment);
                    else
                        text = text.PadRight(-alignment);


                    // write result and skip composite format string
                    sb.Append(text);
                    numWritten = endAt + 1;
                }
            }

            result = sb.ToString();
            return completeSuccess;
        }

        /// <summary>
        /// Replaces the format item in a specified string with the string representation of a corresponding object in a specified array.
        /// Always returns a non-null string.
        /// Throws no exceptions.
        /// </summary>
        /// <param name="result">A copy of <paramref name="format"/> in which the format items have been replaced by the string representation of the corresponding objects in <paramref name="args"/>.</param>
        /// <param name="format">A composite format string.</param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        /// <returns><c>false</c> if <see cref="M:String.Format"/> would have thrown an exception; otherwise <c>true</c>.</returns>
        public static bool TryFormat( out string result, string format, params object[] args )
        {
            return TryFormat(out result, null, format, args);
        }

        /// <summary>
        /// Replaces the format item in a specified string with the string representation of a corresponding object in a specified array. A specified parameter supplies culture-specific formatting information.
        /// Always returns a non-null string.
        /// Throws no exceptions.
        /// </summary>
        /// <param name="provider">An object that supplies culture-specific formatting information; or <c>null</c>.</param>
        /// <param name="format">A composite format string.</param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        /// <returns>A copy of <paramref name="format"/> in which the format items have been replaced by the string representation of the corresponding objects in <paramref name="args"/>.</returns>
        public static string Format( IFormatProvider provider, string format, params object[] args )
        {
            string result;
            TryFormat(out result, provider, format, args);
            return result;
        }

        /// <summary>
        /// Replaces the format item in a specified string with the string representation of a corresponding object in a specified array.
        /// Always returns a non-null string.
        /// Throws no exceptions.
        /// </summary>
        /// <param name="format">A composite format string.</param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        /// <returns>A copy of <paramref name="format"/> in which the format items have been replaced by the string representation of the corresponding objects in <paramref name="args"/>.</returns>
        public static string Format( string format, params object[] args )
        {
            string result;
            TryFormat(out result, null, format, args);
            return result;
        }

        #endregion

        #region DebugPrint, DebugFormat

        /// <summary>
        /// Gets the string representation of an object using the Debug formatter.
        /// Always returns a non-null string.
        /// Throws no exceptions.
        /// </summary>
        /// <param name="obj">The object to format.</param>
        /// <param name="format">The format to use; or <c>null</c>.</param>
        /// <returns>The string representation of <paramref name="obj"/>.</returns>
        public static string DebugPrint( object obj, string format = null )
        {
            return Print(obj, format, StringFormatter.Debug.Default);
        }

        /// <summary>
        /// Replaces the format item in a specified string with the string representation of a corresponding object in a specified array.
        /// Uses the Debug formatter.
        /// Always returns a non-null string.
        /// Throws no exceptions.
        /// </summary>
        /// <param name="format">A composite format string.</param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        /// <returns>A copy of <paramref name="format"/> in which the format items have been replaced by the string representation of the corresponding objects in <paramref name="args"/>.</returns>
        public static string DebugFormat( string format, params object[] args )
        {
            return Format(StringFormatter.Debug.Default, format, args);
        }

        #endregion

        #region InvariantPrint, InvariantFormat

        /// <summary>
        /// Gets the string representation of an object using the invariant culture.
        /// Always returns a non-null string.
        /// Throws no exceptions.
        /// </summary>
        /// <param name="obj">The object to format.</param>
        /// <param name="format">The format to use; or <c>null</c>.</param>
        /// <returns>The string representation of <paramref name="obj"/>.</returns>
        public static string InvariantPrint( object obj, string format = null )
        {
            return Print(obj, format, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Replaces the format item in a specified string with the string representation of a corresponding object in a specified array.
        /// Uses the invariant culture.
        /// Always returns a non-null string.
        /// Throws no exceptions.
        /// </summary>
        /// <param name="format">A composite format string.</param>
        /// <param name="args">An object array that contains zero or more objects to format.</param>
        /// <returns>A copy of <paramref name="format"/> in which the format items have been replaced by the string representation of the corresponding objects in <paramref name="args"/>.</returns>
        public static string InvariantFormat( string format, params object[] args )
        {
            return Format(CultureInfo.InvariantCulture, format, args);
        }

        #endregion
    }
}
