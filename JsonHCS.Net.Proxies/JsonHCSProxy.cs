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
            Task returntask = (Task)this.GetType()
                .GetMethod("GetReturnTask", BindingFlags.NonPublic | BindingFlags.Instance)
                .MakeGenericMethod(TargetType == typeof(void) ? typeof(object) : TargetType)
                .Invoke(this, new object[] { invocation, TargetType });

            if (istask)
            {
                invocation.ReturnValue = returntask;
            }
            else
            {
                returntask.Wait();
                if (invocation.Method.ReturnType != typeof(void))
                    invocation.ReturnValue = returntask.GetType().GetProperty("Result", BindingFlags.Instance | BindingFlags.Public)?.GetValue(returntask);
            }
        }

        Task<T> GetReturnTask<T>(IInvocation invocation, Type targetType)
        {
            return Task.Run(() => { return pluginManager.GetRoute(invocation, baseUrl); })
                .ContinueWith((t) => { return new { route = t.Result, parameters = pluginManager.GetParameters(t.Result, invocation).ToList() }; })
                .ContinueWith((t) => { return pluginManager.Handle<T>(jsonHCS, t.Result.route, t.Result.parameters, targetType, invocation); })
                .Unwrap();
        }
    }
}
