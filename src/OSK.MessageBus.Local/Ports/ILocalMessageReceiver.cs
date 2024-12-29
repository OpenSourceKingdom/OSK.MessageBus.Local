using OSK.Hexagonal.MetaData;
using OSK.MessageBus.Local.Models;
using OSK.MessageBus.Ports;
using System.Threading.Tasks;

namespace OSK.MessageBus.Local.Ports
{
    [HexagonalIntegration(HexagonalIntegrationType.LibraryProvided)]
    public interface ILocalMessageReceiver: IMessageReceiver
    {
        string TopicId { get; }

        Task ReceiveMessageAsync(LocalMessage message);
    }
}
