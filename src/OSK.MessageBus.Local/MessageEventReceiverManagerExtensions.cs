using OSK.MessageBus.Abstractions;
using OSK.MessageBus.Events.Abstractions;
using OSK.MessageBus.Local.Internal.Services;
using OSK.MessageBus.Ports;
using System.Threading.Tasks;
using System;

namespace OSK.MessageBus.Local
{
    public static class MessageEventReceiverManagerExtensions
    {
        public static IMessageEventReceiverManager AddLocalReceiver<TMessage>(this IMessageEventReceiverManager manager,
            string subscriberId, string topicFilter, Func<IMessageEventContext<TMessage>, Task> handler)
            where TMessage : IMessageEvent
            => manager.AddLocalReceiver(subscriberId, topicFilter, null, handler);

        public static IMessageEventReceiverManager AddLocalReceiver<TMessage>(this IMessageEventReceiverManager manager,
            string subscriberId, string topicFilter, Action<IMessageEventReceiverBuilder>? builderConfigurator, Func<IMessageEventContext<TMessage>, Task> handler)
            where TMessage : IMessageEvent
        {
            return manager.AddEventReceiver(subscriberId, (serviceProvider, configurators) =>
            {
                var localReceiverBuilder = new LocalMessageEventReceiverBuilder<TMessage>(serviceProvider);

                foreach (var configurator in configurators)
                {
                    configurator(localReceiverBuilder);
                }

                builderConfigurator?.Invoke(localReceiverBuilder);
                localReceiverBuilder.UseHandler(handler);

                return localReceiverBuilder;
            });
        }

        public static IMessageEventReceiverManager AddLocalReceiver<TMessage, THandler>(this IMessageEventReceiverManager manager,
            string subscriberId, string topicFilter)
            where TMessage : IMessageEvent
            where THandler : IMessageEventHandler<TMessage>
            => manager.AddLocalReceiver<TMessage, THandler>(subscriberId, topicFilter, null);

        public static IMessageEventReceiverManager AddLocalReceiver<TMessage, THandler>(this IMessageEventReceiverManager manager,
            string subscriberId, string topicFilter, Action<IMessageEventReceiverBuilder>? builderConfigurator)
            where TMessage : IMessageEvent
            where THandler : IMessageEventHandler<TMessage>
        {
            return manager.AddEventReceiver(subscriberId, (serviceProvider, configurators) =>
            {
                var localReceiverBuilder = new LocalMessageEventReceiverBuilder<TMessage>(serviceProvider);

                foreach (var configurator in configurators)
                {
                    configurator(localReceiverBuilder);
                }

                builderConfigurator?.Invoke(localReceiverBuilder);
                localReceiverBuilder.UseHandler<TMessage, THandler>();

                return localReceiverBuilder;
            });
        }
    }
}
