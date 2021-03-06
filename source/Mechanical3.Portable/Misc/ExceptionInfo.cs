﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Mechanical3.Core;
using Mechanical3.DataStores;

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
        private readonly string stackTrace;
        private readonly StringState[] data;
        private readonly ExceptionInfo[] innerExceptions;

        #endregion

        #region Constructors

        private ExceptionInfo(
            string type,
            string message,
            string stackTrace,
            StringState[] data,
            ExceptionInfo[] innerExceptions )
        {
            this.type = type ?? string.Empty;
            this.message = message ?? string.Empty;
            this.stackTrace = stackTrace ?? string.Empty;
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
                  stackTrace: exception.StackTrace,
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
        public string StackTrace
        {
            get { return this.stackTrace; }
        }

        /// <summary>
        /// Gets the exception's user generated partial stack trace from <see cref="Data"/>.
        /// </summary>
        /// <value>The exception's partial stack trace.</value>
        public string PartialStackTrace
        {
            get
            {
                var item = this.Data.FirstOrNullable(s => string.Equals(s.Name, StringStateCollection.PartialStackTraceKey, StringComparison.Ordinal));
                return item.HasValue ? item.Value.Value : null;
            }
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

        private static string IndentMultiLineString( string str, string indentation )
        {
            var sb = new StringBuilder();
            using( var reader = new StringReader(str) )
            {
                string line;
                while( (line = reader.ReadLine()).NotNullReference() )
                {
                    if( sb.Length != 0 )
                        sb.AppendLine();

                    sb.Append(indentation);
                    sb.Append(line.Trim());
                }
            }
            return sb.ToString();
        }

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
                    if( string.Equals(state.Name, StringStateCollection.PartialStackTraceKey, StringComparison.Ordinal) )
                        continue;

                    sb.AppendLine(); // newline here
                    sb.Append(indentation);
                    sb.Append(SingleIndentation);
                    sb.Append(state.Name);
                    sb.Append(" = ");
                    sb.Append(state.DisplayValue);
                    //// no newline here
                }
            }

            if( !info.PartialStackTrace.NullOrEmpty() )
            {
                sb.AppendLine();
                sb.Append(indentation);
                sb.AppendLine("PartialStackTrace:");
                sb.Append(IndentMultiLineString(info.PartialStackTrace, indentation + SingleIndentation));
            }

            if( !info.StackTrace.NullOrEmpty() ) // this can actually happen
            {
                //// NOTE: we print this the end, because it is easy to miss info below large stack traces
                sb.AppendLine();
                sb.Append(indentation);
                sb.AppendLine("StackTrace:");
                sb.Append(IndentMultiLineString(info.StackTrace, indentation + SingleIndentation));
            }

            // repeat for inner exceptions
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

        #region Serialization

        private static class Keys
        {
            internal const string Type = "Type";
            internal const string Message = "Message";
            internal const string StackTrace = "StackTrace";
            internal const string Data = "Data";
            internal const string InnerExceptions = "InnerExceptions";
        }

        /// <summary>
        /// Saves the specified instance.
        /// </summary>
        /// <param name="info">The instance to save. May be <c>null</c>.</param>
        /// <param name="writer">The <see cref="DataStoreTextWriter"/> to use.</param>
        public static void Save( ExceptionInfo info, DataStoreTextWriter writer )
        {
            if( writer.NullReference() )
                throw new ArgumentNullException(nameof(writer)).StoreFileLine();

            if( info.NullReference() )
            {
                writer.WriteNull();
                return;
            }

            writer.WriteObjectStart();
            writer.WriteValue(Keys.Type, info.Type);
            writer.WriteValue(Keys.Message, info.Message);
            writer.WriteValue(Keys.StackTrace, info.StackTrace); // may be null

            writer.WriteArrayStart(Keys.Data);
            foreach( var data in info.Data )
                data.SaveTo(writer);
            writer.WriteEnd();

            writer.WriteArrayStart(Keys.InnerExceptions);
            foreach( var inner in info.InnerExceptions )
                Save(inner, writer);
            writer.WriteEnd();

            writer.WriteEnd();
        }

        /// <summary>
        /// Loads the specified instance.
        /// </summary>
        /// <param name="reader">The <see cref="DataStoreTextReader"/> to use.</param>
        /// <returns>The instance loaded. May be <c>null</c>.</returns>
        public static ExceptionInfo LoadFrom( DataStoreTextReader reader )
        {
            if( reader.NullReference() )
                throw new ArgumentNullException(nameof(reader)).StoreFileLine();

            if( reader.Token == DataStoreToken.Value )
            {
                reader.AssertNull();
                return null;
            }
            else
            {
                reader.AssertObjectStart();

                var type = reader.ReadValue<string>(Keys.Type);
                var message = reader.ReadValue<string>(Keys.Message);
                var stackTrace = reader.ReadValue<string>(Keys.StackTrace);

                reader.ReadArrayStart(Keys.Data);
                var data = new List<StringState>();
                while( reader.Read() && reader.Token != DataStoreToken.End )
                    data.Add(StringState.LoadFrom(reader));
                reader.AssertEnd();

                reader.ReadArrayStart(Keys.InnerExceptions);
                var inner = new List<ExceptionInfo>();
                while( reader.Read() && reader.Token != DataStoreToken.End )
                    inner.Add(LoadFrom(reader));
                reader.AssertEnd();

                reader.ReadEnd();
                return new ExceptionInfo(type, message, stackTrace, data.ToArray(), inner.ToArray());
            }
        }

        #endregion
    }
}
