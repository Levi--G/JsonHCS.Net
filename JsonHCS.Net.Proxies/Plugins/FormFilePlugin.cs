using Castle.DynamicProxy;
using JsonHCSNet.Proxies.ApiDefinition;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace JsonHCSNet.Proxies.Plugins
{
    /// <summary>
    /// Provides IFormFile support
    /// </summary>
    public class FormFilePlugin : ProxyPlugin
    {
        public override bool IsRouteProvider => false;

        public override bool IsParameterProvider => true;

        public override bool IsHandler => false;

        public override string GetRoute(PluginManager manager, MemberInfo member)
        {
            throw new NotImplementedException();
        }

        public override IEnumerable<Parameter> GetParameters(PluginManager manager, string route, IInvocation invocation)
        {
            foreach (var p in invocation.Method.GetParameters().Where(p => p.ParameterType == typeof(IFormFile)))
            {
                var name = GetParameterName(nameof(FromFormAttribute), p);
                var val = GetParameterValue(p, invocation.Arguments);
                var mediatype = GetAttribute<MediaTypeAttribute>(p)?.MediaType;
                var mt = mediatype != null ? MediaTypeHeaderValue.Parse(mediatype) : null;
                if (val is IFormFile formFile)
                {
                    yield return new Parameter(p.Name, SourceType.Body, FormFile.CreateFormDataFromStream(formFile.Content, name, formFile.ContentName, mt));
                }
            }
        }

        public override bool CanHandle(Type targetType, IInvocation invocation)
        {
            return false;
        }

        public override Task<T> Handle<T>(PluginManager manager, JsonHCS jsonHCS, string route, List<Parameter> parameters, Type targetType, IInvocation invocation)
        {
            throw new NotImplementedException();
        }
    }
}
