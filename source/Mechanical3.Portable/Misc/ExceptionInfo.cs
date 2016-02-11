using System;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Text;
using Mechanical3.Core;

namespace Mechanical3.Misc
{
    /// <summary>
    /// Basic, immutable information about an exception.
    /// </summary>
    public class ExceptionInfo
    {
        #region Private Fields

        private const string SingleIndentation = "  ";

        private readonly string type;
        private readonly string message;
        private readonly StackTraceInfo stackTrace;
        private readonly StringState[] data;
        private readonly ExceptionInfo[] innerExceptions;

        #endregion

        #region Constructors

        private ExceptionInfo(
            string type,
            string message,
            StackTraceInfo stackTrace,
            StringState[] data,
            ExceptionInfo[] innerExceptions )
        {
            this.type = type ?? string.Empty;
            this.message = message ?? string.Empty;
            this.stackTrace = stackTrace;
            this.data = data.NotNullReference() && data.Length > 0 ? data : null;
            this.innerExceptions = innerExceptions.NotNullReference() && innerExceptions.Length > 0 ? innerExceptions : null;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExceptionInfo"/> class.
        /// </summary>
        /// <param name="exception">The <see cref="Exception"/> to extract information from.</param>
        public ExceptionInfo( Exception exception )
            : this(
                  type: SafeString.DebugPrint(exception.GetType()),
                  message: exception.Message,
                  stackTrace: StackTraceInfo.From(exception.StackTrace),
                  data: exception.GetStoredData().ToArray(),
                  innerExceptions: GetInnerExceptions(exception))
        {
        }

        private static ExceptionInfo[] GetInnerExceptions( Exception exception )
        {
            if( exception.InnerException.NullReference() )
                return null;

            if( exception is AggregateException )
                return ((AggregateException)exception).InnerExceptions.Select(e => new ExceptionInfo(e)).ToArray();
            else
                return new ExceptionInfo[] { new ExceptionInfo(exception.InnerException) };
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets the exception type.
        /// </summary>
        /// <value>The exception type.</value>
        public string Type
        {
            get { return this.type; }
        }

        /// <summary>
        /// Gets the exception message.
        /// </summary>
        /// <value>The exception message.</value>
        public string Message
        {
            get { return this.message; }
        }

        /// <summary>
        /// Gets the exception's stack trace.
        /// </summary>
        /// <value>The exception's stack trace.</value>
        public StackTraceInfo StackTrace
        {
            get { return this.stackTrace; }
        }

        /// <summary>
        /// Gets the data manually stored in the exception (using the Store* methods).
        /// </summary>
        /// <value>Data manually stored in the exception.</value>
        public ImmutableArray<StringState> Data
        {
            get
            {
                if( this.data.NullReference() )
                    return ImmutableArray<StringState>.Empty;
                else
                    return this.data.ToImmutableArray();
            }
        }

        /// <summary>
        /// Gets the <see cref="ExceptionInfo"/> that caused the current exception.
        /// </summary>
        /// <value>The <see cref="ExceptionInfo"/> that caused the current exception.</value>
        public ExceptionInfo InnerException
        {
            get
            {
                if( this.innerExceptions.NullReference() )
                    return null;
                else
                    return this.innerExceptions[0]; // same as AggregateException
            }
        }

        /// <summary>
        /// Gets all inner exceptions of <see cref="AggregateException"/> instances.
        /// </summary>
        /// <value>All inner exceptions of <see cref="AggregateException"/> instances.</value>
        public ImmutableArray<ExceptionInfo> InnerExceptions
        {
            get
            {
                if( this.innerExceptions.NullReference() )
                    return ImmutableArray<ExceptionInfo>.Empty;
                else
                    return this.innerExceptions.ToImmutableArray();
            }
        }

        #endregion

        #region Printing

        private static void Append( StringBuilder sb, ExceptionInfo info, string indentation )
        {
            sb.Append(indentation);
            sb.Append("Type: ");
            sb.Append(info.Type);

            if( !info.Message.NullOrWhiteSpace() )
            {
                sb.AppendLine();
                sb.Append(indentation);
                sb.Append("Message: ");
                sb.Append(info.Message);
            }

            if( info.Data.Length > 0 )
            {
                sb.AppendLine();
                sb.Append(indentation);
                sb.Append("Data:"); // no newline here

                foreach( var state in info.Data )
                {
                    sb.AppendLine(); // newline here
                    sb.Append(indentation);
                    sb.Append(SingleIndentation);
                    sb.Append(state.Name);
                    sb.Append(" = ");
                    sb.Append(state.Value);
                    //// no newline here
                }
            }

            if( info.StackTrace.NotNullReference() ) // this can actually happen
            {
                sb.AppendLine();
                sb.Append(indentation);
                sb.AppendLine("StackTrace:");
                sb.Append(info.StackTrace);
            }

            if( info.InnerException.NotNullReference() )
            {
                if( info.InnerExceptions.Length == 1 )
                {
                    sb.AppendLine();
                    sb.AppendLine();
                    sb.AppendLine();

                    sb.Append(indentation);
                    sb.AppendLine("InnerException:");
                    Append(sb, info.InnerExceptions[0], indentation + SingleIndentation);
                }
                else
                {
                    for( int i = 0; i < info.InnerExceptions.Length; ++i )
                    {
                        sb.AppendLine();
                        sb.AppendLine();
                        sb.AppendLine();

                        sb.Append(indentation);
                        sb.Append("InnerExceptions[");
                        sb.Append(i.ToString("D", CultureInfo.InvariantCulture));
                        sb.AppendLine("]:");
                        Append(sb, info.InnerExceptions[i], indentation + SingleIndentation);
                    }
                }
            }
        }

        /// <summary>
        /// Returns the string representation of the exception.
        /// </summary>
        /// <returns>The string representation of the exception.</returns>
        public override string ToString()
        {
            var sb = new StringBuilder();
            Append(sb, this, indentation: string.Empty);
            return sb.ToString();
        }

        #endregion
    }
}
