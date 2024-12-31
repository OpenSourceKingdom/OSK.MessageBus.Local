using Moq;
using OSK.MessageBus.Local.Internal.Services;
using OSK.MessageBus.Local.Models;
using OSK.MessageBus.Local.Options;
using OSK.MessageBus.Local.Ports;
using OSK.MessageBus.Local.UnitTests.Helpers;
using OSK.Transmissions.Abstractions;
using Xunit;

namespace OSK.MessageBus.Local.UnitTests.Internal.Services
{
    public class LocalMessageTransmitterTests
    {
        #region Variables

        private readonly LocalMessageTransmitter _transmitter;

        #endregion

        #region Constructors

        public LocalMessageTransmitterTests()
        {
            _transmitter = new LocalMessageTransmitter();
        }

        #endregion

        #region RegisterReceiver

        [Fact]
        public void RegisterReceiver_NullReceiver_ThrowsArgumentNullException()
        {
            // Arrange/Act/Assert
            Assert.Throws<ArgumentNullException>(() => _transmitter.RegisterReceiver(null));
        }

        [Fact]
        public void RegisterReceiver_ValidReceiver_AddsReceiverToInternalLookup()
        {
            // Arrange
            var topicId = Guid.NewGuid().ToString();
            var mockReceiver = new Mock<ILocalMessageReceiver>();
            mockReceiver.SetupGet(m => m.TopicId)
                .Returns(topicId);

            // Act
            _transmitter.RegisterReceiver(mockReceiver.Object);

            // Assert
            Assert.True(_transmitter._receivers.TryGetValue(topicId, out var receiverSet));
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
            Assert.Throws<ArgumentNullException>(() => _transmitter.UnregisterReceiver(null));
        }

        [Fact]
        public void UnregisterReceiver_ReceiversTopicIdNotInPublisherLookup_ReturnsSuccessfully()
        {
            // Arrange
            var topicId = Guid.NewGuid().ToString();
            var mockReceiver = new Mock<ILocalMessageReceiver>();
            mockReceiver.SetupGet(m => m.TopicId)
                .Returns(topicId);

            // Act/Assert
            _transmitter.UnregisterReceiver(mockReceiver.Object);
        }

        [Fact]
        public void UnregisterReceiver_ReceiversInLookup_ReturnsSuccessfullyWithRecieverRemoved()
        {
            // Arrange
            var topicIdA = Guid.NewGuid().ToString();
            var mockReceiverA = new Mock<ILocalMessageReceiver>();
            mockReceiverA.SetupGet(m => m.TopicId)
                .Returns(topicIdA);

            var mockReceiverB = new Mock<ILocalMessageReceiver>();
            mockReceiverB.SetupGet(m => m.TopicId)
                .Returns(topicIdA);

            var topicIdB = Guid.NewGuid().ToString();
            var mockReceiverC = new Mock<ILocalMessageReceiver>();
            mockReceiverC.SetupGet(m => m.TopicId)
                .Returns(topicIdB);

            _transmitter.RegisterReceiver(mockReceiverA.Object);
            _transmitter.RegisterReceiver(mockReceiverB.Object);
            _transmitter.RegisterReceiver(mockReceiverC.Object);

            // Act
            _transmitter.UnregisterReceiver(mockReceiverB.Object);

            // Assert
            Assert.True(_transmitter._receivers.TryGetValue(topicIdA, out var receiverSetA));
            Assert.True(_transmitter._receivers.TryGetValue(topicIdB, out var receiverSetB));
            Assert.Single(receiverSetA);
            Assert.Single(receiverSetB);

            var actualReceiver = receiverSetA.First();
            Assert.Equal(actualReceiver, mockReceiverA.Object);

            actualReceiver = receiverSetB.First();
            Assert.Equal(actualReceiver, mockReceiverC.Object);
        }

        #endregion

        #region TransmitAsync

        [Fact]
        public async Task TransmitAsync_NullMessage_ThrowsArgumentNullException()
        {
            // Arrange/Act/Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => _transmitter.TransmitAsync((TestMessage)null, new MessageTransmissionOptions()));
        }

        [Fact]
        public async Task TransmitAsync_NullOptions_ThrowsArgumentNullException()
        {
            // Arrange/Act/Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => _transmitter.TransmitAsync(new TestMessage(), null));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(2)]
        public async Task TransmitAsync_Valid_StoresLocalMessage(int delayTimeInSeconds)
        {
            // Arrange
            var message = new TestMessage()
            {
                TopicId = "Abc"
            };
            var delayTimeSpan = TimeSpan.FromSeconds(delayTimeInSeconds);

            // Act
            await _transmitter.TransmitAsync(message, new MessageTransmissionOptions()
            {
                DelayTimeSpan = delayTimeSpan
            });

            // Assert
            var messages = _transmitter.GetTransmittedMessages().Where(m => m.MessageEvent.TopicId == message.TopicId);
            if (delayTimeInSeconds > 0)
            {
                Assert.Empty(messages);
                await Task.Delay(delayTimeSpan);

                messages = _transmitter.GetTransmittedMessages().Where(m => m.MessageEvent.TopicId == message.TopicId);
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

        #endregion

        #region SendPublishedMessagesAsync

        [Fact]
        public async Task SendPublishedMessagesAsync_NoMessagesToPublish_NoReceiversCalled_ReturnsSuccessfully()
        {
            // Arrange
            var topicA = Guid.NewGuid().ToString();
            var mockReceiverA = new Mock<ILocalMessageReceiver>();
            mockReceiverA.SetupGet(m => m.TopicId)
                .Returns(topicA);

            _transmitter.RegisterReceiver(mockReceiverA.Object);

            await _transmitter.TransmitAsync(new TestMessage()
            {
                TopicId = Guid.NewGuid().ToString()
            }, new MessageTransmissionOptions()
            {
                DelayTimeSpan = TimeSpan.FromMinutes(10)
            });

            // Act
            await _transmitter.SendTransmittedMessagesAsync(new SendTransmittedMessagesOptions()
            {
                RemoveMessagesFropTopicsWithoutReceivers = false
            });

            // Assert
            var messages = _transmitter.GetTransmittedMessages();
            Assert.Empty(messages);

            mockReceiverA.Verify(m => m.ReceiveMessageAsync(It.IsAny<LocalMessage>()), Times.Never);
        }

        [Fact]
        public async Task SendPublishedMessagesAsync_MultipleMessagesToPublish_DoesNotRemoveUnusedEvents_ExpectedReceiversCalled_ReturnsSuccessfully()
        {
            // Arrange
            var topicA = Guid.NewGuid().ToString();
            var mockReceiverA = new Mock<ILocalMessageReceiver>();
            mockReceiverA.SetupGet(m => m.TopicId)
                .Returns(topicA);

            var mockReceiverB = new Mock<ILocalMessageReceiver>();
            mockReceiverB.SetupGet(m => m.TopicId)
                .Returns(topicA);

            var topicB = Guid.NewGuid().ToString();
            var mockReceiverC = new Mock<ILocalMessageReceiver>();
            mockReceiverC.SetupGet(m => m.TopicId)
                .Returns(topicB);

            var mockReceiverD = new Mock<ILocalMessageReceiver>();
            mockReceiverD.SetupGet(m => m.TopicId)
                .Returns(Guid.NewGuid().ToString);

            _transmitter.RegisterReceiver(mockReceiverA.Object);
            _transmitter.RegisterReceiver(mockReceiverB.Object);
            _transmitter.RegisterReceiver(mockReceiverC.Object);
            _transmitter.RegisterReceiver(mockReceiverD.Object);

            await _transmitter.TransmitAsync(new TestMessage()
            {
                TopicId = topicA,
            }, new MessageTransmissionOptions()
            {
                DelayTimeSpan = TimeSpan.FromMinutes(10)
            });
            await _transmitter.TransmitAsync(new TestMessage()
            {
                TopicId = topicA,
            }, new MessageTransmissionOptions());

            await _transmitter.TransmitAsync(new TestMessage()
            {
                TopicId = topicB,
            }, new MessageTransmissionOptions());

            var topicC = Guid.NewGuid().ToString();
            await _transmitter.TransmitAsync(new TestMessage()
            {
                TopicId = topicC,
            }, new MessageTransmissionOptions());
            
            Assert.Equal(3, _transmitter.GetTransmittedMessages().Count());

            // Act
            await _transmitter.SendTransmittedMessagesAsync(new SendTransmittedMessagesOptions()
            {
                RemoveMessagesFropTopicsWithoutReceivers = false
            });

            // Assert
            var messagesToPublish = _transmitter.GetTransmittedMessages().ToList();
            Assert.Single(messagesToPublish);

            var actualMessages = _transmitter._messageEvents.SelectMany(topics => topics.Value.Values).ToList();
            Assert.Equal(2, actualMessages.Count);
            Assert.Single(actualMessages, message => message.MessageEvent.TopicId == topicA);
            Assert.Single(actualMessages, message => message.MessageEvent.TopicId == topicC);

            mockReceiverA.Verify(m => m.ReceiveMessageAsync(It.IsAny<LocalMessage>()), Times.Once);
            mockReceiverB.Verify(m => m.ReceiveMessageAsync(It.IsAny<LocalMessage>()), Times.Once);
            mockReceiverC.Verify(m => m.ReceiveMessageAsync(It.IsAny<LocalMessage>()), Times.Once);
            mockReceiverD.Verify(m => m.ReceiveMessageAsync(It.IsAny<LocalMessage>()), Times.Never);
        }

        [Fact]
        public async Task SendPublishedMessagesAsync_MultipleMessagesToPublish_DoesRemoveUnusedEvents_ExpectedReceiversCalled_ReturnsSuccessfully()
        {
            // Arrange
            var topicA = Guid.NewGuid().ToString();
            var mockReceiverA = new Mock<ILocalMessageReceiver>();
            mockReceiverA.SetupGet(m => m.TopicId)
                .Returns(topicA);

            var mockReceiverB = new Mock<ILocalMessageReceiver>();
            mockReceiverB.SetupGet(m => m.TopicId)
                .Returns(topicA);

            var topicB = Guid.NewGuid().ToString();
            var mockReceiverC = new Mock<ILocalMessageReceiver>();
            mockReceiverC.SetupGet(m => m.TopicId)
                .Returns(topicB);

            var mockReceiverD = new Mock<ILocalMessageReceiver>();
            mockReceiverD.SetupGet(m => m.TopicId)
                .Returns(Guid.NewGuid().ToString);

            _transmitter.RegisterReceiver(mockReceiverA.Object);
            _transmitter.RegisterReceiver(mockReceiverB.Object);
            _transmitter.RegisterReceiver(mockReceiverC.Object);
            _transmitter.RegisterReceiver(mockReceiverD.Object);

            await _transmitter.TransmitAsync(new TestMessage()
            {
                TopicId = topicA,
            }, new MessageTransmissionOptions()
            {
                DelayTimeSpan = TimeSpan.FromMinutes(10)
            });
            await _transmitter.TransmitAsync(new TestMessage()
            {
                TopicId = topicA,
            }, new MessageTransmissionOptions());

            await _transmitter.TransmitAsync(new TestMessage()
            {
                TopicId = topicB,
            }, new MessageTransmissionOptions());

            var topicC = Guid.NewGuid().ToString();
            await _transmitter.TransmitAsync(new TestMessage()
            {
                TopicId = topicC,
            }, new MessageTransmissionOptions());

            Assert.Equal(3, _transmitter.GetTransmittedMessages().Count());

            // Act
            await _transmitter.SendTransmittedMessagesAsync(new SendTransmittedMessagesOptions()
            {
                RemoveMessagesFropTopicsWithoutReceivers = true
            });

            // Assert
            var messagesToPublish = _transmitter.GetTransmittedMessages().ToList();
            Assert.Empty(messagesToPublish);

            var actualMessages = _transmitter._messageEvents.SelectMany(topics => topics.Value.Values).ToList();
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
