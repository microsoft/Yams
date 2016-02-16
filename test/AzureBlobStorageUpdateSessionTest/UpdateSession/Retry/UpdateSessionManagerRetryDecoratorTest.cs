using System;
using System.Threading.Tasks;
using Etg.Yams.Azure.UpdateSession.Retry;
using Etg.Yams.Update;
using Microsoft.Practices.EnterpriseLibrary.TransientFaultHandling;
using Microsoft.WindowsAzure.Storage;
using Moq;
using Xunit;

namespace Etg.Yams.Azure.Test.UpdateSession.Retry
{
    public class UpdateSessionManagerRetryDecoratorTest
    {
        [Fact]
        public async Task TestThatStartUpdateSessionIsRetried()
        {
            string appId = "appId";
            var updateSessionMock = new Mock<IUpdateSessionManager>();
            updateSessionMock.Setup(manager => manager.TryStartUpdateSession(appId))
                .ThrowsAsync(new StorageException())
                .Callback(() => updateSessionMock.Setup(manager => manager.TryStartUpdateSession(appId))
                .ReturnsAsync(true));

            IUpdateSessionManager retryDecorator = new UpdateSessionManagerRetryDecorator(
                updateSessionMock.Object,
                new FixedInterval(1, TimeSpan.Zero),
                new StorageExceptionErrorDetectionStrategy());
            Assert.True(await retryDecorator.TryStartUpdateSession(appId));
        }

        [Fact]
        public async Task TestThatEndUpdateSessionIsRetried()
        {
            string appId = "appId";
            var updateSessionMock = new Mock<IUpdateSessionManager>();
            updateSessionMock.SetupSequence(manager => manager.EndUpdateSession(appId))
                .Throws(new StorageException())
                .Returns(Task.CompletedTask);

            IUpdateSessionManager retryDecorator = new UpdateSessionManagerRetryDecorator(
                updateSessionMock.Object,
                new FixedInterval(1, TimeSpan.Zero),
                new StorageExceptionErrorDetectionStrategy());
            await retryDecorator.EndUpdateSession(appId);
        }

        [Fact]
        public async Task TestThatExceptionIsThrownIfMaxRetryCountIsReached()
        {
            string appId = "appId";
            var updateSessionMock = new Mock<IUpdateSessionManager>();
            updateSessionMock.Setup(manager => manager.TryStartUpdateSession(appId))
                .ThrowsAsync(new StorageException())
                .Callback(() => updateSessionMock.Setup(manager => manager.TryStartUpdateSession(appId))
                .ThrowsAsync(new StorageException())
                .Callback(() => updateSessionMock.Setup(manager => manager.TryStartUpdateSession(appId))
                .ReturnsAsync(true)));

            IUpdateSessionManager retryDecorator = new UpdateSessionManagerRetryDecorator(
                updateSessionMock.Object,
                new FixedInterval(1, TimeSpan.Zero),
                new StorageExceptionErrorDetectionStrategy());
            await Assert.ThrowsAsync<StorageException>(async () => await retryDecorator.TryStartUpdateSession(appId));
        }

        [Fact]
        public async Task TestThatNotAllExceptionsAreRetried()
        {
            string appId = "appId";
            var updateSessionMock = new Mock<IUpdateSessionManager>();
            updateSessionMock.Setup(manager => manager.TryStartUpdateSession(appId))
                .ThrowsAsync(new InvalidOperationException());

            IUpdateSessionManager retryDecorator = new UpdateSessionManagerRetryDecorator(
                updateSessionMock.Object,
                new FixedInterval(1, TimeSpan.Zero),
                new StorageExceptionErrorDetectionStrategy());
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await retryDecorator.TryStartUpdateSession(appId));
        }
    }
}