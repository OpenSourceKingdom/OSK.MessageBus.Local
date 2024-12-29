using OSK.Hexagonal.MetaData;
using OSK.MessageBus.Abstractions;
using OSK.MessageBus.Local.Options;
using System.Threading;
using System.Threading.Tasks;

namespace OSK.MessageBus.Local.Ports
{
    [HexagonalIntegration(HexagonalIntegrationType.LibraryProvided)]
    public interface ILocalMessageTransmitter: IMessageTransmitter
    {
        Task SendTransmittedMessagesAsync(SendTransmittedMessagesOptions options, CancellationToken cancellationToken = default);

        void RegisterReceiver(ILocalMessageReceiver receiver);

        void UnregisterReceiver(ILocalMessageReceiver receiver);
    }
}
