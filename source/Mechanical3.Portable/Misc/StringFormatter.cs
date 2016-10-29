using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Text;
using Mechanical3.Core;

namespace Mechanical3.Misc
{
    /// <summary>
    /// Classes implementing common string formatting tasks.
    /// They are intended to be consumed by methods like SafeString.Format,
    /// string.Format or StringBuilder.AppendFormat.
    /// </summary>
    public static class StringFormatter
    {
        #region Base

        /// <summary>
        /// Inheritors can implement custom string formatting of objects.
        /// </summary>
        public abstract class Base : IFormatProvider, ICustomFormatter
        {
            #region Private Fields

            private readonly IFormatProvider fallbackFormatProvider;

            #endregion

            #region Constructor

            /// <summary>
            /// Initializes a new instance of the <see cref="Base"/> class.
            /// </summary>
            /// <param name="formatProvider">The <see cref="IFormatProvider"/> to fall back on, if inheritors can not format the specified object.</param>
            protected Base( IFormatProvider formatProvider )
            {
                if( formatProvider.NullReference() )
                    throw new ArgumentNullException(nameof(formatProvider)).StoreFileLine();

                this.fallbackFormatProvider = formatProvider;
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
                    return this.fallbackFormatProvider.GetFormat(formatType);
            }

            #endregion

            #region ICustomFormatter

            /// <summary>
            /// Converts the value of a specified object to an equivalent string representation using the specified format.
            /// The specified culture-specific formatting information is ignored.
            /// </summary>
            /// <param name="format">A format string containing formatting specifications.</param>
            /// <param name="arg">An object to format.</param>
            /// <param name="formatProvider">An object that supplies format information about the current instance.</param>
            /// <returns>The string representation of the value of <paramref name="arg"/>, formatted as specified by <paramref name="format"/>.</returns>
            string ICustomFormatter.Format( string format, object arg, IFormatProvider formatProvider )
            {
                //// NOTE: We ignore the specified IFormatProvider. This is fine,
                ////       since unless this method is invoked through explicitly through ICustomFormatter,
                ////       we know that it is the same as "this" object (see SafeString.Format or string.Format implementation).

                return this.Format(arg, format);
            }

            #endregion

            #region Protected Abstract Methods

            /// <summary>
            /// Tries to convert the specified object to a string, using to the specified format.
            /// </summary>
            /// <param name="objectToFormat">The object to convert to a string.</param>
            /// <param name="formatString">The format of the generated string (i.e. the number of decimal places, whether to use a currency sign, ... etc.).</param>
            /// <param name="formattedObject">If the operation was successful, the <paramref name="objectToFormat"/> parameter converted to a string, in the specified format; otherwise <c>null</c>.</param>
            /// <returns><c>true</c> if the specified object could be formatted; otherwise, <c>false</c>.</returns>
            protected abstract bool TryFormat( object objectToFormat, string formatString, out string formattedObject );

            #endregion

            #region Protected Methods

            /// <summary>
            /// Converts the specified object to an equivalent string representation using the specified format, and the fallback <see cref="IFormatProvider"/>.
            /// </summary>
            /// <param name="objectToFormat">The object to convert to a string.</param>
            /// <param name="formatString">The format of the generated string (i.e. the number of decimal places, whether to use a currency sign, ... etc.).</param>
            /// <returns>The string representation of <paramref name="objectToFormat"/>, formatted using <paramref name="formatString"/> and the fallback <see cref="IFormatProvider"/>.</returns>
            public string FallbackFormat( object objectToFormat, string formatString )
            {
                // NOTE: this is not an infinite loop, since the fallback IFormatProvider
                //       will not return our reference as an ICustomFormatter
                return SafeString.Print(objectToFormat, formatString, this.fallbackFormatProvider);
            }

            #endregion

            #region Public Methods

            /// <summary>
            /// Converts the specified object to an equivalent string representation using the specified format.
            /// </summary>
            /// <param name="objectToFormat">The object to convert to a string.</param>
            /// <param name="formatString">The format of the generated string (i.e. the number of decimal places, whether to use a currency sign, ... etc.).</param>
            /// <returns>The string representation of <paramref name="objectToFormat"/>, formatted as specified by <paramref name="formatString"/>.</returns>
            public string Format( object objectToFormat, string formatString )
            {
                bool success;
                string result;
                try
                {
                    success = this.TryFormat(objectToFormat, formatString, out result);
                }
                catch( Exception exception )
                {
                    if( Debugger.IsAttached )
                    {
                        exception.NotNullReference(); // removes unused variable compiler message
                        Debugger.Break();
                    }

                    success = false;
                    result = null;
                }

                if( success )
                    return result;
                else
                    return this.FallbackFormat(objectToFormat, formatString);
            }

            #endregion
        }

        #endregion

        #region Enumerable

        /// <summary>
        /// Supports <see cref="IEnumerable"/> formatting.
        /// All other types fall back to the specified format provider.
        /// </summary>
        public class Enumerable : Base
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
            /// Tries to convert the specified object to a string, using to the specified format.
            /// </summary>
            /// <param name="objectToFormat">The object to convert to a string.</param>
            /// <param name="formatString">The format of the generated string (i.e. the number of decimal places, whether to use a currency sign, ... etc.).</param>
            /// <param name="formattedObject">If the operation was successful, the <paramref name="objectToFormat"/> parameter converted to a string, in the specified format; otherwise <c>null</c>.</param>
            /// <returns><c>true</c> if the specified object could be formatted; otherwise, <c>false</c>.</returns>
            protected override bool TryFormat( object objectToFormat, string formatString, out string formattedObject )
            {
                if( this.IsEnumerable(objectToFormat) )
                {
                    formattedObject = this.FormatEnumerable((IEnumerable)objectToFormat, formatString);
                    return true;
                }
                else
                {
                    formattedObject = null;
                    return false;
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
                    if( !SafeString.TryParseInt32(enumerableFormat.Substring(startIndex: 1), out maxLength)
                     || maxLength < 0 )
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

            private string FormatEnumerable( IEnumerable enumerable, string format )
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
                        currentItemAsString = this.Format(currentItem, itemFormat);
                    }
                    else
                    {
                        currentItemAsString = this.Format(currentItem, formatString: null);
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
            private static readonly Dictionary<Type, Func<Debug, object, string, string>> Formatters;

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

                Formatters = new Dictionary<Type, Func<Debug, object, string, string>>();
                Formatters.Add(typeof(sbyte), ( self, objectToFormat, formatString ) => self.FallbackFormat(objectToFormat, formatString) + "y");
                Formatters.Add(typeof(byte), ( self, objectToFormat, formatString ) => self.FallbackFormat(objectToFormat, formatString) + "uy");
                Formatters.Add(typeof(short), ( self, objectToFormat, formatString ) => self.FallbackFormat(objectToFormat, formatString) + "s");
                Formatters.Add(typeof(ushort), ( self, objectToFormat, formatString ) => self.FallbackFormat(objectToFormat, formatString) + "us");
                Formatters.Add(typeof(int), ( self, objectToFormat, formatString ) => self.FallbackFormat(objectToFormat, formatString));
                Formatters.Add(typeof(uint), ( self, objectToFormat, formatString ) => self.FallbackFormat(objectToFormat, formatString) + "u");
                Formatters.Add(typeof(long), ( self, objectToFormat, formatString ) => self.FallbackFormat(objectToFormat, formatString) + "L");
                Formatters.Add(typeof(ulong), ( self, objectToFormat, formatString ) => self.FallbackFormat(objectToFormat, formatString) + "UL");
                Formatters.Add(typeof(float), ( self, objectToFormat, formatString ) => self.FallbackFormat(objectToFormat, formatString.NullOrEmpty() ? "R" : formatString) + "f");
                Formatters.Add(typeof(double), ( self, objectToFormat, formatString ) => self.FallbackFormat(objectToFormat, formatString.NullOrEmpty() ? "R" : formatString) + "d");
                Formatters.Add(typeof(decimal), ( self, objectToFormat, formatString ) => self.FallbackFormat(objectToFormat, formatString) + "m");
                Formatters.Add(typeof(bool), ( self, objectToFormat, formatString ) => (bool)objectToFormat ? "true" : "false");
                Formatters.Add(typeof(char), ( self, objectToFormat, formatString ) => ((char)objectToFormat).ToString());
                Formatters.Add(typeof(string), ( self, objectToFormat, formatString ) => (string)objectToFormat);
                Formatters.Add(typeof(DateTime), ( self, objectToFormat, formatString ) => ((DateTime)objectToFormat).ToString("o", CultureInfo.InvariantCulture));
                Formatters.Add(typeof(DateTimeOffset), ( self, objectToFormat, formatString ) => ((DateTimeOffset)objectToFormat).ToString("o", CultureInfo.InvariantCulture));
                Formatters.Add(typeof(TimeSpan), ( self, objectToFormat, formatString ) => ((TimeSpan)objectToFormat).ToString("c", CultureInfo.InvariantCulture));
                Formatters.Add(typeof(ExceptionInfo), ( self, objectToFormat, formatString ) => ((ExceptionInfo)objectToFormat).ToString());
                Formatters.Add(typeof(Exception), ( self, objectToFormat, formatString ) => new ExceptionInfo((Exception)objectToFormat).ToString());
                Formatters.Add(typeof(byte[]), ( self, objectToFormat, formatString ) => Convert.ToBase64String((byte[])objectToFormat));
                Formatters.Add(typeof(Mechanical3.IO.FileSystems.FilePath), ( self, objectToFormat, formatString ) => self.Format(((Mechanical3.IO.FileSystems.FilePath)objectToFormat)?.ToString(), formatString: null));
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
            /// Tries to convert the specified object to a string, using to the specified format.
            /// </summary>
            /// <param name="objectToFormat">The object to convert to a string.</param>
            /// <param name="formatString">The format of the generated string (i.e. the number of decimal places, whether to use a currency sign, ... etc.).</param>
            /// <param name="formattedObject">If the operation was successful, the <paramref name="objectToFormat"/> parameter converted to a string, in the specified format; otherwise <c>null</c>.</param>
            /// <returns><c>true</c> if the specified object could be formatted; otherwise, <c>false</c>.</returns>
            protected override bool TryFormat( object objectToFormat, string formatString, out string formattedObject )
            {
                if( objectToFormat.NullReference() )
                {
                    formattedObject = Null;
                }
                else
                {
                    var type = objectToFormat.GetType();
                    Func<Debug, object, string, string> f;
                    if( Formatters.TryGetValue(type, out f) )
                    {
                        formattedObject = f(this, objectToFormat, formatString);
                    }
                    else if( objectToFormat is Type ) // NOTE: the actual reflection types we get are RuntimeType, RuntimeParameterInfo, ..., which inherit Type, ParameterInfo, ... etc.
                    {
                        var sb = new StringBuilder();
                        this.AppendType(sb, formatString, (Type)objectToFormat);
                        formattedObject = sb.ToString();
                    }
                    else if( objectToFormat is TypeInfo )
                    {
                        var sb = new StringBuilder();
                        this.AppendType(sb, formatString, ((TypeInfo)objectToFormat).AsType());
                        formattedObject = sb.ToString();
                    }
                    else if( objectToFormat is ParameterInfo )
                    {
                        var sb = new StringBuilder();
                        this.AppendParameter(sb, formatString, (ParameterInfo)objectToFormat);
                        formattedObject = sb.ToString();
                    }
                    else if( objectToFormat is MethodBase )
                    {
                        var sb = new StringBuilder();
                        this.AppendMethod(sb, formatString, (MethodBase)objectToFormat);
                        formattedObject = sb.ToString();
                    }
                    else
                    {
                        var typeInfo = type.GetTypeInfo();
                        if( typeInfo.IsGenericType )
                        {
                            var typeDef = type.GetGenericTypeDefinition();
                            if( typeDef == typeof(Nullable<>) )
                            {
                                //// NOTE: I do not know how to unit test this:
                                ////        - ((float?)3.14f).GetType() --> float
                                ////        - (float?)null --> null reference
                                ////       Not sure this code ever runs...

                                var hasValue = (bool)typeInfo.GetDeclaredProperty("HasValue").GetValue(objectToFormat, index: null);
                                if( hasValue )
                                {
                                    object value = typeInfo.GetDeclaredProperty("Value").GetValue(objectToFormat, index: null);
                                    formattedObject = this.Format(value, formatString);
                                }
                                else
                                {
                                    formattedObject = Null;
                                }
                            }
                            else if( typeDef == typeof(KeyValuePair<,>) )
                            {
                                object key = typeInfo.GetDeclaredProperty("Key").GetValue(objectToFormat, index: null);
                                object value = typeInfo.GetDeclaredProperty("Value").GetValue(objectToFormat, index: null);
                                formattedObject = string.Format(
                                    "[{0}, {1}]",
                                    this.Format(key, formatString),
                                    this.Format(value, formatString));
                            }
                            else
                            {
                                // No custom formatting available here, but maybe our base type has something...
                                return base.TryFormat(objectToFormat, formatString, out formattedObject);
                            }
                        }
                        else
                        {
                            // No custom formatting available here, but maybe our base type has something...
                            return base.TryFormat(objectToFormat, formatString, out formattedObject);
                        }
                    }
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
