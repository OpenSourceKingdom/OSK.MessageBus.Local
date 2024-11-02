﻿using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OSK.MessageBus.Application;
using OSK.MessageBus.Local.Options;
using OSK.MessageBus.Local.Ports;
using OSK.MessageBus.Ports;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace OSK.MessageBus.Local.Internal.Services
{
    internal class LocalMessageBus(
        IMessageEventReceiverManager manager,
        IEnumerable<MessageBusReceiverConfigurationService> receiverConfigurators,
        IServiceProvider serviceProvider,
        IOptions<LocalMessageBusOptions> options) : MessageBusApplicationService(manager, receiverConfigurators), IDisposable
    {
        #region Variables

        private static CancellationTokenSource? _cancellationTokenSource;
        private bool _started;

        #endregion

        #region IHostedService

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            await base.StartAsync(cancellationToken);

            if (!_started && options.Value.MessageBusRefreshRate.HasValue
                && options.Value.MessageBusRefreshRate > TimeSpan.Zero)
            {
                _cancellationTokenSource = new CancellationTokenSource();
                await Task.Factory.StartNew(
                    static (state) => RunMessageBusAsync((LocalMessageBusConfiguration)state),
                    new LocalMessageBusConfiguration(options.Value, serviceProvider)
                    {
                        CancellationToken = _cancellationTokenSource.Token
                    },
                    cancellationToken,
                    TaskCreationOptions.LongRunning,
                    TaskScheduler.Default);

                _started = true;
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            Dispose();

            await base.StopAsync(cancellationToken);
            _started = false;
        }

        #endregion

        public void Dispose()
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
        }

        #region Helpers

        private static async Task RunMessageBusAsync(LocalMessageBusConfiguration configuration)
        {
            var publisher = configuration.Services.GetRequiredService<ILocalMessageEventPublisher>();
            while (!configuration.CancellationToken.IsCancellationRequested)
            {
                await publisher.SendPublishedMessagesAsync(new SendPublishedMessagesOptions()
                {
                    RemoveMessagesFropTopicsWithoutReceivers = configuration.RemoveMessagesFromTopicsWithoutReceivers
                }, configuration.CancellationToken);
                if (configuration.CancellationToken.IsCancellationRequested)
                {
                    break;
                }

                await Task.Delay(configuration.MessageBusRefreshRate);
            }
        }

        #endregion
    }
}