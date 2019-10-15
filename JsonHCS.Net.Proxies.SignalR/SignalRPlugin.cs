using Castle.DynamicProxy;
using JsonHCSNet.Proxies.SignalR;
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

        public Action<Microsoft.Extensions.Logging.ILoggingBuilder> ConfigureLogging { get; set; }

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

        public override Task Handle(PluginManager manager, JsonHCS jsonHCS, string route, List<Parameter> parameters, Type targetType, IInvocation invocation)
        {
            if (targetType == typeof(HubConnection))
            {
                return CreateOrGetHub(route);
            }
            if (targetType.IsConstructedGenericType && (targetType.GetGenericTypeDefinition() == typeof(HubConnection<>) || targetType.GetGenericTypeDefinition() == typeof(HubConnection<,>)))
            {
                return ConvertTask(CreateOrGetHubObject(route, targetType), targetType);
            }
            if (GetAttribute<HubMethodAttribute>(invocation.Method) is HubMethodAttribute att)
            {
                var task = InvokeOnHub(att, route, targetType, invocation);
                if (targetType != typeof(void))
                {
                    return ConvertTask(task, targetType);
                }
                return task;
            }
            return Task.FromException(new NotSupportedException("The method invocation was not supported"));
        }

        Dictionary<string, HubConnection> Cache = new Dictionary<string, HubConnection>();

        async Task<object> CreateOrGetHubObject(string route, Type type)
        {
            return await CreateOrGetHub(route, new SignalR.HubConnectionBuilder(type));
        }

        async Task<HubConnection> CreateOrGetHub(string route, IHubConnectionBuilder builder = null)
        {
            if (!Cache.TryGetValue(route, out var hub))
            {
                builder = builder ?? new Microsoft.AspNetCore.SignalR.Client.HubConnectionBuilder();
                builder = builder.WithUrl(route);
                if (AutoReconnect)
                {
                    builder.WithAutomaticReconnect();
                }
                if (ConfigureLogging != null)
                {
                    builder.ConfigureLogging(ConfigureLogging);
                }
                hub = builder.Build();
            }
            if (hub.State == HubConnectionState.Disconnected)
            {
                await hub.StartAsync();
            }
            return hub;
        }

        async Task<object> InvokeOnHub(HubMethodAttribute att, string route, Type targetType, IInvocation invocation)
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
                    return await hub.InvokeCoreAsync(methodname, targetType, invocation.Arguments);
                }
            }
            else
            {
                throw new NotSupportedException("The chosen SendType was not supported");
            }
            return null;
        }
    }
}
