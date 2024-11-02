using OSK.Hexagonal.MetaData;
using OSK.MessageBus.Local.Models;
using OSK.MessageBus.Ports;
using System.Threading.Tasks;

namespace OSK.MessageBus.Local.Ports
{
    [HexagonalPort(HexagonalPort.Primary)]
    public interface ILocalMessageEventReceiver: IMessageEventReceiver
    {
        string TopicId { get; }

        Task ReceiveMessageAsync(LocalMessage message);
    }
}
