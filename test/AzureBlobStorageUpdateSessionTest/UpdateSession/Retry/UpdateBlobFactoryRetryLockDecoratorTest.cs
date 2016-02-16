using System;
using System.Threading.Tasks;
using Etg.Yams.Azure.UpdateSession;
using Etg.Yams.Azure.UpdateSession.Retry;
using Microsoft.Practices.EnterpriseLibrary.TransientFaultHandling;
using Moq;
using Xunit;

namespace Etg.Yams.Azure.Test.UpdateSession.Retry
{
    public class UpdateBlobFactoryRetryLockDecoratorTest
    {
        [Fact]
        public async Task TestSuccessfullRetry()
        {
            string appId = "appId";
            IUpdateBlob updateBlob = new Mock<IUpdateBlob>().Object;
            var updateBlobFactoryMock = new Mock<IUpdateBlobFactory>();
            updateBlobFactoryMock.Setup(factory => factory.TryLockUpdateBlob(appId))
                .ThrowsAsync(new UpdateBlobUnavailableException())
                .Callback(() => updateBlobFactoryMock.Setup(factory => factory.TryLockUpdateBlob(appId))
                    .ThrowsAsync(new UpdateBlobUnavailableException())
                    .Callback(() => updateBlobFactoryMock.Setup(factory => factory.TryLockUpdateBlob(appId))
                        .ReturnsAsync(updateBlob)));

            UpdateBlobFactoryRetryLockDecorator retryDecorator =
                new UpdateBlobFactoryRetryLockDecorator(updateBlobFactoryMock.Object,
                    new FixedInterval(2, TimeSpan.Zero));
            Assert.Equal(updateBlob, await retryDecorator.TryLockUpdateBlob(appId));
        }

        [Fact]
        public async Task TestThatExceptionIsThrownIfMaxRetryCountIsReached()
        {
            string appId = "appId";
            IUpdateBlob updateBlob = new Mock<IUpdateBlob>().Object;
            var updateBlobFactoryMock = new Mock<IUpdateBlobFactory>();
            updateBlobFactoryMock.Setup(factory => factory.TryLockUpdateBlob(appId))
                .ThrowsAsync(new UpdateBlobUnavailableException())
                .Callback(() => updateBlobFactoryMock.Setup(factory => factory.TryLockUpdateBlob(appId))
                    .ThrowsAsync(new UpdateBlobUnavailableException())
                    .Callback(() => updateBlobFactoryMock.Setup(factory => factory.TryLockUpdateBlob(appId))
                        .ReturnsAsync(updateBlob)));

            UpdateBlobFactoryRetryLockDecorator retryDecorator =
                new UpdateBlobFactoryRetryLockDecorator(updateBlobFactoryMock.Object,
                    new FixedInterval(1, TimeSpan.Zero));
            await
                Assert.ThrowsAsync<UpdateBlobUnavailableException>(
                    async () => await retryDecorator.TryLockUpdateBlob(appId));
        }

        [Fact]
        public async Task TestThatNotAllExceptionsAreRetried()
        {
            string appId = "appId";
            var updateBlobFactoryMock = new Mock<IUpdateBlobFactory>();
            updateBlobFactoryMock.Setup(factory => factory.TryLockUpdateBlob(appId))
                .ThrowsAsync(new InvalidOperationException());

            UpdateBlobFactoryRetryLockDecorator retryDecorator =
                new UpdateBlobFactoryRetryLockDecorator(updateBlobFactoryMock.Object,
                    new FixedInterval(1, TimeSpan.Zero));
            await
                Assert.ThrowsAsync<InvalidOperationException>(async () => await retryDecorator.TryLockUpdateBlob(appId));
        }
    }
}