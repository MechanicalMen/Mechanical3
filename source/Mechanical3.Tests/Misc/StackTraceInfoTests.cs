using Mechanical3.Misc;
using NUnit.Framework;

namespace Mechanical3.Tests.Misc
{
    [TestFixture(Category = "Misc")]
    public static class StackTraceInfoTests
    {
        private static void AssertFileLineEquals( FileLineInfo info, string file, string member, int? line )
        {
            Test.OrdinalEquals(file, info.File);
            Test.OrdinalEquals(member, info.Member);
            Assert.AreEqual(line.HasValue, info.Line.HasValue);
            if( line.HasValue )
                Assert.AreEqual(line.Value, info.Line.Value);
        }

        [Test]
        public static void StackTraceTests()
        {
            // From array
            var st = StackTraceInfo.From(new FileLineInfo(null, "a", 0), new FileLineInfo(null, "b", 0));
            Assert.AreEqual(2, st.Frames.Length);
            Test.OrdinalEquals("a", st.Frames[0].Member);
            Test.OrdinalEquals("b", st.Frames[1].Member);

            // From string
            // "  at <member>"
            // "  at <member> in <file>:line <line>"
            st = StackTraceInfo.From(
@"   at System.Net.HttpListener.GetContext()
   at Mechanical.WebServer.Listener.GetContexts() in Listener.cs:line 91");
            Assert.AreEqual(2, st.Frames.Length);
            AssertFileLineEquals(st.Frames[0], null, "System.Net.HttpListener.GetContext()", null);
            AssertFileLineEquals(st.Frames[1], "Listener.cs", "Mechanical.WebServer.Listener.GetContexts()", 91);

            // "  at <member> in <file>:<line>"
            // "  at <member>"
            st = StackTraceInfo.From(
@"  at System.Net.NetworkInformation.Ping.Send (System.String hostNameOrAddress, Int32 timeout) [0x00000] in &lt;filename unknown&gt;:0 
  at (wrapper remoting-invoke-with-check) System.Net.NetworkInformation.Ping:Send (string,int)");
            Assert.AreEqual(2, st.Frames.Length);
            AssertFileLineEquals(st.Frames[0], "&lt;filename unknown&gt;", "System.Net.NetworkInformation.Ping.Send (System.String hostNameOrAddress, Int32 timeout) [0x00000]", 0);
            AssertFileLineEquals(st.Frames[1], null, "(wrapper remoting-invoke-with-check) System.Net.NetworkInformation.Ping:Send (string,int)", null);

            // ToString
            st = StackTraceInfo.From(new FileLineInfo("a", "b", 1), new FileLineInfo(null, "b", null));
            Test.OrdinalEquals("   at b in a:line 1\r\n   at b", st.ToString());
        }
    }
}
