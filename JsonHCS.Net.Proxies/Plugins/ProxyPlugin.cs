using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Castle.DynamicProxy;

namespace JsonHCSNet.Proxies.Plugins
{
    public abstract class ProxyPlugin : IProxyPlugin
    {
        public abstract bool IsRouteProvider { get; }
        public abstract bool IsParameterProvider { get; }
        public abstract bool IsHandler { get; }

        public abstract string GetRoute(PluginManager manager, MemberInfo member);
        public abstract IEnumerable<Parameter> GetParameters(PluginManager manager, string route, IInvocation invocation);
        public abstract bool CanHandle(Type targetType, IInvocation invocation);
        public abstract Task Handle(PluginManager manager, JsonHCS jsonHCS, string route, List<Parameter> parameters, Type targetType, IInvocation invocation);

        protected static List<KeyValuePair<string, IEnumerable<string>>> GetHeaders(List<Parameter> parameters)
        {
            List<KeyValuePair<string, IEnumerable<string>>> headers = new List<KeyValuePair<string, IEnumerable<string>>>();
            var headerParameters = parameters.Where(p => p.Type == SourceType.Header);
            foreach (var param in headerParameters)
            {
                var key = param.Name;
                var value = param.Value ?? string.Empty;
                if (value is IEnumerable<string> i)
                {
                    headers.Add(new KeyValuePair<string, IEnumerable<string>>(key, i));
                }
                else if (value is string s)
                {
                    headers.Add(new KeyValuePair<string, IEnumerable<string>>(key, new[] { s }));
                }
                else
                {
                    headers.Add(new KeyValuePair<string, IEnumerable<string>>(key, new[] { value.ToString() }));
                }
            }

            return headers;
        }

        protected static string ApplyQueryParameters(string route, List<Parameter> parameters)
        {
            var queryParameters = parameters.Where(p => p.Type == SourceType.Query);
            if (queryParameters != null && queryParameters.Count() > 0)
            {
                route += "?" + string.Join("&", queryParameters.Select(q =>
                {
                    return $"{q.Name}={System.Net.WebUtility.UrlEncode(q.Value?.ToString() ?? string.Empty)}";
                }));
            }

            return route;
        }

        protected static string ApplyRouteParameters(string route, List<Parameter> parameters)
        {
            var routeParameters = parameters.Where(p => p.Type == SourceType.Route);
            if (routeParameters != null)
            {
                foreach (var param in routeParameters)
                {
                    var value = param.Value.ToString();
                    route = route.Replace($"{{{param.Name}}}", value);
                }
            }

            return route;
        }

        protected static object GetPostParameter(List<Parameter> parameters)
        {
            var postParameters = parameters.Where(p => p.Type == SourceType.Body);
            object postArgument;
            if (postParameters.Count() < 2)
            {
                postArgument = postParameters.FirstOrDefault().Value;
            }
            else
            {
                postArgument = postParameters.ToDictionary(t => t.Name, t => t.Value);
            }

            return postArgument;
        }

        protected static bool HasAttribute(ICustomAttributeProvider data, Type type)
        {
            return data.GetCustomAttributes(type, false).FirstOrDefault() != null;
        }

        protected static bool HasAttribute(ICustomAttributeProvider data, string name)
        {
            return GetAttribute(data, name) != null;
        }

        protected static object GetAttribute(ICustomAttributeProvider data, string name)
        {
            return GetAttributes(data, name).FirstOrDefault();
        }

        protected static T GetAttribute<T>(MemberInfo data) where T : Attribute
        {
            return (T)data.GetCustomAttributes(typeof(T), false).FirstOrDefault();
        }

        protected static IEnumerable<object> GetAttributes(ICustomAttributeProvider data, string name)
        {
            return data.GetCustomAttributes(false).Where(a => a.GetType().Name == name);
        }

        protected static string GetParameterName(string attribute, ParameterInfo parameter)
        {
            var a = GetAttribute(parameter, attribute);
            var name = a.GetType().GetProperty("Name")?.GetValue(a) as string ?? parameter.Name;
            return name;
        }

        protected static object GetParameterValue(ParameterInfo p, object[] args)
        {
            return (args.Length > p.Position ? args[p.Position] : null ?? p.DefaultValue);
        }

        protected static bool IsSimple(Type type)
        {
            var typeInfo = type.GetTypeInfo();
            if (typeInfo.IsGenericType && typeInfo.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                // nullable type, check if the nested type is simple.
                return IsSimple(typeInfo.GetGenericArguments()[0]);
            }
            return typeInfo.IsPrimitive
              || typeInfo.IsEnum
              || type.Equals(typeof(string))
              || type.Equals(typeof(decimal));
        }

        protected Task ConvertTask(Task<object> task, Type type)
        {
            return (Task)typeof(ProxyPlugin).GetMethod("Convert").MakeGenericMethod(type).Invoke(null, new object[] { task });
        }

        public async static Task<T> Convert<T>(Task<object> task)
        {
            return await task.ConfigureAwait(false) is T value ? value : default(T);
        }
    }
}
