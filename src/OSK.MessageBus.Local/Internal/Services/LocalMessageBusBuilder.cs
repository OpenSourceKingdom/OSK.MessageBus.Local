using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using OSK.MessageBus.Abstractions;
using OSK.MessageBus.Local.Options;
using OSK.MessageBus.Local.Ports;
using OSK.MessageBus.Messages.Abstractions;
using OSK.MessageBus.Options;
using OSK.MessageBus.Ports;

namespace OSK.MessageBus.Local.Internal.Services
{
    internal class LocalMessageBusBuilder(IServiceCollection services, IMessageReceiverGroupBuilder<ILocalMessageReceiver> builder)
        : ILocalMessageBusBuilder
    {
        #region Variables

        private readonly List<Action<IMessageReceiverGroupBuilder<ILocalMessageReceiver>>> _receiverConfigurations = [];
        private Action<MessageBusConfigurationOptions>? _messageBusConfiguration;
        private Action<LocalMessageBusOptions>? _localMessageBusOptions;

        #endregion

        #region ILocalMessageBusBuilder

        public ILocalMessageBusBuilder AddLocalReceiver<TMessage>(string topicFilter, 
            Action<IMessageReceiverBuilder>? builderConfigurator, Func<IMessageTransmissionContext<TMessage>, Task> handler) 
            where TMessage : IMessage
        {
            _receiverConfigurations.Add(transmissionBuilder =>
            {
                transmissionBuilder.AddMessageEventReceiver<LocalMessageEventReceiver<TMessage>>(
                    Guid.NewGuid().ToString(), [topicFilter], receiverBuilder =>
                    {
                        receiverBuilder.UseHandler(handler);
                        builderConfigurator?.Invoke(receiverBuilder);
                    });
            });
            return this;
        }

        public ILocalMessageBusBuilder AddLocalReceiver<TMessage, THandler>(string topicFilter,
            Action<IMessageReceiverBuilder>? builderConfigurator)
            where TMessage : IMessage
            where THandler : IMessageTransmissionHandler<TMessage>
        {
            _receiverConfigurations.Add(transmissionBuilder =>
            {
                transmissionBuilder.AddMessageEventReceiver<LocalMessageEventReceiver<TMessage>>(
                    Guid.NewGuid().ToString(), [topicFilter], receiverBuilder =>
                    {
                        receiverBuilder.UseHandler<TMessage, THandler>();
                        builderConfigurator?.Invoke(receiverBuilder);
                    });
            });
            return this;
        }

        public ILocalMessageBusBuilder WithLocalBusRuntimeService(Action<LocalMessageBusOptions> configuration)
        {
            _localMessageBusOptions = configuration;
            return this;
        }

        public ILocalMessageBusBuilder WithMessageBusConfiguration(Action<MessageBusConfigurationOptions> configuration)
        {
            _messageBusConfiguration = configuration;
            return this;
        }

        #endregion

        #region Helpers

        internal void Apply()
        {
            if (_messageBusConfiguration is null)
            {
                services.AddMessageTransmissions();
            }
            else
            {
                services.AddMessageTransmissions(_messageBusConfiguration);
            }

            if (_localMessageBusOptions is not null)
            {
                services.Configure(_localMessageBusOptions);
                services.AddHostedService<LocalMessageBus>();
            }

            foreach (var receiverConfiguration in _receiverConfigurations)
            {
                receiverConfiguration(builder);
            }
        }

        #endregion
    }
}
