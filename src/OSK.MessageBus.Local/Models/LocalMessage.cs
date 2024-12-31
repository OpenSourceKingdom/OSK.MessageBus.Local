using OSK.Transmissions.Messages.Abstractions;
using System;

namespace OSK.MessageBus.Local.Models
{
    public class LocalMessage(IMessage message)
    {
        public Guid Id { get; set; }

        public DateTime? TriggerTime { get; set; }

        public IMessage MessageEvent => message;
    }
}
