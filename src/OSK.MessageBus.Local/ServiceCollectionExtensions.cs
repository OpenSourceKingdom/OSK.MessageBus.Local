using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OSK.MessageBus.Local.Internal.Services;
using OSK.MessageBus.Local.Options;
using OSK.MessageBus.Local.Ports;
using OSK.MessageBus.Ports;
using System;

namespace OSK.MessageBus.Local
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddLocalBus(this IServiceCollection services)
            => services.AddLocalBus(null);

        public static IServiceCollection AddLocalBus(this IServiceCollection services,
            Action<LocalMessageBusOptions>? localBusConfigurator)
        {
            services.AddMessageBus();
            services.TryAddSingleton<IMessageEventPublisher>(provider => provider.GetRequiredService<ILocalMessageEventPublisher>());
            services.TryAddSingleton<ILocalMessageEventPublisher, LocalMessageEventPublisher>();

            if (localBusConfigurator != null)
            {
                services.Configure(localBusConfigurator);
                services.AddHostedService<LocalMessageBus>();
            }

            return services;
        }
    }
}
