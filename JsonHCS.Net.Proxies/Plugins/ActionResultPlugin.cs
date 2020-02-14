using Castle.DynamicProxy;
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
    public class ActionResultPlugin : ProxyPlugin
    {
        public override bool IsRouteProvider => false;

        public override bool IsParameterProvider => false;

        public override bool IsHandler => true;

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
            return typeof(ApiDefinition.IActionResult).IsAssignableFrom(targetType);
        }

        public override Task Handle(PluginManager manager, JsonHCS jsonHCS, string route, List<Parameter> parameters, Type targetType, IInvocation invocation)
        {
            //Get HttpResponseMessage with default implementation
            Task returntask = manager.Handle(jsonHCS, route, parameters, typeof(System.Net.Http.HttpResponseMessage), invocation);

            //implement own usage
            if (targetType.IsConstructedGenericType)
            {
                returntask = (Task)this.GetType().GetMethod("GetActionResultT").MakeGenericMethod(targetType.GetGenericArguments().First()).Invoke(this, new object[] { returntask, jsonHCS });
            }
            else if (typeof(ApiDefinition.ActionResult).IsAssignableFrom(targetType))
            {
                returntask = GetActionResult(returntask as Task<System.Net.Http.HttpResponseMessage>, jsonHCS);
            }
            else
            {
                returntask = GetIActionResult(returntask as Task<System.Net.Http.HttpResponseMessage>, jsonHCS);
            }
            return returntask;
        }

        async Task<ApiDefinition.ActionResult> GetActionResult(Task<System.Net.Http.HttpResponseMessage> response, JsonHCS jsonHCS)
        {
            return new ActionResultSupport.GenericResult<object>(await response.ConfigureAwait(false), jsonHCS);
        }

        async Task<ApiDefinition.IActionResult> GetIActionResult(Task<System.Net.Http.HttpResponseMessage> response, JsonHCS jsonHCS)
        {
            return new ActionResultSupport.GenericResult<object>(await response.ConfigureAwait(false), jsonHCS);
        }

        public async Task<ApiDefinition.ActionResult<T>> GetActionResultT<T>(Task<System.Net.Http.HttpResponseMessage> response, JsonHCS jsonHCS)
        {
            return new ActionResultSupport.GenericResult<T>(await response.ConfigureAwait(false), jsonHCS);
        }
    }
}
