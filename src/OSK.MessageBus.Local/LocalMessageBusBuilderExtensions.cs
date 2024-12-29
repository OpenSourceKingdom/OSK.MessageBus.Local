using OSK.MessageBus.Abstractions;
using OSK.MessageBus.Ports;
using System.Threading.Tasks;
using System;
using OSK.MessageBus.Local.Ports;
using OSK.MessageBus.Messages.Abstractions;

namespace OSK.MessageBus.Local
{
    public static class LocalMessageBusBuilderExtensions
    {
        public static ILocalMessageBusBuilder AddLocalReceiver<TMessage>(this ILocalMessageBusBuilder builder,
            string topicFilter, Func<IMessageTransmissionContext<TMessage>, Task> handler)
            where TMessage : IMessage
            => builder.AddLocalReceiver(topicFilter, null, handler);

        public static ILocalMessageBusBuilder AddLocalReceiver<TMessage, THandler>(this ILocalMessageBusBuilder builder,
            string topicFilter)
            where TMessage : IMessage
            where THandler : IMessageTransmissionHandler<TMessage>
            => builder.AddLocalReceiver<TMessage, THandler>(topicFilter, null);
    }
}
