using System;
using System.Threading;
using Moq;
using Xunit;

namespace Microsoft.AspNet.SignalR.Client.Infrastructure
{
    public class TaskQueueMonitorFacts
    {
        [Fact]
        public void ErrorsAreTriggeredForLongRunningTasks()
        {
            var connection = new Mock<IConnection>();
            var monitor = new TaskQueueMonitor(connection.Object, Timeout.InfiniteTimeSpan);

            monitor.TaskStarted();

            monitor.Beat();
            monitor.Beat();

            connection.Verify(c => c.OnError(It.IsAny<Exception>()), Times.Once());
        }

        [Fact]
        public void ErrorsAreNotTriggeredMultipleTimesForTheSameTask()
        {
            var connection = new Mock<IConnection>();
            var monitor = new TaskQueueMonitor(connection.Object, Timeout.InfiniteTimeSpan);


            monitor.TaskStarted();

            monitor.Beat();
            monitor.Beat();
            monitor.Beat();

            connection.Verify(c => c.OnError(It.IsAny<Exception>()), Times.Once());
        }

        [Fact]
        public void NoErrorsAreTriggeredBeforeATaskStarts()
        {
            var connection = new Mock<IConnection>();
            var monitor = new TaskQueueMonitor(connection.Object, Timeout.InfiniteTimeSpan);

            monitor.Beat();
            monitor.Beat();

            connection.Verify(c => c.OnError(It.IsAny<Exception>()), Times.Never());
        }
    }
}
