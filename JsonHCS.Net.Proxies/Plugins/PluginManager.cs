using Castle.DynamicProxy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace JsonHCSNet.Proxies.Plugins
{
    public class PluginManager
    {
        public IProxyPlugin[] Plugins { get; private set; }

        public PluginManager(params IProxyPlugin[] plugins)
        {
            Plugins = plugins;
        }

        public string GetRoute(IInvocation invocation, string baseUrl)
        {
            var TargetType = invocation.TargetType ?? invocation.Method.DeclaringType;
            var route = new List<string>();
            {
                Type target = TargetType;
                while (target != null)
                {
                    route.Add(GetRoute(target.GetTypeInfo()));
                    target = (target.IsNested) ? target = target.DeclaringType : target = null;
                }
            }
            route.Add(baseUrl);
            route.Reverse();
            route.Add(GetRoute(invocation.Method));
            var fullroute = string.Join("/", route.Where(s => s != null).Select(s => s.Trim('/')));
            if (TargetType != null)
            {
                fullroute = fullroute.Replace("[controller]", TargetType.Name.Replace("Controller", ""));
            }
            return fullroute;
        }

        public string GetRoute(MemberInfo member)
        {
            return Plugins.Where(p => p.IsRouteProvider).Select(p => p.GetRoute(this, member)).FirstOrDefault(r => r != null);
        }

        public IEnumerable<Parameter> GetParameters(string route, IInvocation invocation)
        {
            return Plugins.Where(p => p.IsParameterProvider).SelectMany(p => p.GetParameters(this, route, invocation)).Distinct();
        }

        public bool CanHandle(Type targetType, IInvocation invocation)
        {
            return Plugins.Where(p => p.IsHandler).Any(p => p.CanHandle(targetType, invocation));
        }

        public Task<T> Handle<T>(JsonHCS jsonHCS, string route, List<Parameter> parameters, Type targetType, IInvocation invocation)
        {
            foreach (var plugin in Plugins.Where(p => p.IsHandler))
            {
                if (plugin.CanHandle(typeof(T), invocation))
                {
                    return plugin.Handle<T>(this, jsonHCS, route, parameters, targetType, invocation);
                }
            }
            return Task.FromException<T>(new NotImplementedException("No plugins could handle this request"));
        }
    }
}
