using System;
using OSK.MessageBus.Abstractions;
using System.Threading.Tasks;
using OSK.MessageBus.Local.Options;
using OSK.MessageBus.Ports;
using OSK.MessageBus.Options;
using OSK.MessageBus.Messages.Abstractions;

namespace OSK.MessageBus.Local.Ports
{
    public interface ILocalMessageBusBuilder
    {
        ILocalMessageBusBuilder WithMessageBusConfiguration(Action<MessageBusConfigurationOptions> configuration);

        ILocalMessageBusBuilder WithLocalBusRuntimeService(Action<LocalMessageBusOptions> configuration);

        ILocalMessageBusBuilder AddLocalReceiver<TMessage>(string topicFilter, 
            Action<IMessageReceiverBuilder>? builderConfigurator, Func<IMessageTransmissionContext<TMessage>, Task> handler)
            where TMessage : IMessage;

        ILocalMessageBusBuilder AddLocalReceiver<TMessage, THandler>(string topicFilter,
            Action<IMessageReceiverBuilder>? builderConfigurator)
            where TMessage : IMessage
            where THandler : IMessageTransmissionHandler<TMessage>;
    }
}
