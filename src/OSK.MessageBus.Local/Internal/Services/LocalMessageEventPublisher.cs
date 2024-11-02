using OSK.MessageBus.Abstractions;
using OSK.MessageBus.Events.Abstractions;
using OSK.MessageBus.Local.Models;
using OSK.MessageBus.Local.Options;
using OSK.MessageBus.Local.Ports;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OSK.MessageBus.Local.Internal.Services
{
    internal class LocalMessageEventPublisher
        : ILocalMessageEventPublisher
    {
        #region Variables

        internal readonly Dictionary<string, HashSet<ILocalMessageEventReceiver>> _receivers = new();
        internal readonly ConcurrentDictionary<string, ConcurrentDictionary<Guid, LocalMessage>> _messageEvents = new();

        #endregion

        #region ILocalMessageEventPublisher

        public Task PublishAsync<TMessage>(TMessage message, MessagePublishOptions options,
            CancellationToken cancellationToken = default)
            where TMessage : IMessageEvent
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }
            if (string.IsNullOrWhiteSpace(message.TopicId))
            {
                throw new ArgumentNullException(nameof(message.TopicId));
            }
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            var topicMessages = _messageEvents.GetOrAdd(message.TopicId, new ConcurrentDictionary<Guid, LocalMessage>());
            topicMessages.AddOrUpdate(Guid.NewGuid(), 
                addValueFactory: messageId => new LocalMessage(message)
                {
                    Id = messageId,
                    TriggerTime = options.DelayTimeSpan > TimeSpan.Zero
                     ? DateTime.Now.Add(options.DelayTimeSpan)
                     : null
                },
                // Don't update already published messages, if somehow Guid confliction occurs
                updateValueFactory: (messageId, currentValue) => currentValue);

            return Task.CompletedTask;
        }

        public async Task SendPublishedMessagesAsync(SendPublishedMessagesOptions options, CancellationToken cancellationToken = default)
        {
            var publishedTopicMessages = GetPublishedMessages();

            foreach (var message in publishedTopicMessages)
            {
                if (!_receivers.TryGetValue(message.MessageEvent.TopicId, out var receiverSet))
                {
                    if (options.RemoveMessagesFropTopicsWithoutReceivers)
                    {
                        _messageEvents.Remove(message.MessageEvent.TopicId, out _);
                    }
                    continue;
                }

                foreach (var receiver in receiverSet)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        break;
                    }
                    await receiver.ReceiveMessageAsync(message);
                }

                _messageEvents[message.MessageEvent.TopicId].Remove(message.Id, out _);
                if (_messageEvents[message.MessageEvent.TopicId].Count == 0)
                {
                    _messageEvents.Remove(message.MessageEvent.TopicId, out _);
                }
            }
        }

        public void RegisterReceiver(ILocalMessageEventReceiver receiver)
        {
            if (receiver == null)
            {
                throw new ArgumentNullException(nameof(receiver));
            }
            if (!_receivers.TryGetValue(receiver.TopicId, out var receiverSet))
            {
                receiverSet = new HashSet<ILocalMessageEventReceiver>();
                _receivers.Add(receiver.TopicId, receiverSet);
            }

            receiverSet.Add(receiver);
        }

        public void UnregisterReceiver(ILocalMessageEventReceiver receiver)
        {
            if (receiver == null)
            {
                throw new ArgumentNullException(nameof(receiver));
            }
            if (_receivers.TryGetValue(receiver.TopicId, out var receiverSet))
            {
                receiverSet.Remove(receiver);
                if (receiverSet.Count == 0)
                {
                    _receivers.Remove(receiver.TopicId);
                }
            }
        }

        #endregion

        #region Helpers

        internal IEnumerable<LocalMessage> GetPublishedMessages()
        {
            return _messageEvents.Values.SelectMany(messageEventTopics => messageEventTopics.Values)
                    .Where(message => message.TriggerTime is null || message.TriggerTime <= DateTime.Now);
        }

        #endregion
    }
}
