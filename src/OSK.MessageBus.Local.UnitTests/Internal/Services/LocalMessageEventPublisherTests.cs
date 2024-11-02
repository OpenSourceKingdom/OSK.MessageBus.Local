using Microsoft.Extensions.Options;
using Moq;
using OSK.MessageBus.Abstractions;
using OSK.MessageBus.Local.Internal.Services;
using OSK.MessageBus.Local.Models;
using OSK.MessageBus.Local.Options;
using OSK.MessageBus.Local.Ports;
using OSK.MessageBus.Local.UnitTests.Helpers;
using Xunit;

namespace OSK.MessageBus.Local.UnitTests.Internal.Services
{
    public class LocalMessageEventPublisherTests
    {
        #region Variables

        private LocalMessageEventPublisher _publisher;

        #endregion

        #region Constructors

        public LocalMessageEventPublisherTests()
        {
            _publisher = new LocalMessageEventPublisher();
        }

        #endregion

        #region RegisterReceiver

        [Fact]
        public void RegisterReceiver_NullReceiver_ThrowsArgumentNullException()
        {
            // Arrange/Act/Assert
            Assert.Throws<ArgumentNullException>(() => _publisher.RegisterReceiver(null));
        }

        [Fact]
        public void RegisterReceiver_ValidReceiver_AddsReceiverToInternalLookup()
        {
            // Arrange
            var topicId = Guid.NewGuid().ToString();
            var mockReceiver = new Mock<ILocalMessageEventReceiver>();
            mockReceiver.SetupGet(m => m.TopicId)
                .Returns(topicId);

            // Act
            _publisher.RegisterReceiver(mockReceiver.Object);

            // Assert
            Assert.True(_publisher._receivers.TryGetValue(topicId, out var receiverSet));
            Assert.Single(receiverSet);

            var actualReceiver = receiverSet.First();
            Assert.Equal(actualReceiver, mockReceiver.Object);
        }

        #endregion

        #region UnregisterReceiver

        [Fact]
        public void UnregisterReceiver_NullReceiver_ThrowsArgumentNullException()
        {
            // Arrange/Act/Assert
            Assert.Throws<ArgumentNullException>(() => _publisher.UnregisterReceiver(null));
        }

        [Fact]
        public void UnregisterReceiver_ReceiversTopicIdNotInPublisherLookup_ReturnsSuccessfully()
        {
            // Arrange
            var topicId = Guid.NewGuid().ToString();
            var mockReceiver = new Mock<ILocalMessageEventReceiver>();
            mockReceiver.SetupGet(m => m.TopicId)
                .Returns(topicId);

            // Act/Assert
            _publisher.UnregisterReceiver(mockReceiver.Object);
        }

        [Fact]
        public void UnregisterReceiver_ReceiversInLookup_ReturnsSuccessfullyWithRecieverRemoved()
        {
            // Arrange
            var topicIdA = Guid.NewGuid().ToString();
            var mockReceiverA = new Mock<ILocalMessageEventReceiver>();
            mockReceiverA.SetupGet(m => m.TopicId)
                .Returns(topicIdA);

            var mockReceiverB = new Mock<ILocalMessageEventReceiver>();
            mockReceiverB.SetupGet(m => m.TopicId)
                .Returns(topicIdA);

            var topicIdB = Guid.NewGuid().ToString();
            var mockReceiverC = new Mock<ILocalMessageEventReceiver>();
            mockReceiverC.SetupGet(m => m.TopicId)
                .Returns(topicIdB);

            _publisher.RegisterReceiver(mockReceiverA.Object);
            _publisher.RegisterReceiver(mockReceiverB.Object);
            _publisher.RegisterReceiver(mockReceiverC.Object);

            // Act
            _publisher.UnregisterReceiver(mockReceiverB.Object);

            // Assert
            Assert.True(_publisher._receivers.TryGetValue(topicIdA, out var receiverSetA));
            Assert.True(_publisher._receivers.TryGetValue(topicIdB, out var receiverSetB));
            Assert.Single(receiverSetA);
            Assert.Single(receiverSetB);

            var actualReceiver = receiverSetA.First();
            Assert.Equal(actualReceiver, mockReceiverA.Object);

            actualReceiver = receiverSetB.First();
            Assert.Equal(actualReceiver, mockReceiverC.Object);
        }

        #endregion

        #region PublishAsync

        [Fact]
        public async Task PublishAsync_NullMessage_ThrowsArgumentNullException()
        {
            // Arrange/Act/Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => _publisher.PublishAsync((TestEvent)null, new MessagePublishOptions()));
        }

        [Fact]
        public async Task PublishAsync_NullOptions_ThrowsArgumentNullException()
        {
            // Arrange/Act/Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => _publisher.PublishAsync(new TestEvent(), null));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(2)]
        public async Task PublishAsync_Valid_StoresLocalMessage(int delayTimeInSeconds)
        {
            // Arrange
            var message = new TestEvent()
            {
                TopicId = "Abc"
            };
            var delayTimeSpan = TimeSpan.FromSeconds(delayTimeInSeconds);

            // Act
            await _publisher.PublishAsync(message, new MessagePublishOptions()
            {
                DelayTimeSpan = delayTimeSpan
            });

            // Assert
            var messages = _publisher.GetPublishedMessages().Where(m => m.MessageEvent.TopicId == message.TopicId);
            if (delayTimeInSeconds > 0)
            {
                Assert.Empty(messages);
                await Task.Delay(delayTimeSpan);

                messages = _publisher.GetPublishedMessages().Where(m => m.MessageEvent.TopicId == message.TopicId);
            }

            Assert.Single(messages);
            var outputMessage = messages.First();

            Assert.Equal(message, outputMessage.MessageEvent);
            if (delayTimeInSeconds == 0)
            {
                Assert.Null(outputMessage.TriggerTime);
            }
            else
            {
                Assert.NotNull(outputMessage.TriggerTime);
            }
        }

        #endregion/ 

        #region SendPublishedMessagesAsync

        [Fact]
        private async Task SendPublishedMessagesAsync_NoMessagesToPublish_NoReceiversCalled_ReturnsSuccessfully()
        {
            // Arrange
            var topicA = Guid.NewGuid().ToString();
            var mockReceiverA = new Mock<ILocalMessageEventReceiver>();
            mockReceiverA.SetupGet(m => m.TopicId)
                .Returns(topicA);

            _publisher.RegisterReceiver(mockReceiverA.Object);

            await _publisher.PublishAsync(new TestEvent()
            {
                TopicId = Guid.NewGuid().ToString()
            }, new MessagePublishOptions()
            {
                DelayTimeSpan = TimeSpan.FromMinutes(10)
            });

            // Act
            await _publisher.SendPublishedMessagesAsync(new SendPublishedMessagesOptions()
            {
                RemoveMessagesFropTopicsWithoutReceivers = false
            });

            // Assert
            var messages = _publisher.GetPublishedMessages();
            Assert.Empty(messages);

            mockReceiverA.Verify(m => m.ReceiveMessageAsync(It.IsAny<LocalMessage>()), Times.Never);
        }

        [Fact]
        private async Task SendPublishedMessagesAsync_MultipleMessagesToPublish_DoesNotRemoveUnusedEvents_ExpectedReceiversCalled_ReturnsSuccessfully()
        {
            // Arrange
            var topicA = Guid.NewGuid().ToString();
            var mockReceiverA = new Mock<ILocalMessageEventReceiver>();
            mockReceiverA.SetupGet(m => m.TopicId)
                .Returns(topicA);

            var mockReceiverB = new Mock<ILocalMessageEventReceiver>();
            mockReceiverB.SetupGet(m => m.TopicId)
                .Returns(topicA);

            var topicB = Guid.NewGuid().ToString();
            var mockReceiverC = new Mock<ILocalMessageEventReceiver>();
            mockReceiverC.SetupGet(m => m.TopicId)
                .Returns(topicB);

            var mockReceiverD = new Mock<ILocalMessageEventReceiver>();
            mockReceiverD.SetupGet(m => m.TopicId)
                .Returns(Guid.NewGuid().ToString);

            _publisher.RegisterReceiver(mockReceiverA.Object);
            _publisher.RegisterReceiver(mockReceiverB.Object);
            _publisher.RegisterReceiver(mockReceiverC.Object);
            _publisher.RegisterReceiver(mockReceiverD.Object);

            await _publisher.PublishAsync(new TestEvent()
            {
                TopicId = topicA,
            }, new MessagePublishOptions()
            {
                DelayTimeSpan = TimeSpan.FromMinutes(10)
            });
            await _publisher.PublishAsync(new TestEvent()
            {
                TopicId = topicA,
            }, new MessagePublishOptions());

            await _publisher.PublishAsync(new TestEvent()
            {
                TopicId = topicB,
            }, new MessagePublishOptions());

            var topicC = Guid.NewGuid().ToString();
            await _publisher.PublishAsync(new TestEvent()
            {
                TopicId = topicC,
            }, new MessagePublishOptions());
            
            Assert.Equal(3, _publisher.GetPublishedMessages().Count());

            // Act
            await _publisher.SendPublishedMessagesAsync(new SendPublishedMessagesOptions()
            {
                RemoveMessagesFropTopicsWithoutReceivers = false
            });

            // Assert
            var messagesToPublish = _publisher.GetPublishedMessages().ToList();
            Assert.Single(messagesToPublish);

            var actualMessages = _publisher._messageEvents.SelectMany(topics => topics.Value.Values).ToList();
            Assert.Equal(2, actualMessages.Count);
            Assert.Single(actualMessages, message => message.MessageEvent.TopicId == topicA);
            Assert.Single(actualMessages, message => message.MessageEvent.TopicId == topicC);

            mockReceiverA.Verify(m => m.ReceiveMessageAsync(It.IsAny<LocalMessage>()), Times.Once);
            mockReceiverB.Verify(m => m.ReceiveMessageAsync(It.IsAny<LocalMessage>()), Times.Once);
            mockReceiverC.Verify(m => m.ReceiveMessageAsync(It.IsAny<LocalMessage>()), Times.Once);
            mockReceiverD.Verify(m => m.ReceiveMessageAsync(It.IsAny<LocalMessage>()), Times.Never);
        }

        [Fact]
        private async Task SendPublishedMessagesAsync_MultipleMessagesToPublish_DoesRemoveUnusedEvents_ExpectedReceiversCalled_ReturnsSuccessfully()
        {
            // Arrange
            var topicA = Guid.NewGuid().ToString();
            var mockReceiverA = new Mock<ILocalMessageEventReceiver>();
            mockReceiverA.SetupGet(m => m.TopicId)
                .Returns(topicA);

            var mockReceiverB = new Mock<ILocalMessageEventReceiver>();
            mockReceiverB.SetupGet(m => m.TopicId)
                .Returns(topicA);

            var topicB = Guid.NewGuid().ToString();
            var mockReceiverC = new Mock<ILocalMessageEventReceiver>();
            mockReceiverC.SetupGet(m => m.TopicId)
                .Returns(topicB);

            var mockReceiverD = new Mock<ILocalMessageEventReceiver>();
            mockReceiverD.SetupGet(m => m.TopicId)
                .Returns(Guid.NewGuid().ToString);

            _publisher.RegisterReceiver(mockReceiverA.Object);
            _publisher.RegisterReceiver(mockReceiverB.Object);
            _publisher.RegisterReceiver(mockReceiverC.Object);
            _publisher.RegisterReceiver(mockReceiverD.Object);

            await _publisher.PublishAsync(new TestEvent()
            {
                TopicId = topicA,
            }, new MessagePublishOptions()
            {
                DelayTimeSpan = TimeSpan.FromMinutes(10)
            });
            await _publisher.PublishAsync(new TestEvent()
            {
                TopicId = topicA,
            }, new MessagePublishOptions());

            await _publisher.PublishAsync(new TestEvent()
            {
                TopicId = topicB,
            }, new MessagePublishOptions());

            var topicC = Guid.NewGuid().ToString();
            await _publisher.PublishAsync(new TestEvent()
            {
                TopicId = topicC,
            }, new MessagePublishOptions());

            Assert.Equal(3, _publisher.GetPublishedMessages().Count());

            // Act
            await _publisher.SendPublishedMessagesAsync(new SendPublishedMessagesOptions()
            {
                RemoveMessagesFropTopicsWithoutReceivers = true
            });

            // Assert
            var messagesToPublish = _publisher.GetPublishedMessages().ToList();
            Assert.Empty(messagesToPublish);

            var actualMessages = _publisher._messageEvents.SelectMany(topics => topics.Value.Values).ToList();
            Assert.Single(actualMessages);
            Assert.Single(actualMessages, message => message.MessageEvent.TopicId == topicA);
            
            mockReceiverA.Verify(m => m.ReceiveMessageAsync(It.IsAny<LocalMessage>()), Times.Once);
            mockReceiverB.Verify(m => m.ReceiveMessageAsync(It.IsAny<LocalMessage>()), Times.Once);
            mockReceiverC.Verify(m => m.ReceiveMessageAsync(It.IsAny<LocalMessage>()), Times.Once);
            mockReceiverD.Verify(m => m.ReceiveMessageAsync(It.IsAny<LocalMessage>()), Times.Never);
        }


        #endregion
    }
}
