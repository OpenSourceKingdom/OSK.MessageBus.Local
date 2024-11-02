using OSK.Hexagonal.MetaData;
using OSK.MessageBus.Local.Options;
using OSK.MessageBus.Ports;
using System.Threading;
using System.Threading.Tasks;

namespace OSK.MessageBus.Local.Ports
{
    [HexagonalPort(HexagonalPort.Primary)]
    public interface ILocalMessageEventPublisher: IMessageEventPublisher
    {
        Task SendPublishedMessagesAsync(SendPublishedMessagesOptions options, CancellationToken cancellationToken = default);

        void RegisterReceiver(ILocalMessageEventReceiver receiver);

        void UnregisterReceiver(ILocalMessageEventReceiver receiver);
    }
}
