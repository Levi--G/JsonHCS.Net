using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace JsonHCSNet.Proxies.SignalR
{
    public class HubConnectionBuilder : Microsoft.AspNetCore.SignalR.Client.IHubConnectionBuilder
    {
        private bool _hubConnectionBuilt;

        /// <inheritdoc />
        public IServiceCollection Services { get; }
        Type type;

        /// <summary>
        /// Initializes a new instance of the <see cref="HubConnectionBuilder"/> class.
        /// </summary>
        public HubConnectionBuilder(Type type)
        {
            this.type = type;
            Services = new ServiceCollection();
            Services.AddSingleton(type);
            Services.AddLogging();
            this.AddJsonProtocol();
        }

        /// <inheritdoc />
        public Microsoft.AspNetCore.SignalR.Client.HubConnection Build()
        {
            // Build can only be used once
            if (_hubConnectionBuilt)
            {
                throw new InvalidOperationException("HubConnectionBuilder allows creation only of a single instance of HubConnection.");
            }

            _hubConnectionBuilt = true;

            // The service provider is disposed by the HubConnection
            var serviceProvider = Services.BuildServiceProvider();

            var connectionFactory = serviceProvider.GetService<Microsoft.AspNetCore.Connections.IConnectionFactory>() ??
                throw new InvalidOperationException($"Cannot create HubConnection instance. An {nameof(Microsoft.AspNetCore.Connections.IConnectionFactory)} was not configured.");

            var endPoint = serviceProvider.GetService<System.Net.EndPoint>() ??
                throw new InvalidOperationException($"Cannot create HubConnection instance. An {nameof(System.Net.EndPoint)} was not configured.");

            return serviceProvider.GetService(type) as Microsoft.AspNetCore.SignalR.Client.HubConnection;
        }
    }
}
