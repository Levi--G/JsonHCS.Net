using Castle.DynamicProxy;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace JsonHCSNet.Proxies.Plugins
{
    public interface IProxyPlugin
    {
        bool IsRouteProvider { get; }
        bool IsParameterProvider { get; }
        bool IsHandler { get; }

        string GetRoute(PluginManager manager, MemberInfo member);

        IEnumerable<Parameter> GetParameters(PluginManager manager, string route, IInvocation invocation);

        bool CanHandle(Type targetType, IInvocation invocation);

        Task<T> Handle<T>(PluginManager manager, JsonHCS jsonHCS, string route, List<Parameter> parameters, IInvocation invocation);
    }
}
