using System;

namespace OSK.MessageBus.Local.Options
{
    public class LocalMessageBusOptions
    {
        public TimeSpan? MessageBusRefreshRate { get; set; }

        public bool RemoveMessagesFromTopicsWithoutReceivers { get; set; }
    }
}
