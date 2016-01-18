using System;
using System.Globalization;
using System.IO;
using Mechanical3.Core;
using NUnit.Framework;

namespace Mechanical3.Tests.Core
{
    [TestFixture(Category = "Core")]
    public static class SafeStringTests
    {
        #region NonFormattableTestObject

        private static class NonFormattableTestObject
        {
            internal class Normal
            {
                public override string ToString()
                {
                    return "non formattable ToString()";
                }
            }

            internal class Null
            {
                public override string ToString()
                {
                    return null;
                }
            }

            internal class Exception
            {
                public override string ToString()
                {
                    throw new FileNotFoundException();
                }
            }
        }

        #endregion

        #region FormattableTestObject

        private static class FormattableTestObject
        {
            internal class Normal : IFormattable
            {
                public string ToString( string format, IFormatProvider formatProvider )
                {
                    return $"IFormattable ToString(\"{format}\", ...)";
                }

                public override string ToString()
                {
                    return "formattable ToString()";
                }
            }

            internal class Null : IFormattable
            {
                public string ToString( string format, IFormatProvider formatProvider )
                {
                    return null;
                }

                public override string ToString()
                {
                    return null;
                }
            }

            internal class Exception : IFormattable
            {
                public string ToString( string format, IFormatProvider formatProvider )
                {
                    throw new FileNotFoundException();
                }

                public override string ToString()
                {
                    throw new FileNotFoundException();
                }
            }
        }

        #endregion

        #region FormatProvider

        private static class FormatProvider
        {
            #region CustomFormatter

            internal static class CustomFormatter
            {
                internal class Normal : ICustomFormatter
                {
                    internal static readonly Normal Default = new Normal();

                    public string Format( string format, object arg, IFormatProvider formatProvider )
                    {
                        return $"custom formatter ({format})";
                    }
                }

                internal class Null : ICustomFormatter
                {
                    internal static readonly Null Default = new Null();

                    public string Format( string format, object arg, IFormatProvider formatProvider )
                    {
                        return null;
                    }
                }

                internal class Exception : ICustomFormatter
                {
                    internal static readonly Exception Default = new Exception();

                    public string Format( string format, object arg, IFormatProvider formatProvider )
                    {
                        throw new FileNotFoundException();
                    }
                }
            }

            #endregion

            internal class Normal : IFormatProvider
            {
                internal static readonly Normal Default = new Normal();

                public object GetFormat( Type formatType )
                {
                    if( formatType == typeof(ICustomFormatter) )
                        return CustomFormatter.Normal.Default;
                    else
                        return CultureInfo.InvariantCulture;
                }
            }

            internal class NullFormatter : IFormatProvider
            {
                internal static readonly NullFormatter Default = new NullFormatter();

                public object GetFormat( Type formatType )
                {
                    if( formatType == typeof(ICustomFormatter) )
                        return CustomFormatter.Null.Default;
                    else
                        return CultureInfo.InvariantCulture;
                }
            }

            internal class ExceptionFormatter : IFormatProvider
            {
                internal static readonly ExceptionFormatter Default = new ExceptionFormatter();

                public object GetFormat( Type formatType )
                {
                    if( formatType == typeof(ICustomFormatter) )
                        return CustomFormatter.Exception.Default;
                    else
                        return CultureInfo.InvariantCulture;
                }
            }

            internal class NullProvider : IFormatProvider
            {
                internal static readonly NullProvider Default = new NullProvider();

                public object GetFormat( Type formatType )
                {
                    return null;
                }
            }

            internal class ExceptionProvider : IFormatProvider
            {
                internal static readonly ExceptionProvider Default = new ExceptionProvider();

                public object GetFormat( Type formatType )
                {
                    throw new FileNotFoundException();
                }
            }
        }

        #endregion

        #region Print (successfull)

        private static void TestTryPrintSuccess( string expectedResult, object obj, string format, IFormatProvider formatProvider )
        {
            string actualResult;
            Assert.True(SafeString.TryPrint(obj, format, formatProvider, out actualResult));
            Test.OrdinalEquals(expectedResult, actualResult);

            if( format.NullOrEmpty() )
                format = "{0}";
            else
                format = "{0:" + format + "}";
            actualResult = string.Format(formatProvider, format, obj);
            Test.OrdinalEquals(expectedResult, actualResult);
        }

        [Test]
        public static void SuccessfullTryPrintTests()
        {
            // null
            TestTryPrintSuccess(string.Empty, null, format: null, formatProvider: null);
            TestTryPrintSuccess(string.Empty, null, format: "X", formatProvider: null);
            TestTryPrintSuccess("custom formatter ()", null, format: null, formatProvider: FormatProvider.Normal.Default);
            TestTryPrintSuccess("custom formatter (X)", null, format: "X", formatProvider: FormatProvider.Normal.Default);

            // non formattable
            TestTryPrintSuccess("non formattable ToString()", new NonFormattableTestObject.Normal(), format: null, formatProvider: null);
            TestTryPrintSuccess("non formattable ToString()", new NonFormattableTestObject.Normal(), format: "X", formatProvider: null);
            TestTryPrintSuccess("non formattable ToString()", new NonFormattableTestObject.Normal(), format: null, formatProvider: FormatProvider.NullFormatter.Default);
            TestTryPrintSuccess("non formattable ToString()", new NonFormattableTestObject.Normal(), format: "X", formatProvider: FormatProvider.NullFormatter.Default);
            TestTryPrintSuccess("non formattable ToString()", new NonFormattableTestObject.Normal(), format: null, formatProvider: FormatProvider.NullProvider.Default);
            TestTryPrintSuccess("non formattable ToString()", new NonFormattableTestObject.Normal(), format: "X", formatProvider: FormatProvider.NullProvider.Default);
            TestTryPrintSuccess(string.Empty, new NonFormattableTestObject.Null(), format: null, formatProvider: null);
            TestTryPrintSuccess(string.Empty, new NonFormattableTestObject.Null(), format: "X", formatProvider: null);

            // formattable
            TestTryPrintSuccess("IFormattable ToString(\"\", ...)", new FormattableTestObject.Normal(), format: null, formatProvider: null);
            TestTryPrintSuccess("IFormattable ToString(\"X\", ...)", new FormattableTestObject.Normal(), format: "X", formatProvider: null);
            TestTryPrintSuccess("IFormattable ToString(\"\", ...)", new FormattableTestObject.Normal(), format: null, formatProvider: FormatProvider.NullFormatter.Default);
            TestTryPrintSuccess("IFormattable ToString(\"X\", ...)", new FormattableTestObject.Normal(), format: "X", formatProvider: FormatProvider.NullFormatter.Default);
            TestTryPrintSuccess("IFormattable ToString(\"\", ...)", new FormattableTestObject.Normal(), format: null, formatProvider: FormatProvider.NullProvider.Default);
            TestTryPrintSuccess("IFormattable ToString(\"X\", ...)", new FormattableTestObject.Normal(), format: "X", formatProvider: FormatProvider.NullProvider.Default);
            TestTryPrintSuccess(string.Empty, new FormattableTestObject.Null(), format: null, formatProvider: null);
            TestTryPrintSuccess(string.Empty, new FormattableTestObject.Null(), format: "X", formatProvider: null);

            // custom formatter
            TestTryPrintSuccess("custom formatter ()", new NonFormattableTestObject.Normal(), format: null, formatProvider: FormatProvider.Normal.Default);
            TestTryPrintSuccess("custom formatter (X)", new NonFormattableTestObject.Normal(), format: "X", formatProvider: FormatProvider.Normal.Default);
            TestTryPrintSuccess("custom formatter ()", new FormattableTestObject.Normal(), format: null, formatProvider: FormatProvider.Normal.Default);
            TestTryPrintSuccess("custom formatter (X)", new FormattableTestObject.Normal(), format: "X", formatProvider: FormatProvider.Normal.Default);
            TestTryPrintSuccess("custom formatter ()", null, format: null, formatProvider: FormatProvider.Normal.Default);
        }

        #endregion

        #region Print (error recovery)

        private static void TestTryPrintFailure( string expectedResult, object obj, IFormatProvider formatProvider, string format = null, Type exceptionType = null )
        {
            string actualResult;
            Assert.False(SafeString.TryPrint(obj, format, formatProvider, out actualResult));
            Test.OrdinalEquals(expectedResult, actualResult);

            if( format.NullOrEmpty() )
                format = "{0}";
            else
                format = "{0:" + format + "}";
            if( exceptionType.NullReference() )
                exceptionType = typeof(FileNotFoundException);
            Assert.Throws(exceptionType, () => string.Format(formatProvider, format, obj));
        }

        [Test]
        public static void FailingTryPrintTests()
        {
            // bad non formattable
            TestTryPrintFailure(string.Empty, new NonFormattableTestObject.Exception(), formatProvider: null);

            // bad formattable
            TestTryPrintFailure(string.Empty, new FormattableTestObject.Exception(), formatProvider: null);

            // bad custom provider
            TestTryPrintFailure("non formattable ToString()", new NonFormattableTestObject.Normal(), format: null, formatProvider: FormatProvider.ExceptionFormatter.Default);
            TestTryPrintFailure("IFormattable ToString(\"X\", ...)", new FormattableTestObject.Normal(), format: "X", formatProvider: FormatProvider.ExceptionFormatter.Default);

            // bad format provider
            TestTryPrintFailure("non formattable ToString()", new NonFormattableTestObject.Normal(), format: null, formatProvider: FormatProvider.ExceptionProvider.Default);
            TestTryPrintFailure("IFormattable ToString(\"X\", ...)", new FormattableTestObject.Normal(), format: "X", formatProvider: FormatProvider.ExceptionProvider.Default);
        }

        #endregion

        #region Format (successfull)

        private static void TestTryFormatSuccess( string expectedResult, IFormatProvider formatProvider, string format, params object[] args )
        {
            string actualResult;
            Assert.True(SafeString.TryFormat(out actualResult, formatProvider, format, args));
            Test.OrdinalEquals(expectedResult, actualResult);

            actualResult = string.Format(formatProvider, format, args);
            Test.OrdinalEquals(expectedResult, actualResult);
        }

        [Test]
        public static void SuccessfullTryFormatTests()
        {
            // null arguments
            TestTryFormatSuccess(string.Empty, null, "{0}", new object[] { null });
            TestTryFormatSuccess(string.Empty, CultureInfo.InvariantCulture, "{0}", new object[] { null });

            // no special characters
            TestTryFormatSuccess("asd", CultureInfo.InvariantCulture, "asd");
            TestTryFormatSuccess("asd", CultureInfo.InvariantCulture, "asd", 5);
            TestTryFormatSuccess(string.Empty, CultureInfo.InvariantCulture, string.Empty, 5);

            // escaped brackets
            TestTryFormatSuccess("{", CultureInfo.InvariantCulture, "{{");
            TestTryFormatSuccess("a}b", CultureInfo.InvariantCulture, "a}}b");

            // alignment
            TestTryFormatSuccess("  x", CultureInfo.InvariantCulture, "{0,3}", 'x');
            TestTryFormatSuccess("ax  b", CultureInfo.InvariantCulture, "a{0,-3}b", 'x');
            TestTryFormatSuccess("yxxy", CultureInfo.InvariantCulture, "y{0,1}y", "xx"); // optional padding

            // format
            TestTryFormatSuccess("11", CultureInfo.InvariantCulture, "{0:x}", 17);
            TestTryFormatSuccess(" 11", CultureInfo.InvariantCulture, "{0,3:x}", 17);
            TestTryFormatSuccess("17", CultureInfo.InvariantCulture, "{0:}", 17); // empty format string = empty string of literals
        }

        #endregion

        #region Format (error recovery)

        private static void TestTryFormatFailure<T>( string expectedResult, IFormatProvider formatProvider, string format, params object[] args )
            where T : Exception
        {
            TestTryFormatFailure(expectedResult, typeof(T), formatProvider, format, args);
        }

        private static void TestTryFormatFailure( string expectedResult, IFormatProvider formatProvider, string format, params object[] args )
        {
            Type exceptionType = null;
            TestTryFormatFailure(expectedResult, exceptionType, formatProvider, format, args);
        }

        private static void TestTryFormatFailure( string expectedResult, Type exceptionType, IFormatProvider formatProvider, string format, params object[] args )
        {
            string actualResult;
            Assert.False(SafeString.TryFormat(out actualResult, formatProvider, format, args));
            Test.OrdinalEquals(expectedResult, actualResult);

            if( exceptionType.NullReference() )
                exceptionType = typeof(FormatException);
            Assert.Throws(exceptionType, () => string.Format(formatProvider, format, args));
        }

        [Test]
        public static void FailingTryFormatTests()
        {
            // invalid arguments
            TestTryFormatFailure<ArgumentNullException>("X", CultureInfo.InvariantCulture, "X", null);
            TestTryFormatFailure<ArgumentNullException>("{0}", CultureInfo.InvariantCulture, "{0}", null);
            TestTryFormatFailure<ArgumentNullException>(string.Empty, CultureInfo.InvariantCulture, null, 5);

            // missing curly bracket
            TestTryFormatFailure("x{yz", CultureInfo.InvariantCulture, "x{yz");
            TestTryFormatFailure("ab}c", CultureInfo.InvariantCulture, "ab}c");

            // invalid index
            TestTryFormatFailure("{}", CultureInfo.InvariantCulture, "{}", 5);
            TestTryFormatFailure("{a}", CultureInfo.InvariantCulture, "{a}", 5);
            TestTryFormatFailure("{-1}", CultureInfo.InvariantCulture, "{-1}", 5);
            TestTryFormatFailure("a{100}b", CultureInfo.InvariantCulture, "a{100}b", 5);

            // invalid alignment
            TestTryFormatFailure("5", CultureInfo.InvariantCulture, "{0,}", 5);
            TestTryFormatFailure("5", CultureInfo.InvariantCulture, "{0,a}", 5);
        }

        #endregion
    }
}
