using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Mechanical3.Core;
using Mechanical3.Events;
using Mechanical3.Misc;
using Mechanical3.MVVM;
using Mechanical3.Tests.Events;
using NUnit.Framework;

namespace Mechanical3.Tests.MVVM
{
    [TestFixture(Category = "UI")]
    public static class UITests
    {
        [Test]
        public static void UIInvokeTests()
        {
            Test.CreateInstanceAndRunInNewAppDomain<InvokeTests>();
        }

        [Test]
        public static void UIAwaiterTests()
        {
            Test.CreateInstanceAndRunInNewAppDomain<CombinedAwaiterTests>();
            Test.CreateInstanceAndRunInNewAppDomain<UIHandlerAwaiterTests>();
        }

        public class InvokeTests : MarshalByRefObject, Test.IAppDomainRunnable
        {
            public void Run()
            {
                TestSynchronizationContext.RunOnNew(() =>
                {
                    var eventPump = new ManualEventPump();
                    MechanicalApp.Initialize(
                        SynchronizationContextUIHandler.FromCurrent(),
                        eventPump);

                    var uiContext = SynchronizationContext.Current;
                    UI.Invoke(() => Assert.AreSame(uiContext, SynchronizationContext.Current));
                    UI.InvokeAsync(() => Assert.AreSame(uiContext, SynchronizationContext.Current)).Wait();
                    Assert.AreEqual(5, UI.Invoke(() => 5));
                    Assert.AreEqual(5, UI.InvokeAsync(() => 5).Result);

                    TestSynchronizationContext.RunOnNew(() =>
                    {
                        Assert.AreNotSame(uiContext, SynchronizationContext.Current);

                        UI.Invoke(() => Assert.AreSame(uiContext, SynchronizationContext.Current));
                        UI.InvokeAsync(() => Assert.AreSame(uiContext, SynchronizationContext.Current)).Wait();
                        Assert.AreEqual(5, UI.Invoke(() => 5));
                        Assert.AreEqual(5, UI.InvokeAsync(() => 5).Result);
                    });

                    eventPump.BeginClose();
                    eventPump.HandleAll();
                });
            }
        }

        public class CombinedAwaiterTests : MarshalByRefObject, Test.IAppDomainRunnable
        {
            public void Run()
            {
                TestSynchronizationContext.RunOnNew(() =>
                {
                    var eventPump = new ManualEventPump();
                    MechanicalApp.Initialize(
                        SynchronizationContextUIHandler.FromCurrent(),
                        eventPump);

                    this.CombinedTests_StartsOnUI().Wait();

                    eventPump.BeginClose();
                    eventPump.HandleAll();
                });
            }

            private async Task CombinedTests_StartsOnUI()
            {
                var uiThread = Thread.CurrentThread;
                var uiContext = SynchronizationContext.Current;

                // if already on the UI thread, just continue
                await UI.CatchAsync();
                Assert.AreEqual(uiThread, Thread.CurrentThread); // NOTE: since the test context does not changes threads, this doesn't really mean anything
                Assert.AreSame(uiContext, SynchronizationContext.Current);

                // switch to ThreadPool thread
                await UI.ReleaseAsync();
                var workThread = Thread.CurrentThread;
                Assert.AreNotEqual(uiThread, workThread);
                Assert.AreNotSame(uiContext, SynchronizationContext.Current);

                // if already on the ThreadPool, just continue
                await UI.ReleaseAsync();
                Assert.AreEqual(workThread, Thread.CurrentThread);
                Assert.AreNotSame(uiContext, SynchronizationContext.Current);

                // some work in between
                Thread.Sleep(ManualEventPumpTests.SmallSleepTime);
                await Task.Run(() =>
                {
                    // this is a third thread
                    Assert.AreNotEqual(uiThread, Thread.CurrentThread);
                    Assert.AreNotEqual(workThread, Thread.CurrentThread);
                });
                Assert.AreNotEqual(uiThread, Thread.CurrentThread);
                Assert.AreNotSame(uiContext, SynchronizationContext.Current);

                // switch to UI thread
                await UI.CatchAsync();
                Assert.AreSame(uiContext, SynchronizationContext.Current); // NOTE: we can not check the thread, since the test context does not actually change it.
            }
        }

        public class UIHandlerAwaiterTests : MarshalByRefObject, Test.IAppDomainRunnable
        {
            //// NOTE: Exceptions from code invoked from the UI sometimes take additional steps to "show up".
            ////       Here we want to make sure, that the caller always receives the exceptions, after swithing to the UI thread.

            #region SilentSynchronizationContextWrapper

            private class SilentSynchronizationContextWrapper : SynchronizationContext
            {
                private readonly SynchronizationContext context;

                internal SilentSynchronizationContextWrapper( SynchronizationContext syncContext )
                {
                    if( syncContext.NullReference() )
                        throw new ArgumentNullException(nameof(syncContext)).StoreFileLine();

                    this.context = syncContext;
                }

                public override void Post( SendOrPostCallback d, object state )
                {
                    try
                    {
                        context.Post(d, state);
                    }
                    catch
                    {
                    }
                }

                public override void Send( SendOrPostCallback d, object state )
                {
                    try
                    {
                        context.Send(d, state);
                    }
                    catch
                    {
                    }
                }
            }

            #endregion

            public void Run()
            {
                TestSynchronizationContext.RunOnNew(() =>
                {
                    var eventPump = new ManualEventPump();
                    MechanicalApp.Initialize(
                        new SynchronizationContextUIHandler(
                            new SilentSynchronizationContextWrapper( // forces IUIThreadHandler to swallow exceptions
                                SynchronizationContext.Current)),
                        eventPump);

                    Action<Task> failIfNoTimeZoneExceptionThrown = task =>
                    {
                        try
                        {
                            task.Wait();
                            Assert.Fail("No exception thrown!");
                        }
                        catch( Exception ex )
                        {
                            var aex = ex as AggregateException;
                            if( aex.NotNullReference() )
                            {
                                var tzex = aex.InnerExceptions.FirstOrDefault() as TimeZoneNotFoundException;
                                if( tzex.NotNullReference() )
                                    return;
                            }
                            Assert.Fail("Unexpected exception type!");
                        }
                    };

                    Task.Run(() => failIfNoTimeZoneExceptionThrown(this.ExceptionTest_NoCatch_StartsOnThreadPool())).Wait();
                    Task.Run(() => failIfNoTimeZoneExceptionThrown(this.ExceptionTest_WithCatch_StartsOnThreadPool())).Wait();

                    eventPump.BeginClose();
                    eventPump.HandleAll();
                });
            }

            private async Task ExceptionTest_NoCatch_StartsOnThreadPool()
            {
                await UI.CatchAsync();
                throw new TimeZoneNotFoundException();
            }

            private async Task ExceptionTest_WithCatch_StartsOnThreadPool()
            {
                // NOTE: This is probably effectively the same as the previous one,
                //       but since I'm not sure how the custom awaiter interacts
                //       with the generated state machine for try-catch blocks,
                //       I'll just leave this here.
                try
                {
                    await UI.CatchAsync();
                    throw new MemberAccessException();
                }
                catch( MemberAccessException )
                {
                    throw new TimeZoneNotFoundException();
                }
            }
        }
    }
}
