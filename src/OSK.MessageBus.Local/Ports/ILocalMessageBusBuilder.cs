using System;
using OSK.MessageBus.Abstractions;
using System.Threading.Tasks;
using OSK.MessageBus.Local.Options;
using OSK.MessageBus.Ports;
using OSK.MessageBus.Messages.Abstractions;
using OSK.Hexagonal.MetaData;

namespace OSK.MessageBus.Local.Ports
{
    [HexagonalIntegration(HexagonalIntegrationType.LibraryProvided)]
    public interface ILocalMessageBusBuilder
    {
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
