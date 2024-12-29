﻿using Microsoft.Extensions.DependencyInjection;
using OSK.MessageBus.Local.Internal.Services;
using OSK.MessageBus.Local.Ports;
using System;

namespace OSK.MessageBus.Local
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddLocalBus(this IServiceCollection services, Action<ILocalMessageBusBuilder> localBusConfigurator)
        {
            services.AddMessageEventTransmitter<LocalMessageTransmitter, ILocalMessageReceiver>("OSK.MessageBus.Local", builder =>
            {
                var localMessageBusBuilder = new LocalMessageBusBuilder(services, builder);
                localBusConfigurator(localMessageBusBuilder);

                localMessageBusBuilder.Apply();
            });

            return services;
        }
    }
}
