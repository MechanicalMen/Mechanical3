using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;
using Mechanical3.Core;

namespace Mechanical3.Misc
{
    //// NOTE: code here should depend only on the .NET framework! (except for SafeString)

    /// <summary>
    /// Helps implementing <see cref="ICustomFormatter"/>.
    /// </summary>
    public static class StringFormatter
    {
        //// NOTE: the Format method of an ICustomFormatter instance may be called in two ways:
        ////   - explicitly by the user, in which case the formatProvider passed to ICustomFormatter.Format may be anything.
        ////   - implicitly through an IFormatProvider passed to [Safe]String.Format, in which case
        ////     the formatProvider parameter of ICustomFormatter.Format is the same object
        ////     (which was passed to [Safe]String.Format; and the same object that ICustomFormatter.Format is being called on, although the interface is different)

        #region FallbackBase

        /// <summary>
        /// Inheritors of this type can format objects as they choose, and let other objects
        /// be formatted using the specified default format provider.
        /// </summary>
        public abstract class FallbackBase : IFormatProvider, ICustomFormatter
        {
            #region Private Fields

            private readonly IFormatProvider fallbackProvider;

            #endregion

            #region Constructors

            /// <summary>
            /// Initializes a new instance of the <see cref="FallbackBase"/> class.
            /// </summary>
            /// <param name="fallbackProvider">The <see cref="IFormatProvider"/> to fall back on, when custom formatting is not available, or fails.</param>
            protected FallbackBase( IFormatProvider fallbackProvider )
            {
                if( fallbackProvider.NullReference() )
                    throw new ArgumentException(nameof(fallbackProvider));

                this.fallbackProvider = fallbackProvider;
            }

            #endregion

            #region Protected Methods

            /// <summary>
            /// Override the <see cref="ICustomFormatter"/> fallback.
            /// Tries to converts the value of a specified object to an equivalent string representation using the specified format.
            /// </summary>
            /// <param name="arg">An object to format.</param>
            /// <param name="format">A format string containing formatting specifications.</param>
            /// <param name="formattedArg">The string representation of the value of <paramref name="arg"/>, formatted as specified by <paramref name="format"/>.</param>
            /// <returns><c>true</c> if <paramref name="arg"/> was successfully formatted; otherwise <c>false</c>.</returns>
            protected virtual bool TryFormat( object arg, string format, out string formattedArg )
            {
                formattedArg = null;
                return false;
            }

            /// <summary>
            /// Gets the string representation of the specified object, without invoking custom formatting (other than the fallback format provider).
            /// </summary>
            /// <param name="arg">An object to format.</param>
            /// <param name="format">A format string containing formatting specifications.</param>
            /// <returns>The string representation of the value of <paramref name="arg"/>, formatted as specified by <paramref name="format"/>.</returns>
            protected string PrintFallback( object arg, string format )
            {
                return Mechanical3.Core.SafeString.Print(arg, format, this.fallbackProvider);
            }

            #endregion

            #region IFormatProvider

            /// <summary>
            /// Returns an object that provides formatting services for the specified type.
            /// </summary>
            /// <param name="formatType">An object that specifies the type of format object to return.</param>
            /// <returns>An instance of the object specified by <paramref name="formatType"/>, if the <see cref="IFormatProvider"/> implementation can supply that type of object; otherwise, <c>null</c>.</returns>
            object IFormatProvider.GetFormat( Type formatType )
            {
                if( formatType == typeof(ICustomFormatter) )
                    return this;
                else
                    return this.fallbackProvider.GetFormat(formatType);
            }

            #endregion

            #region ICustomFormatter

            /// <summary>
            /// Converts the value of a specified object to an equivalent string representation using the specified format and culture-specific formatting information.
            /// </summary>
            /// <param name="format">A format string containing formatting specifications.</param>
            /// <param name="arg">An object to format.</param>
            /// <param name="formatProvider">An object that supplies format information about the current instance.</param>
            /// <returns>The string representation of the value of <paramref name="arg"/>, formatted as specified by <paramref name="format"/> and <paramref name="formatProvider"/>.</returns>
            string ICustomFormatter.Format( string format, object arg, IFormatProvider formatProvider )
            {
                bool customFormattingWorked;
                string customFormattedArg;
                try
                {
                    customFormattingWorked = this.TryFormat(arg, format, out customFormattedArg);
                }
                catch
                {
                    customFormattingWorked = false;
                    customFormattedArg = null;
                }

                if( customFormattingWorked
                 && customFormattedArg.NotNullReference() )
                    return customFormattedArg;
                else
                    return this.PrintFallback(arg, format);
            }

            #endregion
        }

        #endregion

        #region Enumerable

        /// <summary>
        /// Supports <see cref="IEnumerable"/> formatting.
        /// All other types fall back to the specified format provider.
        /// </summary>
        public class Enumerable : FallbackBase
        {
            #region Private Fields

            private const string OpeningBracket = "{";
            private const string ClosingBracket = "}";
            private const string Separator = ", ";
            private const string Ellipsis = "...";

            private const int DefaultMaxLength = 256;
            private const string DefaultElementFormat = null;

            #endregion

            #region Constructors

            /// <summary>
            /// Initializes a new instance of the <see cref="Enumerable"/> class.
            /// </summary>
            /// <param name="fallbackProvider">The <see cref="IFormatProvider"/> to fall back on, when custom formatting is not available, or fails.</param>
            public Enumerable( IFormatProvider fallbackProvider )
                : base(fallbackProvider)
            {
            }

            #endregion

            #region Protected Methods

            /// <summary>
            /// Override the <see cref="ICustomFormatter"/> fallback.
            /// Tries to converts the value of a specified object to an equivalent string representation using the specified format.
            /// </summary>
            /// <param name="arg">An object to format.</param>
            /// <param name="format">A format string containing formatting specifications.</param>
            /// <param name="formattedArg">The string representation of the value of <paramref name="arg"/>, formatted as specified by <paramref name="format"/>.</param>
            /// <returns><c>true</c> if <paramref name="arg"/> was successfully formatted; otherwise <c>false</c>.</returns>
            protected override bool TryFormat( object arg, string format, out string formattedArg )
            {
                if( this.IsEnumerable(arg) )
                {
                    formattedArg = this.Format((IEnumerable)arg, format);
                    return true;
                }
                else
                {
                    return base.TryFormat(arg, format, out formattedArg);
                }
            }

            #endregion

            #region Private Methods

            private bool IsEnumerable( object obj )
            {
                return (obj is IEnumerable) && !(obj is string); // we don't want to print strings as a collection of characters!
            }

            private void ParseFormat( string format, out int maxLength, out string itemFormat )
            {
                if( format.NullOrEmpty() )
                    goto error;

                int semicolonIndex = format.IndexOf(';');
                string enumerableFormat;
                if( semicolonIndex != -1 )
                {
                    enumerableFormat = format.Substring(startIndex: 0, length: semicolonIndex);
                    itemFormat = format.Substring(startIndex: semicolonIndex + 1);
                }
                else
                {
                    enumerableFormat = format;
                    itemFormat = null;
                }

                if( string.Equals(enumerableFormat, "G", StringComparison.OrdinalIgnoreCase) )
                {
                    maxLength = DefaultMaxLength;
                }
                else if( enumerableFormat.Length > 0
                      && char.ToUpperInvariant(enumerableFormat[0]) == 'L' )
                {
                    if( !SafeString.TryParseInt32(enumerableFormat.Substring(startIndex: 1), out maxLength) )
                        goto error;
                }
                else
                {
                    goto error;
                }

                return;

            error:
                //// TODO: log format error
                maxLength = DefaultMaxLength;
                itemFormat = DefaultElementFormat;
            }

            private string Format( IEnumerable enumerable, string format )
            {
                int maxLength;
                string itemFormat;
                this.ParseFormat(format, out maxLength, out itemFormat);

                bool isFirstItem = true;
                object currentItem;
                string currentItemAsString;
                int closedLength;
                var sb = new StringBuilder();
                sb.Append(OpeningBracket);

                var enumerator = enumerable.GetEnumerator();
                while( enumerator.MoveNext() )
                {
                    if( isFirstItem )
                        isFirstItem = false;
                    else
                        sb.Append(Separator);

                    // print items using ourselves as a formatter
                    currentItem = enumerator.Current;
                    if( this.IsEnumerable(currentItem) )
                    {
                        // NOTE: We only apply the format string to enumerables, since other types may not ignore it.
                        //       For example a format like "L99:L0" is fine for enumerables, but when applied to an
                        //       integer like '5', it will be taken as a custom format string: most of it will be printed
                        //       as a literal, except for '0' which is a placeholder (the end result being 5 --> "L99:L5")
                        currentItemAsString = SafeString.Print(currentItem, format: itemFormat, formatProvider: this);
                    }
                    else
                    {
                        currentItemAsString = SafeString.Print(currentItem, format: null, formatProvider: this);
                    }

                    closedLength = sb.Length + currentItemAsString.Length + Separator.Length + Ellipsis.Length + ClosingBracket.Length;
                    if( closedLength <= maxLength )
                    {
                        sb.Append(currentItemAsString);
                    }
                    else
                    {
                        sb.Append(Ellipsis);
                        break;
                    }
                }

                sb.Append(ClosingBracket);

                // perform one last check
                if( sb.Length <= maxLength )
                    return sb.ToString();
                else
                    return string.Empty;
            }

            #endregion
        }

        #endregion

        #region Debug

        //// NOTE: we are not escaping non-ASCII or non-printable characters
        ////       that is left for the serializing code, or the displaying program to handle
        ////       (e.g. replacing CRLF with \r\n is the job of the interpreting program, not ours)

        /// <summary>
        /// Culture independent formatter for logging debug data.
        /// </summary>
        public class Debug : Enumerable
        {
            #region Private Fields

            private const string Null = "null";
            private static readonly Dictionary<Type, string> BuiltInTypes;

            #endregion

            #region Constructors

            static Debug()
            {
                BuiltInTypes = new Dictionary<Type, string>();
                BuiltInTypes.Add(typeof(byte), "byte");
                BuiltInTypes.Add(typeof(sbyte), "sbyte");
                BuiltInTypes.Add(typeof(short), "short");
                BuiltInTypes.Add(typeof(ushort), "ushort");
                BuiltInTypes.Add(typeof(int), "int");
                BuiltInTypes.Add(typeof(uint), "uint");
                BuiltInTypes.Add(typeof(long), "long");
                BuiltInTypes.Add(typeof(ulong), "ulong");
                BuiltInTypes.Add(typeof(float), "float");
                BuiltInTypes.Add(typeof(double), "double");
                BuiltInTypes.Add(typeof(decimal), "decimal");
                BuiltInTypes.Add(typeof(char), "char");
                BuiltInTypes.Add(typeof(string), "string");
                BuiltInTypes.Add(typeof(bool), "bool");
                BuiltInTypes.Add(typeof(object), "object");
                BuiltInTypes.Add(typeof(void), "void");
            }

            /// <summary>
            /// Initializes a new instance of the <see cref="Debug"/> class.
            /// </summary>
            protected Debug()
                : base(fallbackProvider: CultureInfo.InvariantCulture)
            {
            }

            #endregion

            #region Private Methods

            //// TODO: could Roslyn be used to pretty-print these?

            #region Type

            private void AppendArrayBase( StringBuilder sb, string format, Type type )
            {
                while( type.IsArray )
                    type = type.GetElementType();

                this.AppendType(sb, format, type);
            }

            private void AppendArrayBrackets( StringBuilder sb, string format, Type type )
            {
                if( type.IsArray )
                {
                    sb.Append('[');
                    sb.Append(',', type.GetArrayRank() - 1);
                    sb.Append(']');

                    this.AppendArrayBrackets(sb, format, type.GetElementType());
                }
            }

            private void AppendType( StringBuilder sb, string format, Type type )
            {
                if( type.IsGenericParameter )
                {
                    sb.Append(type.Name);
                }
                else if( type.IsArray )
                {
                    this.AppendArrayBase(sb, format, type);
                    this.AppendArrayBrackets(sb, format, type);
                }
                else
                {
                    string name;
                    if( BuiltInTypes.TryGetValue(type, out name) )
                    {
                        sb.Append(name);
                    }
                    else
                    {
                        var typeInfo = type.GetTypeInfo();

                        // print namespace and possibly declaring type
                        if( type.IsNested )
                        {
                            this.AppendType(sb, format, type.DeclaringType);
                            sb.Append('.');
                        }
                        else
                        {
                            // only print namespace if not from 'mscorlib'
                            if( !string.Equals(typeInfo.Assembly.GetName().Name, "mscorlib", StringComparison.Ordinal) )
                            {
                                // the namespace of nested types is the same as that of their declaring types
                                sb.Append(type.Namespace);
                                sb.Append('.');
                            }
                        }

                        // print name
                        name = type.Name;
                        int index = name.LastIndexOf('`');
                        if( index != -1 )
                            sb.Append(name, 0, index);
                        else
                            sb.Append(name);

                        // print generic arguments
                        // NOTE: same code as with MethodBase
                        if( typeInfo.IsGenericType )
                        {
                            Type[] types;
                            if( typeInfo.IsGenericTypeDefinition )
                                types = typeInfo.GenericTypeParameters;
                            else
                                types = type.GenericTypeArguments;

                            sb.Append('<');
                            this.AppendType(sb, format, types[0]);
                            for( int i = 1; i < types.Length; ++i )
                            {
                                sb.Append(", ");
                                this.AppendType(sb, format, types[i]);
                            }
                            sb.Append('>');
                        }
                    }
                }
            }

            #endregion

            #region ParameterInfo

            private void AppendParameter( StringBuilder sb, string format, ParameterInfo paramInfo )
            {
                var paramType = paramInfo.ParameterType;
                if( paramType.IsByRef )
                {
                    // the parameter is passed by reference
                    // is it 'out' or 'ref'?
                    if( paramInfo.IsOut )
                        sb.Append("out ");
                    else
                        sb.Append("ref ");

                    // float instead of Single&
                    paramType = paramType.GetElementType();
                }

                if( paramInfo.GetCustomAttribute<ParamArrayAttribute>().NotNullReference() )
                {
                    // 'params' parameter
                    sb.Append("params ");
                }

                // print parameter type
                this.AppendType(sb, format, paramType);
                sb.Append(' ');

                // print parameter name
                sb.Append(paramInfo.Name);
            }

            #endregion

            #region MethodBase

            private void AppendMethod( StringBuilder sb, string format, MethodBase methodBase )
            {
                // print the return value, if this is not a constructor
                var methodInfo = methodBase as MethodInfo;
                if( methodInfo.NotNullReference() )
                {
                    this.AppendType(sb, format, methodInfo.ReturnType);
                    sb.Append(' ');
                }

                // print the declaring type (if constructor)
                this.AppendType(sb, format, methodBase.DeclaringType);
                if( methodInfo.NotNullReference() )
                    sb.Append('.'); // constructors already have the '.' in their name

                // print the name
                sb.Append(methodBase.Name);

                // print generic parameters
                // NOTE: same code as with Type
                if( methodBase.IsGenericMethod )
                {
                    var types = methodBase.GetGenericArguments();

                    sb.Append('<');
                    this.AppendType(sb, format, types[0]);
                    for( int i = 1; i < types.Length; ++i )
                    {
                        sb.Append(", ");
                        this.AppendType(sb, format, types[i]);
                    }
                    sb.Append('>');
                }

                // print the parameter list
                var parameters = methodBase.GetParameters();
                sb.Append('(');

                if( parameters.Length != 0 )
                {
                    this.AppendParameter(sb, format, parameters[0]);
                    for( int i = 1; i < parameters.Length; ++i )
                    {
                        sb.Append(", ");
                        this.AppendParameter(sb, format, parameters[i]);
                    }
                }

                sb.Append(')');
            }

            #endregion

            #endregion

            #region Protected Methods

            /// <summary>
            /// Override the <see cref="ICustomFormatter"/> fallback.
            /// Tries to converts the value of a specified object to an equivalent string representation using the specified format.
            /// </summary>
            /// <param name="arg">An object to format.</param>
            /// <param name="format">A format string containing formatting specifications.</param>
            /// <param name="formattedArg">The string representation of the value of <paramref name="arg"/>, formatted as specified by <paramref name="format"/>.</param>
            /// <returns><c>true</c> if <paramref name="arg"/> was successfully formatted; otherwise <c>false</c>.</returns>
            protected override bool TryFormat( object arg, string format, out string formattedArg )
            {
                if( arg.NullReference() )
                {
                    formattedArg = Null;
                }
                else if( arg is sbyte )
                {
                    formattedArg = this.PrintFallback(arg, format) + "y";
                }
                else if( arg is byte )
                {
                    formattedArg = this.PrintFallback(arg, format) + "uy";
                }
                else if( arg is short )
                {
                    formattedArg = this.PrintFallback(arg, format) + "s";
                }
                else if( arg is ushort )
                {
                    formattedArg = this.PrintFallback(arg, format) + "us";
                }
                else if( arg is uint )
                {
                    formattedArg = this.PrintFallback(arg, format) + "u";
                }
                else if( arg is long )
                {
                    formattedArg = this.PrintFallback(arg, format) + "L";
                }
                else if( arg is ulong )
                {
                    formattedArg = this.PrintFallback(arg, format) + "UL";
                }
                else if( arg is float )
                {
                    formattedArg = this.PrintFallback(arg, string.IsNullOrEmpty(format) ? "R" : format) + "f";
                }
                else if( arg is double )
                {
                    formattedArg = this.PrintFallback(arg, string.IsNullOrEmpty(format) ? "R" : format) + "d";
                }
                else if( arg is decimal )
                {
                    formattedArg = this.PrintFallback(arg, format) + "m";
                }
                else if( arg is bool )
                {
                    formattedArg = (bool)arg ? "true" : "false";
                }
                else if( arg is char )
                {
                    formattedArg = ((char)arg).ToString();
                }
                else if( arg is string )
                {
                    formattedArg = (string)arg;
                }
                else if( arg is byte[] )
                {
                    formattedArg = Convert.ToBase64String((byte[])arg);
                }
                else if( arg is Type )
                {
                    var sb = new StringBuilder();
                    this.AppendType(sb, format, (Type)arg);
                    formattedArg = sb.ToString();
                }
                else if( arg is TypeInfo )
                {
                    var sb = new StringBuilder();
                    this.AppendType(sb, format, ((TypeInfo)arg).AsType());
                    formattedArg = sb.ToString();
                }
                else if( arg is ParameterInfo )
                {
                    var sb = new StringBuilder();
                    this.AppendParameter(sb, format, (ParameterInfo)arg);
                    formattedArg = sb.ToString();
                }
                else if( arg is MethodBase )
                {
                    var sb = new StringBuilder();
                    this.AppendMethod(sb, format, (MethodBase)arg);
                    formattedArg = sb.ToString();
                }
                else if( arg is Exception )
                {
                    formattedArg = ((Exception)arg).ToString();
                }
                else if( arg is DateTime )
                {
                    formattedArg = ((DateTime)arg).ToString("o", CultureInfo.InvariantCulture);
                }
                else if( arg is DateTimeOffset )
                {
                    formattedArg = ((DateTimeOffset)arg).ToString("o", CultureInfo.InvariantCulture);
                }
                else if( arg is TimeSpan )
                {
                    formattedArg = ((TimeSpan)arg).ToString("c", CultureInfo.InvariantCulture);
                }
                else
                {
                    var type = arg.GetType();
                    var typeInfo = type.GetTypeInfo();
                    if( typeInfo.IsGenericType )
                    {
                        var typeDef = type.GetGenericTypeDefinition();
                        if( typeDef == typeof(Nullable<>) )
                        {
                            if( arg.NullReference() )
                                formattedArg = Null;

                            var hasValue = (bool)arg.GetType().GetTypeInfo().GetDeclaredProperty("HasValue").GetValue(arg, index: null);
                            if( hasValue )
                            {
                                object value = arg.GetType().GetTypeInfo().GetDeclaredProperty("Value").GetValue(arg, index: null);
                                formattedArg = SafeString.Print(value, format, formatProvider: this);
                            }
                            else
                            {
                                formattedArg = Null;
                            }
                        }
                    }

                    // No custom formatting available here, but maybe our base type has something...
                    return base.TryFormat(arg, format, out formattedArg);
                }

                return true;
            }

            #endregion

            /// <summary>
            /// The default instance of the class.
            /// </summary>
            public static readonly Debug Default = new Debug();
        }

        #endregion
    }
}
