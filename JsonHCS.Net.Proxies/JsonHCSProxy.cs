using Castle.DynamicProxy;
using JsonHCSNet.Proxies.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;


namespace JsonHCSNet.Proxies
{
    internal class JsonHCSProxy : IInterceptor
    {
        PluginManager pluginManager;
        JsonHCS jsonHCS;
        readonly string baseUrl;

        internal JsonHCSProxy(PluginManager pluginManager, JsonHCS jsonHCS, string baseUrl)
        {
            this.pluginManager = pluginManager;
            this.jsonHCS = jsonHCS;
            this.baseUrl = baseUrl;
        }

        public void Intercept(IInvocation invocation)
        {
            var TargetType = invocation.Method.ReturnType;
            bool istask = typeof(Task).IsAssignableFrom(TargetType);
            if (istask)
            {
                if (TargetType.IsConstructedGenericType)
                {
                    TargetType = TargetType.GetGenericArguments().First();
                }
                else
                {
                    TargetType = typeof(void);
                }
            }
            var route = pluginManager.GetRoute(invocation, baseUrl);
            var parameters = pluginManager.GetParameters(route, invocation).ToList();
            Task returntask = pluginManager.Handle(jsonHCS, route, parameters, TargetType, invocation);
            if (istask)
            {
                invocation.ReturnValue = returntask;
            }
            else if (TargetType != typeof(void))
            {
                returntask.Wait();
                invocation.ReturnValue = returntask.GetType().GetProperty("Result", BindingFlags.Instance | BindingFlags.Public)?.GetValue(returntask);
            }
        }
    }
}
