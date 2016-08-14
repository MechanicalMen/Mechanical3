using System;
using System.Globalization;
using Mechanical3.Misc;

namespace Mechanical3.Core
{
    /* Rules:
        *                     any character, zero or more times
        ?                     any character, zero or one time (NOTE: this is different in Glob patterns)
        !                     exactly one character
        \                     escape character (e.g. "\*", "\\", ...)
        any other character   matching of that character (case and culture sensitivity specified by user)
    */

    /// <summary>
    /// Basic string-based pattern matching. Syntax is similar to globs, but there are some key differences.
    /// </summary>
    public static class StringPattern
    {
        private static char[] specialCharacters = new char[] { '*', '?', '!', '\\' };

        /// <summary>
        /// Indicates whether the specified pattern matches the specified input string.
        /// </summary>
        /// <param name="text">The string to search for a match.</param>
        /// <param name="pattern">The string pattern to match.</param>
        /// <param name="comparison">The type of comparison to use for literals.</param>
        /// <returns><c>true</c> if the pattern matched the input string; otherwise, <c>false</c>.</returns>
        public static bool IsMatch( string text, string pattern, StringComparison comparison )
        {
            switch( comparison )
            {
            case StringComparison.CurrentCulture:
                return IsMatch(text, pattern, CultureInfo.CurrentCulture, CompareOptions.None);

            case StringComparison.CurrentCultureIgnoreCase:
                return IsMatch(text, pattern, CultureInfo.CurrentCulture, CompareOptions.IgnoreCase);

            case StringComparison.Ordinal:
                return IsMatch(text, pattern, CultureInfo.InvariantCulture, CompareOptions.Ordinal);

            case StringComparison.OrdinalIgnoreCase:
                return IsMatch(text, pattern, CultureInfo.InvariantCulture, CompareOptions.OrdinalIgnoreCase);

            default:
                throw new ArgumentException("Unknown comparison!").Store(nameof(comparison), comparison);
            }
        }

        /// <summary>
        /// Indicates whether the specified pattern matches the specified input string.
        /// </summary>
        /// <param name="text">The string to search for a match.</param>
        /// <param name="pattern">The string pattern to match.</param>
        /// <param name="culture">The culture to base for comparing literals.</param>
        /// <param name="options">The type of comparison to use the specified <paramref name="culture"/> for.</param>
        /// <returns><c>true</c> if the pattern matched the input string; otherwise, <c>false</c>.</returns>
        public static bool IsMatch( string text, string pattern, CultureInfo culture, CompareOptions options )
        {
            if( culture.NullReference() )
                throw new ArgumentNullException(nameof(culture)).StoreFileLine();

            return IsMatch(text, 0, text.Length, pattern, 0, pattern.Length, culture.CompareInfo, options);
        }

        /// <summary>
        /// Indicates whether the specified pattern matches the specified input string.
        /// </summary>
        /// <param name="text">The string to search for a match.</param>
        /// <param name="pattern">The string pattern to match.</param>
        /// <param name="localizedComparer">The <see cref="LocalizedStringComparer"/> to use.</param>
        /// <returns><c>true</c> if the pattern matched the input string; otherwise, <c>false</c>.</returns>
        public static bool IsMatch( string text, string pattern, LocalizedStringComparer localizedComparer )
        {
            if( localizedComparer.NullReference() )
                throw new ArgumentNullException(nameof(localizedComparer)).StoreFileLine();

            return IsMatch(text, 0, text.Length, pattern, 0, pattern.Length, localizedComparer.CompareInfo, localizedComparer.CompareOptions);
        }

        private static bool IsMatch( string text, int textStartIndex, int textLength, string pattern, int patternStartIndex, int patternLength, CompareInfo compareInfo, CompareOptions compareOptions )
        {
            try
            {
                int textIndex = textStartIndex;
                int patternIndex = patternStartIndex;
                int textEnd = textStartIndex + textLength;
                int patternEnd = patternStartIndex + patternLength;

                // consume pattern
                while( patternIndex < patternEnd )
                {
                    // search for next special character
                    int count = patternEnd - patternIndex; // number of pattern characters left
                    int nextSpecialCharAt = pattern.IndexOfAny(specialCharacters, startIndex: patternIndex, count: count);

                    // compare text before it
                    if( nextSpecialCharAt != -1 )
                    {
                        // number of characters to compare
                        count = nextSpecialCharAt - patternIndex;
                    }
                    if( textIndex + count > textEnd
                     || compareInfo.Compare(text, textIndex, count, pattern, patternIndex, count, compareOptions) != 0 )
                    {
                        // character mismatch
                        return false;
                    }
                    else
                    {
                        // pattern matches the text we found
                        patternIndex += count;
                        textIndex += count;
                    }

                    if( nextSpecialCharAt != -1 )
                    {
                        // see what character we found
                        switch( pattern[patternIndex] )
                        {
                        case '*':
                            // match as much as possible
                            // (only constrained by the rest of the pattern)
                            {
                                for( int matchEnd = textEnd; matchEnd >= textIndex; matchEnd-- )
                                {
                                    if( IsMatch(text, matchEnd, textEnd - matchEnd, pattern, patternIndex + 1, patternEnd - patternIndex - 1, compareInfo, compareOptions) )
                                    {
                                        // match found
                                        return true;
                                    }
                                }

                                // no match
                                return false;
                            }

                        case '?':
                            // match the current text character, it the rest of the pattern can be matched
                            if( IsMatch(text, textIndex + 1, textEnd - textIndex - 1, pattern, patternIndex + 1, patternEnd - patternIndex - 1, compareInfo, compareOptions) )
                            {
                                // match found
                                return true;
                            }
                            else
                            {
                                // zero characters matched
                                ++patternIndex;
                            }
                            break;

                        case '!':
                            // match any character, once
                            ++patternIndex;
                            ++textIndex;
                            break;

                        case '\\':
                            // match the special character after it
                            {
                                // no more pattern?
                                if( patternIndex == patternEnd - 1 )
                                    throw new FormatException("Invalid use of escape character!").StoreFileLine();

                                // escaped character recognized?
                                if( Array.IndexOf(specialCharacters, pattern[patternIndex + 1]) == -1 )
                                    throw new FormatException("Invalid use of escape character!").StoreFileLine();

                                // escaped character matches text character?
                                if( text[textIndex] != pattern[patternIndex + 1] )
                                {
                                    // character mismatch
                                    return false;
                                }
                                else
                                {
                                    // escaped character found in text
                                    ++textIndex;
                                    patternIndex += 2;
                                }
                            }
                            break;

                        default:
                            throw new NotImplementedException().StoreFileLine();
                        }
                    }
                }

                // pattern matching finished:
                // we should have "read" all of the text at this point
                return textIndex == textEnd;
            }
            catch( Exception ex )
            {
                ex.Store("text", text.Substring(textStartIndex, textLength));
                ex.Store("pattern", pattern.Substring(patternStartIndex, patternLength));
                ex.Store("cultureName", compareInfo.Name);
                ex.Store(nameof(compareOptions), compareOptions);
                throw;
            }
        }
    }
}
