using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Mechanical3.Core;

namespace Mechanical3.Misc
{
    /// <summary>
    /// An immutable class that helps exploring stack trace information.
    /// </summary>
    public class StackTraceInfo
    {
        #region Private Fields

        private static readonly Regex[] StackFrameRegexes = new Regex[]
            {
                // "  at member in file:[line] 0"
                new Regex(@"^\s+at\s+(?<member>.+)\s+in\s+(?<file>.+)\:(?:\s*line)?\s*(?<line>\d+)\s*$", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.Singleline), // have seen examples of this

                // "  at member in file"
                new Regex(@"^\s+at\s+(?<member>.+)\s+in\s+(?<file>.+)$", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.Singleline),

                // "  at member"
                new Regex(@"^\s+at\s+(?<member>.+)$", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.Singleline), // have seen examples of this

                // --- End of stack trace from previous location where exception was thrown ---
                new Regex(@"^---[^-]+---$", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.Singleline), // have seen examples of this with async/await (possibly due to custom awaiters)
            };

        private readonly FileLineInfo[] frames;

        #endregion

        #region Constructors

        private StackTraceInfo( FileLineInfo[] stackFrames )
        {
            if( stackFrames.NullReference() )
                throw new ArgumentNullException(nameof(stackFrames)).StoreFileLine();

            if( stackFrames.Length == 0 )
                throw new ArgumentException("No stack frames specified!").StoreFileLine();

            this.frames = stackFrames;
        }

        /// <summary>
        /// Creates a new <see cref="StackTraceInfo"/> instance from the specified stack frames.
        /// </summary>
        /// <param name="stackFrames">The stack frames to create a new <see cref="StackTraceInfo"/> instance from. If <c>null</c> or empty, the result will be <c>null</c>.</param>
        /// <returns>A new <see cref="StackTraceInfo"/> instance; or <c>null</c> if there were no stack frames specified.</returns>
        public static StackTraceInfo From( params FileLineInfo[] stackFrames )
        {
            if( stackFrames.NullReference()
             || stackFrames.Length == 0 )
                return null;

            return new StackTraceInfo(stackFrames);
        }

        /// <summary>
        /// Parses the specified string into a new <see cref="StackTraceInfo"/> instance.
        /// Returns <c>null</c>, if there were no stack frames in the string.
        /// </summary>
        /// <param name="stackTrace">The stack trace string to parse.</param>
        /// <returns>A new <see cref="StackTraceInfo"/> instance; or <c>null</c> if there were no stack frames in the string.</returns>
        public static StackTraceInfo From( string stackTrace )
        {
            if( stackTrace.NullOrEmpty() )
                return null;

            // the stack frame format may change from line to line
            var parsedFrames = new List<FileLineInfo>();
            using( var reader = new StringReader(stackTrace) )
            {
                string line;
                while( (line = reader.ReadLine()).NotNullReference() )
                {
                    bool success = false;
                    foreach( var regex in StackFrameRegexes )
                    {
                        var match = regex.Match(line);
                        if( match.Success )
                        {
                            string file = match.Groups["file"].Success ? match.Groups["file"].Value : null;
                            string member = match.Groups["member"].Success ? match.Groups["member"].Value : null;
                            int? fileLine = match.Groups["line"].Success ? int.Parse(match.Groups["line"].Value, NumberStyles.Integer, CultureInfo.InvariantCulture) : (int?)null;

                            if( member.NotNullReference() )
                                parsedFrames.Add(new FileLineInfo(file, member, fileLine));

                            success = true;
                            break;
                        }
                    }

                    if( !success )
                        throw new FormatException().Store(nameof(line), line).Store(nameof(stackTrace), stackTrace);
                }
            }

            return new StackTraceInfo(parsedFrames.ToArray());
        }

        #endregion

        #region Public Members

        /// <summary>
        /// Gets the stack frames of this stack trace.
        /// </summary>
        /// <value>The stack frames of the stack trace.</value>
        public ImmutableArray<FileLineInfo> Frames
        {
            get
            {
                if( this.frames.NullReference() )
                    return ImmutableArray<FileLineInfo>.Empty;
                else
                    return this.frames.ToImmutableArray();
            }
        }

        /// <summary>
        /// Returns the string representation of this stack trace.
        /// </summary>
        /// <returns>The string representation of this stack trace.</returns>
        public override string ToString()
        {
            var sb = new StringBuilder();
            foreach( var f in this.frames )
                AppendStackFrame(sb, f);
            return sb.ToString();
        }

        /// <summary>
        /// Appends the specified stack frame to the end of the stack trace.
        /// </summary>
        /// <param name="stackTrace">The stack trace to append to.</param>
        /// <param name="stackFrame">The stack frame to append to the end of the stack trace.</param>
        public static void AppendStackFrame( StringBuilder stackTrace, FileLineInfo stackFrame )
        {
            if( stackTrace == null )
                throw new ArgumentNullException(nameof(stackTrace)).StoreFileLine();

            if( stackTrace.Length != 0
             && stackTrace[stackTrace.Length - 1] != '\n' )
                stackTrace.Append("\r\n"); // tests show CRLF line terminator on both Windows and Android

            stackTrace.Append("   at ");
            stackTrace.Append(stackFrame.Member);
            if( stackFrame.File.NotNullReference() )
            {
                stackTrace.Append(" in ");
                stackTrace.Append(stackFrame.File);
                if( stackFrame.Line.HasValue )
                {
                    stackTrace.Append(":line ");
                    stackTrace.Append(stackFrame.Line.Value.ToString("D", CultureInfo.InvariantCulture));
                }
            }
        }

        #endregion
    }
}
