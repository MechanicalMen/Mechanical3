using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Mechanical3.Core;
using Mechanical3.Misc;
using NUnit.Framework;

namespace Mechanical3.Tests.Misc
{
    [TestFixture(Category = "Misc")]
    public static class StringFormatterTests
    {
        #region Test helpers

        private static string ToString( ICustomFormatter formatter, object obj, string format = null )
        {
            return formatter.Format(format, obj, formatProvider: null); // formatProvider is ignored by our implementation
        }

        public class Nested1
        {
            public Nested1()
            {
            }

            public Nested1( int i )
            {
            }

            internal struct Nested2<T>
            {
                internal interface INested3
                {
                }
            }
        }

        public static void ParamTestMethod( int p0, out float p1, ref byte p2, params object[] p3 )
        {
            p1 = default(float);
        }

        public static void GenericTestMethod<T1, T2>()
        {
        }

        #endregion

        #region TestFallbackFormatter

        private class TestFallbackFormatter : StringFormatter.Base
        {
            internal TestFallbackFormatter( IFormatProvider fallbackProvider )
                : base(fallbackProvider)
            {
            }

            protected override bool TryFormat( object objectToFormat, string formatString, out string formattedObject )
            {
                if( objectToFormat is float )
                {
                    if( (float)objectToFormat >= 0 )
                    {
                        formattedObject = "x";
                        return true;
                    }
                    else
                    {
                        formattedObject = null;
                        return false;
                    }
                }
                else if( objectToFormat is int )
                {
                    throw new Exception("Failed to format argument!");
                }
                else
                {
                    formattedObject = null;
                    return false;
                }
            }
        }

        #endregion

        [Test]
        public static void FallbackFormatterTests()
        {
            var culture = CultureInfo.InvariantCulture;
            var formatter = new TestFallbackFormatter(culture);

            // GetFormat
            Assert.True(object.ReferenceEquals(formatter, ((IFormatProvider)formatter).GetFormat(typeof(ICustomFormatter))));
            Assert.False(object.ReferenceEquals(formatter, ((IFormatProvider)formatter).GetFormat(typeof(NumberFormatInfo))));
            Assert.True(object.ReferenceEquals(culture.GetFormat(typeof(NumberFormatInfo)), ((IFormatProvider)formatter).GetFormat(typeof(NumberFormatInfo))));

            // Format
            Test.OrdinalEquals("x", formatter.Format(3.14f, "G")); // TestFallbackFormatter
            Test.OrdinalEquals("-3.14", formatter.Format(-3.14f, "G")); // InvariantCulture (TryFormat returned false)
            Test.OrdinalEquals("5", formatter.Format(5, "G")); // InvariantCulture (TryFormat threw an exception)

            Assert.Throws<ArgumentNullException>(() => new TestFallbackFormatter(null));
        }

        [Test]
        public static void EnumerableFormatterTests()
        {
            ICustomFormatter formatter = new StringFormatter.Enumerable(CultureInfo.InvariantCulture);

            // null enumeration
            Test.OrdinalEquals(string.Empty, ToString(formatter, null, "G"));
            Test.OrdinalEquals(string.Empty, ToString(formatter, null, "L64"));
            Test.OrdinalEquals(string.Empty, ToString(formatter, null, "L0"));

            // empty enumeration
            Test.OrdinalEquals("{}", ToString(formatter, new int[0], "G"));
            Test.OrdinalEquals("{}", ToString(formatter, new int[0], "L3"));
            Test.OrdinalEquals("{}", ToString(formatter, new int[0], "L2"));
            Test.OrdinalEquals(string.Empty, ToString(formatter, new int[0], "L1"));
            Test.OrdinalEquals(string.Empty, ToString(formatter, new int[0], "L0"));

            // simple enumerations
            var simpleArray = new int[] { 0, 1, 2 };
            Test.OrdinalEquals("{0, 1, 2}", ToString(formatter, simpleArray, "G"));
            Test.OrdinalEquals("{0, 1, 2}", ToString(formatter, simpleArray, "L14"));
            Test.OrdinalEquals("{0, 1, ...}", ToString(formatter, simpleArray, "L13"));
            Test.OrdinalEquals("{0, 1, ...}", ToString(formatter, simpleArray, "L11"));
            Test.OrdinalEquals("{0, ...}", ToString(formatter, simpleArray, "L10"));
            Test.OrdinalEquals("{0, ...}", ToString(formatter, simpleArray, "L8"));
            Test.OrdinalEquals("{...}", ToString(formatter, simpleArray, "L7"));
            Test.OrdinalEquals("{...}", ToString(formatter, simpleArray, "L5"));
            Test.OrdinalEquals(string.Empty, ToString(formatter, simpleArray, "L4"));

            // large enumerations
            var largeArray = Enumerable.Range(start: 1, count: 100).ToArray();
            Test.OrdinalEquals(
                "{1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37, 38, 39, 40, 41, 42, 43, 44, 45, 46, 47, 48, 49, 50, 51, 52, 53, 54, 55, 56, 57, 58, 59, 60, 61, 62, 63, 64, 65, ...}",
                ToString(formatter, largeArray, "G"));
            Test.OrdinalEquals(
                "{1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37, 38, 39, 40, 41, 42, 43, 44, 45, 46, 47, 48, 49, 50, 51, 52, 53, 54, 55, 56, 57, 58, 59, 60, 61, 62, 63, 64, 65, 66, 67, 68, 69, 70, 71, 72, 73, 74, 75, 76, 77, 78, 79, 80, 81, 82, 83, 84, 85, 86, 87, 88, 89, 90, 91, 92, 93, 94, 95, 96, 97, 98, 99, 100}",
                ToString(formatter, largeArray, "L400"));

            // invalid format replaced by "G"
            Test.OrdinalEquals(ToString(formatter, largeArray, "G"), ToString(formatter, largeArray, "L-10"));
            Test.OrdinalEquals(ToString(formatter, largeArray, "G"), ToString(formatter, largeArray, "L"));
            Test.OrdinalEquals(ToString(formatter, largeArray, "G"), ToString(formatter, largeArray, "X"));

            // arrays of arrays
            var complexArray = new object[]
            {
                new object[]
                {
                    1,
                    new object[] { 2, 3, 4 },
                    5
                },
                new object[] { },
                new object[] { 6, 7 },
                8
            };
            Test.OrdinalEquals("{{1, {2, 3, 4}, 5}, {}, {6, 7}, 8}", ToString(formatter, complexArray, "G"));
            Test.OrdinalEquals("{{1, {2, ...}, 5}, {}, {6, 7}, 8}", ToString(formatter, complexArray, "L99;L99;L10"));
            Test.OrdinalEquals("{{1, , 5}, {}, {6, 7}, 8}", ToString(formatter, complexArray, "L99;L99;L0"));
            Test.OrdinalEquals("{{1, , ...}, {}, {6, ...}, 8}", ToString(formatter, complexArray, "L99;L10;L0"));
            Test.OrdinalEquals("{, {}, , 8}", ToString(formatter, complexArray, "L99;L2"));
            Test.OrdinalEquals("{, , , 8}", ToString(formatter, complexArray, "L99;L0"));
        }

        [Test]
        public static void DebugFormatterTests()
        {
            ICustomFormatter formatter = StringFormatter.Debug.Default;

            // literals
            Test.OrdinalEquals("null", ToString(formatter, null));
            Test.OrdinalEquals("true", ToString(formatter, true));
            Test.OrdinalEquals("false", ToString(formatter, false));
            Test.OrdinalEquals("8y", ToString(formatter, (sbyte)8));
            Test.OrdinalEquals("8uy", ToString(formatter, (byte)8));
            Test.OrdinalEquals("8s", ToString(formatter, (short)8));
            Test.OrdinalEquals("8us", ToString(formatter, (ushort)8));
            Test.OrdinalEquals("8", ToString(formatter, (int)8));
            Test.OrdinalEquals("8u", ToString(formatter, 8u));
            Test.OrdinalEquals("8L", ToString(formatter, 8L));
            Test.OrdinalEquals("8UL", ToString(formatter, 8UL));
            Test.OrdinalEquals("8f", ToString(formatter, 8f));
            Test.OrdinalEquals("8d", ToString(formatter, 8d));
            Test.OrdinalEquals("8m", ToString(formatter, 8m));
            Test.OrdinalEquals("a", ToString(formatter, 'a'));
            Test.OrdinalEquals(@"a", ToString(formatter, "a"));
            Test.OrdinalEquals(@"a""'b", ToString(formatter, @"a""'b"));

            // DateTime, DateTimeOffset, TimeSpan
            var now = DateTime.Now;
            Test.OrdinalEquals(now.ToString("o"), ToString(formatter, now));
            now = new DateTime(now.Ticks, DateTimeKind.Utc);
            Test.OrdinalEquals(now.ToString("o"), ToString(formatter, now));
            now = new DateTime(now.Ticks, DateTimeKind.Unspecified);
            Test.OrdinalEquals(now.ToString("o"), ToString(formatter, now));
            var now2 = new DateTimeOffset(now.Ticks, TimeSpan.FromHours(1));
            Test.OrdinalEquals(now2.ToString("o"), ToString(formatter, now2));
            now2 = new DateTimeOffset(now.Ticks, TimeSpan.Zero);
            Test.OrdinalEquals(now2.ToString("o"), ToString(formatter, now2));

            // built in types
            Test.OrdinalEquals("byte", ToString(formatter, typeof(byte)));
            Test.OrdinalEquals("sbyte", ToString(formatter, typeof(sbyte)));
            Test.OrdinalEquals("short", ToString(formatter, typeof(short)));
            Test.OrdinalEquals("ushort", ToString(formatter, typeof(ushort)));
            Test.OrdinalEquals("int", ToString(formatter, typeof(int)));
            Test.OrdinalEquals("uint", ToString(formatter, typeof(uint)));
            Test.OrdinalEquals("long", ToString(formatter, typeof(long)));
            Test.OrdinalEquals("ulong", ToString(formatter, typeof(ulong)));
            Test.OrdinalEquals("float", ToString(formatter, typeof(float)));
            Test.OrdinalEquals("double", ToString(formatter, typeof(double)));
            Test.OrdinalEquals("decimal", ToString(formatter, typeof(decimal)));
            Test.OrdinalEquals("char", ToString(formatter, typeof(char)));
            Test.OrdinalEquals("string", ToString(formatter, typeof(string)));
            Test.OrdinalEquals("bool", ToString(formatter, typeof(bool)));
            Test.OrdinalEquals("object", ToString(formatter, typeof(object)));
            Test.OrdinalEquals("void", ToString(formatter, typeof(void)));

            // generics
            Test.OrdinalEquals("Exception", ToString(formatter, typeof(Exception)));
            Test.OrdinalEquals("Tuple<Type, Exception>", ToString(formatter, typeof(Tuple<Type, Exception>)));
            Test.OrdinalEquals("Action<T1, T2>", ToString(formatter, typeof(Action<,>)));
            Test.OrdinalEquals("KeyValuePair<int, TValue>", ToString(formatter, typeof(KeyValuePair<,>).MakeGenericType(typeof(int), typeof(KeyValuePair<,>).GetGenericArguments()[1])));
            Test.OrdinalEquals("IEnumerable", ToString(formatter, typeof(System.Collections.IEnumerable)));
            Test.OrdinalEquals("IEnumerable<object>", ToString(formatter, typeof(IEnumerable<object>)));
            Test.OrdinalEquals("IEnumerator<T>", ToString(formatter, typeof(IEnumerator<>)));
            Test.OrdinalEquals("Action<KeyValuePair<IEnumerable<float>, Tuple<int, char, string>>, IEnumerator<Tuple<decimal>>>", ToString(formatter, typeof(Action<KeyValuePair<IEnumerable<float>, Tuple<int, char, string>>, IEnumerator<Tuple<decimal>>>)));

            // full namespace, nested types (& nested generics)
            Test.OrdinalEquals("Mechanical3.Tests.Misc.StringFormatterTests", ToString(formatter, typeof(StringFormatterTests)));
            Test.OrdinalEquals("Mechanical3.Tests.Misc.StringFormatterTests.Nested1.Nested2<T>.INested3<T>", ToString(formatter, typeof(StringFormatterTests.Nested1.Nested2<>.INested3)));
            Test.OrdinalEquals("Mechanical3.Tests.Misc.StringFormatterTests.Nested1.Nested2<T>.INested3<int>", ToString(formatter, typeof(StringFormatterTests.Nested1.Nested2<int>.INested3)));

            // array types
            Test.OrdinalEquals("int[]", ToString(formatter, typeof(int[])));
            Test.OrdinalEquals("int[,,]", ToString(formatter, typeof(int[,,])));
            Test.OrdinalEquals("IEnumerable<int>[,,,][][,,]", ToString(formatter, typeof(IEnumerable<int>[,,,][][,,])));

            // parameters
            Test.OrdinalEquals("int p0", ToString(formatter, typeof(StringFormatterTests).GetMethod("ParamTestMethod").GetParameters()[0]));
            Test.OrdinalEquals("out float p1", ToString(formatter, typeof(StringFormatterTests).GetMethod("ParamTestMethod").GetParameters()[1]));
            Test.OrdinalEquals("ref byte p2", ToString(formatter, typeof(StringFormatterTests).GetMethod("ParamTestMethod").GetParameters()[2]));
            Test.OrdinalEquals("params object[] p3", ToString(formatter, typeof(StringFormatterTests).GetMethod("ParamTestMethod").GetParameters()[3]));

            // methods, constructors
            Test.OrdinalEquals("void Mechanical3.Tests.Misc.StringFormatterTests.ParamTestMethod(int p0, out float p1, ref byte p2, params object[] p3)", ToString(formatter, typeof(StringFormatterTests).GetMethod("ParamTestMethod")));
            var genericTestMethod_GenericDefinition = typeof(StringFormatterTests).GetMethod("GenericTestMethod").GetGenericMethodDefinition();
            Test.OrdinalEquals("void Mechanical3.Tests.Misc.StringFormatterTests.GenericTestMethod<T1, T2>()", ToString(formatter, genericTestMethod_GenericDefinition));
            Test.OrdinalEquals("void Mechanical3.Tests.Misc.StringFormatterTests.GenericTestMethod<int, float>()", ToString(formatter, genericTestMethod_GenericDefinition.MakeGenericMethod(typeof(int), typeof(float))));
            Test.OrdinalEquals("void Mechanical3.Tests.Misc.StringFormatterTests.GenericTestMethod<int, T2>()", ToString(formatter, genericTestMethod_GenericDefinition.MakeGenericMethod(typeof(int), genericTestMethod_GenericDefinition.GetGenericArguments()[1])));
            Test.OrdinalEquals("Mechanical3.Tests.Misc.StringFormatterTests.Nested1.ctor()", ToString(formatter, typeof(StringFormatterTests.Nested1).GetConstructors().Where(ctor => ctor.GetParameters().Length == 0).First()));
            Test.OrdinalEquals("Mechanical3.Tests.Misc.StringFormatterTests.Nested1.ctor(int i)", ToString(formatter, typeof(StringFormatterTests.Nested1).GetConstructors().Where(ctor => ctor.GetParameters().Length == 1).First()));

            // Exception, ExceptionInfo
            var exception = new Exception("test message").Store("testValue", 3); // see ExceptionInfo tests for further details
            string exceptionString = @"Type: Exception
Message: test message
Data:
  testValue = 3
PartialStackTrace:
  at DebugFormatterTests in StringFormatterTests.cs:line 261"; // Exception.ToString has no "Data" part
            Test.OrdinalEquals(exceptionString, ToString(formatter, exception));
            Test.OrdinalEquals(exceptionString, ToString(formatter, new ExceptionInfo(exception)));

            // KeyValuePair
            Test.OrdinalEquals("[3.14f, null]", ToString(formatter, new KeyValuePair<float, string>(3.14f, null)));

            // byte[]
            Test.OrdinalEquals("AAECAwQFBgcICQ==", ToString(formatter, new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 }));

            // FilePath
            Test.OrdinalEquals("test/", ToString(formatter, Mechanical3.IO.FileSystems.FilePath.FromDirectoryName("test")));
        }
    }
}
