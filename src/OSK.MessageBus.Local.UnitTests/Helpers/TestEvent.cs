using OSK.MessageBus.Events.Abstractions;

namespace OSK.MessageBus.Local.UnitTests.Helpers
{
    public class TestEvent : IMessageEvent
    {
        public string TopicId { get; set; }
    }
}
