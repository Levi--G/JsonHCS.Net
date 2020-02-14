using Castle.DynamicProxy;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace JsonHCSNet.Proxies.Plugins
{
    public class BasicPlugin : ProxyPlugin
    {
        public override bool IsRouteProvider => true;

        public override bool IsParameterProvider => true;

        public override bool IsHandler => true;

        public override string GetRoute(PluginManager manager, MemberInfo member)
        {
            var routeattributes = new[] { "RouteAttribute", "HttpGetAttribute", "HttpPostAttribute", "HttpPutAttribute", "HttpDeleteAttribute" };
            var route = member.GetCustomAttributes(true).Where(a => routeattributes.Contains(a.GetType().Name)).Select(att => att?.GetType().GetProperty("Template")?.GetValue(att) as string).FirstOrDefault(r => r != null);
            return route;
        }

        public override IEnumerable<Parameter> GetParameters(PluginManager manager, string route, IInvocation invocation)
        {
            var routeparams = Regex.Matches(route, @"\{.+?\}", RegexOptions.Compiled).OfType<Match>().Select(m => m.Value.Substring(1, m.Value.Length - 2)).ToArray();
            return invocation.Method.GetParameters().Select(p => FindParameterTypeAndName(p, routeparams, invocation.Arguments));
        }

        public override bool CanHandle(Type targetType, IInvocation invocation)
        {
            return true;
        }

        public override async Task<T> Handle<T>(PluginManager manager, JsonHCS jsonHCS, string route, List<Parameter> parameters, IInvocation invocation)
        {
            var targetType = typeof(T);
            object postArgument = GetPostParameter(parameters);
            route = ApplyRouteParameters(route, parameters);
            route = ApplyQueryParameters(route, parameters);
            var headers = GetHeaders(parameters);

            var method = FindHttpMethod(invocation.Method, parameters);
            if (method == HttpMethod.Get && targetType == typeof(System.IO.Stream))
            {
                return (T)(object)await jsonHCS.GetStreamAsync(route).ConfigureAwait(false);
            }
            else if (method == HttpMethod.Get && targetType == typeof(System.IO.MemoryStream))
            {
                return (T)(object)await jsonHCS.GetMemoryStreamAsync(route).ConfigureAwait(false);
            }

            HttpContent content = null;

            if (postArgument != null)
            {
                content = new StringContent(jsonHCS.SerializeJson(postArgument));
                content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            }

            var response = await jsonHCS.SendRequestAsync(method, route, content, headers).ConfigureAwait(false);

            if (targetType == typeof(void) || targetType == typeof(HttpResponseMessage))
            {
                return (T)(object)response;
            }
            else if (HasAttribute(invocation.Method, typeof(RawStringAttribute)))
            {
                return (T)(object)await JsonHCS.ReadContentAsString(response).ConfigureAwait(false);
            }
            else if (targetType == typeof(JObject))
            {
                return (T)(object)jsonHCS.DeserializeJObject(await JsonHCS.ReadContentAsString(response).ConfigureAwait(false));
            }
            else
            {
                return jsonHCS.DeserializeJson<T>(await JsonHCS.ReadContentAsString(response).ConfigureAwait(false));
            }
        }

        static HttpMethod FindHttpMethod(ICustomAttributeProvider data, List<Parameter> parameters)
        {
            if (HasAttribute(data, "HttpPostAttribute"))
            {
                return HttpMethod.Post;
            }
            if (HasAttribute(data, "HttpPutAttribute"))
            {
                return HttpMethod.Put;
            }
            if (HasAttribute(data, "HttpDeleteAttribute"))
            {
                return HttpMethod.Delete;
            }
            if (HasAttribute(data, "HttpGetAttribute"))
            {
                return HttpMethod.Get;
            }
            if (parameters.Any(p => p.Type == SourceType.Body || p.Type == SourceType.Form))
            {
                return HttpMethod.Post;
            }
            return HttpMethod.Get;
        }

        static Parameter FindParameterTypeAndName(ParameterInfo parameter, string[] queryParameters, object[] values)
        {
            if (HasAttribute(parameter, "FromBodyAttribute"))
            {
                return new Parameter(parameter.Name, SourceType.Body, GetParameterValue(parameter, values));
            }
            if (HasAttribute(parameter, "FromFormAttribute"))
            {
                return new Parameter(GetParameterName("FromFormAttribute", parameter), SourceType.Form, GetParameterValue(parameter, values));
            }
            if (HasAttribute(parameter, "FromHeaderAttribute"))
            {
                return new Parameter(GetParameterName("FromHeaderAttribute", parameter), SourceType.Header, GetParameterValue(parameter, values));
            }
            if (HasAttribute(parameter, "FromQueryAttribute"))
            {
                return new Parameter(GetParameterName("FromQueryAttribute", parameter), SourceType.Query, GetParameterValue(parameter, values));
            }
            if (HasAttribute(parameter, "FromRouteAttribute"))
            {
                return new Parameter(GetParameterName("FromRouteAttribute", parameter), SourceType.Route, GetParameterValue(parameter, values));
            }
            if (HasAttribute(parameter, "FromServicesAttribute"))
            {
                return new Parameter(parameter.Name, SourceType.Services, GetParameterValue(parameter, values));
            }
            //implicit
            if (!IsSimple(parameter.ParameterType))
            {
                return new Parameter(parameter.Name, SourceType.Body, GetParameterValue(parameter, values));
            }
            if (queryParameters.Contains(parameter.Name))
            {
                return new Parameter(parameter.Name, SourceType.Route, GetParameterValue(parameter, values));
            }
            return new Parameter(parameter.Name, SourceType.Query, GetParameterValue(parameter, values));
        }
    }
}
