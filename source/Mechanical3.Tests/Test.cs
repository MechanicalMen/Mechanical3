using System;
using System.IO;
using System.Linq;
using System.Text;
using Mechanical3.Core;
using Mechanical3.IO.FileSystems;
using NUnit.Framework;

namespace Mechanical3.Tests
{
    public static class Test
    {
        public static void OrdinalEquals( string x, string y )
        {
            Assert.True(string.Equals(x, y, StringComparison.Ordinal));
        }

        public static string ReplaceLineTerminators( string input, string newLine )
        {
            var sb = new StringBuilder();
            using( var reader = new StringReader(input) )
            {
                string line;
                while( (line = reader.ReadLine()).NotNullReference() )
                {
                    if( sb.Length != 0 )
                        sb.Append(newLine);

                    sb.Append(line);
                }
            }
            return sb.ToString();
        }

        public static void AssertAreEqual( string[] expected, string[] actual, StringComparer comparer )
        {
            Assert.AreEqual(expected.Length, actual.Length);

            for( int i = 0; i < expected.Length; ++i )
            {
                Assert.IsTrue(comparer.Equals(expected[i], actual[i]));
            }
        }

        public static void AssertAreEqual( string[] expected, FilePath[] actual, bool sort = true )
        {
            if( sort )
            {
                expected = expected?.OrderBy(str => str, FilePath.Comparer).ToArray();
                actual = actual?.OrderBy(p => p.ToString(), FilePath.Comparer).ToArray();
            }

            AssertAreEqual(expected, actual?.Select(p => p.ToString()).ToArray(), FilePath.Comparer);
        }

        public static void CreateInstanceAndRunInNewAppDomain<T>()
            where T : IAppDomainRunnable, new()
        {
            // NOTE: based on: http://stackoverflow.com/questions/2008691/pass-and-execute-delegate-in-separate-appdomain
            AppDomain childDomain = null;
            try
            {
                // Construct and initialize settings for a second AppDomain.
                AppDomainSetup domainSetup = new AppDomainSetup()
                {
                    ApplicationBase = AppDomain.CurrentDomain.SetupInformation.ApplicationBase,
                    ConfigurationFile = AppDomain.CurrentDomain.SetupInformation.ConfigurationFile,
                    ApplicationName = AppDomain.CurrentDomain.SetupInformation.ApplicationName,
                    LoaderOptimization = LoaderOptimization.MultiDomainHost
                };

                // Create the child AppDomain used for the service tool at runtime.
                childDomain = AppDomain.CreateDomain("Your Child AppDomain", null, domainSetup);

                // Create an instance of the runtime in the second AppDomain. 
                // A proxy to the object is returned.
                IAppDomainRunnable runtime = (IAppDomainRunnable)childDomain.CreateInstanceAndUnwrap(typeof(T).Assembly.FullName, typeof(T).FullName);

                // start the runtime.  call will marshal into the child runtime appdomain
                runtime.Run();
            }
            finally
            {
                // runtime has exited, finish off by unloading the runtime appdomain
                if( childDomain != null )
                    AppDomain.Unload(childDomain);
            }
        }

        public interface IAppDomainRunnable
        {
            void Run(); // NOTE: we should be able to accept parameters, and return a value, as long as they are all serializable
        }
    }
}
