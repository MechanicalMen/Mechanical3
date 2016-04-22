using System.Threading;
using Mechanical3.Misc;
using NUnit.Framework;

namespace Mechanical3.Tests.Misc
{
    [TestFixture(Category = "Misc")]
    public static class SynchronizationContextUIHandlerTests
    {
        [Test]
        public static void SyncContextUIHandlerTest()
        {
            TestSynchronizationContext.RunOnNew(() =>
            {
                var uiContext = SynchronizationContext.Current;
                var uiHandler = SynchronizationContextUIHandler.FromCurrent();
                Assert.True(uiHandler.IsOnUIThread());

                TestSynchronizationContext.RunOnNew(() =>
                {
                    Assert.False(uiHandler.IsOnUIThread());
                    Assert.AreNotSame(uiContext, SynchronizationContext.Current);

                    uiHandler.Invoke(() => Assert.AreSame(uiContext, SynchronizationContext.Current));
                    uiHandler.BeginInvoke(() => Assert.AreSame(uiContext, SynchronizationContext.Current));
                });
            });

            //// NOTE: invoking from the ui context is undefined: should we deadlock, Post, or what? Depends on the implementation.
        }
    }
}
