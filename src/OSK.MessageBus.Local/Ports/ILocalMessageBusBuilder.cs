using System;
using System.Threading.Tasks;
using OSK.MessageBus.Local.Options;
using OSK.Hexagonal.MetaData;
using OSK.Transmissions.Ports;
using OSK.Transmissions.Abstractions;
using OSK.Transmissions.Messages.Abstractions;

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
