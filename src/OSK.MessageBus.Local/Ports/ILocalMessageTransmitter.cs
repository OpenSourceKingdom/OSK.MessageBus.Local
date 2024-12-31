using OSK.Hexagonal.MetaData;
using OSK.MessageBus.Local.Options;
using OSK.Transmissions.Abstractions;
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
