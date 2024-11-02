using OSK.MessageBus.Local.Options;
using System;
using System.Threading;

namespace OSK.MessageBus.Local.Internal
{
    internal class LocalMessageBusConfiguration(LocalMessageBusOptions options, IServiceProvider serviceProvider)
    {
        public IServiceProvider Services => serviceProvider;

        public CancellationToken CancellationToken { get; set; }

        public TimeSpan MessageBusRefreshRate = options.MessageBusRefreshRate!.Value;

        public bool RemoveMessagesFromTopicsWithoutReceivers => options.RemoveMessagesFromTopicsWithoutReceivers;
    }
}
