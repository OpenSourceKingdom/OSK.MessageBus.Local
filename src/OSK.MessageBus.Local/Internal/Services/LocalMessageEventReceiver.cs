using OSK.MessageBus.Local.Ports;
using System;
using System.Threading.Tasks;
using OSK.MessageBus.Models;
using OSK.MessageBus.Local.Models;
using OSK.MessageBus.Messages.Abstractions;

namespace OSK.MessageBus.Local.Internal.Services
{
    internal class LocalMessageEventReceiver<TMessage>(string receiverId, string topicId, MessageTransmissionDelegate transmissionDelegate,
        ILocalMessageTransmitter transmitter, IServiceProvider serviceProvider)
        : MessageEventReceiverBase(receiverId, transmissionDelegate, serviceProvider), ILocalMessageReceiver
        where TMessage : IMessage
    {
        #region ILocalMessageEventReceiver

        public string TopicId => topicId;

        public override void Dispose()
        {
            transmitter.UnregisterReceiver(this);
        }

        public override void Start()
        {
            transmitter.RegisterReceiver(this);
        }

        public Task ReceiveMessageAsync(LocalMessage message)
        {
            if (message.MessageEvent is TMessage typedMessage)
            {
                return ProcessTransmissionAsync(typedMessage, message);
            }

            return Task.CompletedTask;
        }

        #endregion
    }
}
