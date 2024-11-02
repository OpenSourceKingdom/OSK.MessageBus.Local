using OSK.MessageBus.Events.Abstractions;
using System;

namespace OSK.MessageBus.Local.Models
{
    public class LocalMessage(IMessageEvent messageEvent)
    {
        public Guid Id { get; set; }

        public DateTime? TriggerTime { get; set; }

        public IMessageEvent MessageEvent => messageEvent;
    }
}
