using System.Threading.Tasks;
using System;
using OSK.MessageBus.Local.Ports;
using OSK.Transmissions.Abstractions;
using OSK.Transmissions.Messages.Abstractions;
using OSK.Transmissions.Ports;

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
