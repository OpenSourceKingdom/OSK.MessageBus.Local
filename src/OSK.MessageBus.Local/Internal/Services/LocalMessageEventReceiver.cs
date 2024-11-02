using OSK.MessageBus.Local.Ports;
using System;
using System.Threading.Tasks;
using OSK.MessageBus.Models;
using OSK.MessageBus.Events.Abstractions;
using OSK.MessageBus.Local.Models;

namespace OSK.MessageBus.Local.Internal.Services
{
    internal class LocalMessageEventReceiver<TMessage>(string subscriptionId, MessageEventDelegate eventDelegate,
        ILocalMessageEventPublisher publisher, IServiceProvider serviceProvider)
        : MessageEventReceiverBase(eventDelegate, serviceProvider), ILocalMessageEventReceiver
        where TMessage : IMessageEvent
    {
        #region MessageReceiverBase

        public string TopicId => subscriptionId;

        public override void Dispose()
        {
            publisher.UnregisterReceiver(this);
        }

        public override void Start()
        {
            publisher.RegisterReceiver(this);
        }

        public Task ReceiveMessageAsync(LocalMessage message)
        {
            if (message is TMessage typedMessage)
            {
                return HandleEventAsync(typedMessage, message);
            }

            return Task.CompletedTask;
        }

        #endregion
    }
}
