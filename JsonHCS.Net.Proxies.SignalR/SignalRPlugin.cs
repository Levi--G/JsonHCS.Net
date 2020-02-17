using Castle.DynamicProxy;
using JsonHCSNet.Proxies.SignalR;
using Microsoft.AspNetCore.Http.Connections.Client;
//using Microsoft.AspNet.SignalR.Client;
using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace JsonHCSNet.Proxies.Plugins
{
    /// <summary>
    /// Provides ActionResult support
    /// </summary>
    public class SignalRPlugin : ProxyPlugin
    {
        public override bool IsRouteProvider => false;

        public override bool IsParameterProvider => false;

        public override bool IsHandler => true;

        public bool AutoReconnect { get; set; }

        public bool ConnectOnRequest { get; set; } = true;

        public Action<HttpConnectionOptions> ConfigureOptions { get; set; } = (a) => { };

        public Func<string, IHubConnectionBuilder, HubConnection> ConnectionBuilder { get; set; }

        public Action<Microsoft.Extensions.Logging.ILoggingBuilder> ConfigureLogging { get; set; }

        public SignalRPlugin()
        {
            ConnectionBuilder = DefaultConnectionBuilder;
        }

        public override string GetRoute(PluginManager manager, MemberInfo member)
        {
            throw new NotImplementedException();
        }

        public override IEnumerable<Parameter> GetParameters(PluginManager manager, string route, IInvocation invocation)
        {
            throw new NotImplementedException();
        }

        public override bool CanHandle(Type targetType, IInvocation invocation)
        {
            return targetType == typeof(HubConnection) || (targetType.IsConstructedGenericType && (targetType.GetGenericTypeDefinition() == typeof(HubConnection<>) || targetType.GetGenericTypeDefinition() == typeof(HubConnection<,>))) || HasAttribute(invocation.Method, typeof(HubMethodAttribute));
        }

        public override Task<T> Handle<T>(PluginManager manager, JsonHCS jsonHCS, string route, List<Parameter> parameters, Type targetType, IInvocation invocation)
        {
            if (targetType == typeof(HubConnection))
            {
                return CreateOrGetHub(route) as Task<T>;
            }
            if (targetType.IsConstructedGenericType && (targetType.GetGenericTypeDefinition() == typeof(HubConnection<>) || targetType.GetGenericTypeDefinition() == typeof(HubConnection<,>)))
            {
                return Convert<T>(CreateOrGetHubObject(route, targetType));
            }
            if (GetAttribute<HubMethodAttribute>(invocation.Method) is HubMethodAttribute att)
            {
                return InvokeOnHub<T>(att, route, targetType, invocation);
            }
            return Task.FromException<T>(new NotSupportedException("The method invocation was not supported"));
        }

        Dictionary<string, HubConnection> Cache = new Dictionary<string, HubConnection>();

        async Task<object> CreateOrGetHubObject(string route, Type type)
        {
            return await CreateOrGetHub(route, new SignalR.HubConnectionBuilder(type));
        }

        HubConnection DefaultConnectionBuilder(string route, IHubConnectionBuilder builder)
        {
            builder = builder.WithUrl(route, ConfigureOptions);
            if (AutoReconnect)
            {
                builder.WithAutomaticReconnect();
            }
            if (ConfigureLogging != null)
            {
                builder.ConfigureLogging(ConfigureLogging);
            }
            return builder.Build();
        }

        async Task<HubConnection> CreateOrGetHub(string route, IHubConnectionBuilder builder = null)
        {
            HubConnection hub;
            lock (Cache)
            {
                if (!Cache.TryGetValue(route, out hub))
                {
                    builder = builder ?? new Microsoft.AspNetCore.SignalR.Client.HubConnectionBuilder();
                    hub = ConnectionBuilder(route, builder);
                    Cache[route] = hub;
                }
            }
            if (hub.State == HubConnectionState.Disconnected && ConnectOnRequest)
            {
                await hub.StartAsync();
            }
            return hub;
        }

        async Task<T> InvokeOnHub<T>(HubMethodAttribute att, string route, Type targetType, IInvocation invocation)
        {
            var hub = await CreateOrGetHub(route);
            var methodname = att.MethodName ?? invocation.Method.Name;
            if (att.Type == SendType.Send)
            {
                await hub.SendCoreAsync(methodname, invocation.Arguments);
            }
            else if (att.Type == SendType.Invoke)
            {
                if (targetType == typeof(void))
                {
                    await hub.InvokeCoreAsync(methodname, invocation.Arguments);
                }
                else
                {
                    return await hub.InvokeCoreAsync<T>(methodname, invocation.Arguments);
                }
            }
            else
            {
                throw new NotSupportedException("The chosen SendType was not supported");
            }
            return default;
        }
    }
}
