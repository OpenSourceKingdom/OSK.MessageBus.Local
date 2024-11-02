using Microsoft.Extensions.DependencyInjection;
using OSK.MessageBus.Events.Abstractions;
using OSK.MessageBus.Models;
using OSK.MessageBus.Ports;
using System;

namespace OSK.MessageBus.Local.Internal.Services
{
    internal class LocalMessageEventReceiverBuilder<TMessage>(IServiceProvider serviceProvider) : MessageEventReceiverBuilderBase
        where TMessage: IMessageEvent
    {
        #region MessageEventReceiverBuilderBase Overrides

        protected override IMessageEventReceiver BuildReceiver(string subscriptionId, MessageEventDelegate eventDelegate)
        {
            return ActivatorUtilities.CreateInstance<LocalMessageEventReceiver<TMessage>>(serviceProvider, subscriptionId, eventDelegate);
        }

        #endregion
    }
}
