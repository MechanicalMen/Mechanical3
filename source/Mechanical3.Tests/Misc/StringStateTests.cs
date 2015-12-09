﻿using System;
using Mechanical3.Misc;
using NUnit.Framework;

namespace Mechanical3.Tests.Misc
{
    public static class StringStateTests
    {
        [Test]
        public static void ConstructorTests()
        {
            // bad constructor parameters
            Assert.Throws<ArgumentException>(() => new StringState(name: null, value: "y", valueType: "Z"));
            Assert.Throws<ArgumentException>(() => new StringState(name: string.Empty, value: "y", valueType: "z"));
            Assert.Throws<ArgumentException>(() => new StringState(name: " x", value: "y", valueType: "z"));
            Assert.Throws<ArgumentException>(() => new StringState(name: "x ", value: "y", valueType: "z"));

            Assert.Throws<ArgumentNullException>(() => new StringState(name: "x", value: null, valueType: "z"));

            Assert.Throws<ArgumentException>(() => new StringState(name: "x", value: "y", valueType: string.Empty));
            Assert.Throws<ArgumentException>(() => new StringState(name: "x", value: "y", valueType: " z"));
            Assert.Throws<ArgumentException>(() => new StringState(name: "x", value: "y", valueType: "z "));

            // valid parameters
            var state = new StringState(name: "x", value: "y", valueType: "z");
            Test.OrdinalEquals("x", state.Name);
            Test.OrdinalEquals("y", state.Value);
            Test.OrdinalEquals("z", state.ValueType);

            state = StringState.From("test2", 3.14);
            Test.OrdinalEquals("test2", state.Name);
            Test.OrdinalEquals("3.14d", state.Value);
            Test.OrdinalEquals("double", state.ValueType);

            state = StringState.From("test3", (NUnit.Framework.TestAttribute)null);
            Test.OrdinalEquals("test3", state.Name);
            Test.OrdinalEquals("null", state.Value);
            Test.OrdinalEquals("NUnit.Framework.TestAttribute (null)", state.ValueType);
        }
    }
}
