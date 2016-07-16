using System;
using System.Linq;
using Mechanical3.Misc;
using NUnit.Framework;

namespace Mechanical3.Tests.Misc
{
    [TestFixture(Category = "Misc")]
    public static class StringStateCollectionTests
    {
        internal static bool Equals( StringState s1, StringState s2 )
        {
            return string.Equals(s1.Name, s2.Name, StringComparison.Ordinal)
                && string.Equals(s1.Value, s2.Value, StringComparison.Ordinal)
                && string.Equals(s1.ValueType, s2.ValueType, StringComparison.Ordinal);
        }

        private static bool Equals( StringState[] arr1, StringState[] arr2 )
        {
            if( object.ReferenceEquals(arr1, arr2) )
                return true;

            if( object.ReferenceEquals(arr1, null)
             || object.ReferenceEquals(arr2, null) )
                return false;

            if( arr1.Length != arr2.Length )
                return false;

            for( int i = 0; i < arr1.Length; ++i )
            {
                if( !Equals(arr1[i], arr2[i]) )
                    return false;
            }

            return true;
        }

        [Test]
        public static void BasicStringStateTests()
        {
            var state1 = StringState.From("test1", (object)null);
            var state2 = StringState.From("test2", 5);

            var collection = new StringStateCollection();
            Assert.True(Equals(new StringState[0], collection.ToArray()));

            Assert.Throws<ArgumentException>(() => collection.Add(default(StringState)));

            collection.Add(state1);
            Assert.True(collection.ContainsKey(state1.Name));
            Assert.False(collection.ContainsKey(state2.Name));
            Assert.True(Equals(new StringState[] { state1 }, collection.ToArray()));

            collection.Add(state2);
            Assert.True(collection.ContainsKey(state1.Name));
            Assert.True(collection.ContainsKey(state2.Name));
            Assert.True(Equals(new StringState[] { state1, state2 }, collection.ToArray()));
        }

        [Test]
        public static void KeyConflictTests()
        {
            var state = StringState.From("test", 5);
            var state3 = StringState.From("test", 3.14f);

            var collection = new StringStateCollection();
            collection.Add(state);
            collection.Add(state);
            collection.Add(state3);
            Assert.True(collection.ContainsKey(state.Name));
            var contents = new StringState[]
            {
                state,
                new StringState(state.Name + 2.ToString(), state.Value, state.ValueType),
                new StringState(state.Name + 3.ToString(), state3.Value, state3.ValueType),
            };
            Assert.True(Equals(contents, collection.ToArray()));
        }

        [Test]
        public static void PartialStackTraceTests()
        {
            var state = StringState.From("test", 3.14f);

            var collection = new StringStateCollection();
            Assert.False(collection.HasPartialStackTrace);
            Assert.Null(collection.GetPartialStackTrace());

            // adding random things does not add a partial stack trace
            collection.Add(state);
            Assert.False(collection.HasPartialStackTrace);
            Assert.Null(collection.GetPartialStackTrace());

            var info1 = new FileLineInfo("Test.cs", "Test", 1);
            collection.AddPartialStackTrace(info1);
            Assert.True(collection.HasPartialStackTrace);
            Assert.True(collection.ContainsKey("PartialStackTrace"));
            Assert.True(Equals(new StringState[] { state, new StringState("PartialStackTrace", info1.ToString(), "string") }, collection.ToArray()));
            Test.OrdinalEquals(collection.GetPartialStackTrace(), info1.ToString());

            var info2 = new FileLineInfo("Test2.cs", "Test2", 2);
            collection.AddPartialStackTrace(info2);
            Assert.True(collection.HasPartialStackTrace);
            Assert.True(collection.ContainsKey("PartialStackTrace"));
            Assert.True(Equals(new StringState[] { state, new StringState("PartialStackTrace", info1.ToString() + "\r\n" + info2.ToString(), "string") }, collection.ToArray()));
            Test.OrdinalEquals(collection.GetPartialStackTrace(), info1.ToString() + "\r\n" + info2.ToString());
        }
    }
}
