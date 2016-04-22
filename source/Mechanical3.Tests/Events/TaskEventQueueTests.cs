using Mechanical3.Events;
using NUnit.Framework;

namespace Mechanical3.Tests.Events
{
    [TestFixture(Category = "Events")]
    public static class TaskEventQueueTests
    {
        [Test]
        public static void TaskEventQueueTest()
        {
            var queue = new TaskEventQueue();
            var recorder = new ManualEventPumpTests.EventRecorder<ManualEventPumpTests.TestEvent<int>>();
            queue.Subscribe(recorder);

            Assert.Null(recorder.LastEvent);
            var evnt = new ManualEventPumpTests.TestEvent<int>();
            queue.Enqueue(evnt);
            System.Threading.Thread.Sleep(ManualEventPumpTests.SmallSleepTime);
            Assert.True(object.ReferenceEquals(evnt, recorder.LastEvent));

            queue.BeginClose();
            System.Threading.Thread.Sleep(ManualEventPumpTests.SmallSleepTime);
        }
    }
}
