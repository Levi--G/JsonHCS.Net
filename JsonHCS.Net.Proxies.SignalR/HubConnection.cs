using Castle.DynamicProxy;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace JsonHCSNet.Proxies.SignalR
{
    public class HubConnection<T> : HubConnection where T : class
    {
        public HubConnection(IConnectionFactory connectionFactory, IHubProtocol protocol, EndPoint endPoint, IServiceProvider serviceProvider, ILoggerFactory loggerFactory) : base(connectionFactory, protocol, endPoint, serviceProvider, loggerFactory)
        {
            MakeProxy(new ProxyGenerator());
        }

        public HubConnection(IConnectionFactory connectionFactory, IHubProtocol protocol, EndPoint endPoint, IServiceProvider serviceProvider, ILoggerFactory loggerFactory, IRetryPolicy reconnectPolicy) : base(connectionFactory, protocol, endPoint, serviceProvider, loggerFactory, reconnectPolicy)
        {
            MakeProxy(new ProxyGenerator());
        }

        protected virtual void MakeProxy(ProxyGenerator p)
        {
            if (typeof(T).IsClass)
            {
                Send = p.CreateClassProxy<T>(new HubConnectionProxy(this, SendType.Send));
                Invoke = p.CreateClassProxy<T>(new HubConnectionProxy(this, SendType.Invoke));
            }
            else if (typeof(T).IsInterface)
            {
                Send = p.CreateInterfaceProxyWithoutTarget<T>(new HubConnectionProxy(this, SendType.Send));
                Invoke = p.CreateInterfaceProxyWithoutTarget<T>(new HubConnectionProxy(this, SendType.Invoke));
            }
        }

        public T Send { get; private set; }
        public T Invoke { get; private set; }
    }
    public class HubConnection<T,Y> : HubConnection<T> where T : class  where Y : class
    {
        public HubConnection(IConnectionFactory connectionFactory, IHubProtocol protocol, EndPoint endPoint, IServiceProvider serviceProvider, ILoggerFactory loggerFactory) : base(connectionFactory, protocol, endPoint, serviceProvider, loggerFactory)
        {
        }

        public HubConnection(IConnectionFactory connectionFactory, IHubProtocol protocol, EndPoint endPoint, IServiceProvider serviceProvider, ILoggerFactory loggerFactory, IRetryPolicy reconnectPolicy) : base(connectionFactory, protocol, endPoint, serviceProvider, loggerFactory, reconnectPolicy)
        {
        }

        protected override void MakeProxy(ProxyGenerator p)
        {
            base.MakeProxy(p);
            if (typeof(Y).IsClass)
            {
                On = p.CreateClassProxy<Y>(new ListenerProxy(this));
            }
            else if (typeof(Y).IsInterface)
            {
                On = p.CreateInterfaceProxyWithoutTarget<Y>(new ListenerProxy(this));
            }
        }

        public new Y On { get; private set; }
    }
}
