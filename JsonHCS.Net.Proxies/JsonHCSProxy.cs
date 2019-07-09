using Castle.DynamicProxy;
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
        JsonHCS jsonHCS;
        readonly string baseUrl;

        internal JsonHCSProxy(JsonHCS jsonHCS, string baseUrl)
        {
            this.jsonHCS = jsonHCS;
            this.baseUrl = baseUrl;
        }

        public void Intercept(IInvocation invocation)
        {
            string fullroute = GetRoute(invocation);

            var returnparam = invocation.Method.ReturnType;
            bool istask = typeof(Task).IsAssignableFrom(returnparam);
            if (istask)
            {
                if (returnparam.IsConstructedGenericType)
                {
                    returnparam = returnparam.GetGenericArguments().First();
                }
                else
                {
                    returnparam = typeof(void);
                }
            }
            bool noreturn = returnparam == typeof(void);
            bool isaction = typeof(ApiDefinition.IActionResult).IsAssignableFrom(returnparam);
            bool isIAction = false;
            bool isgenericaction = false;
            if (isaction)
            {
                if (returnparam.IsConstructedGenericType)
                {
                    isgenericaction = true;
                    returnparam = returnparam.GetGenericArguments().First();
                }
                else
                {
                    isIAction = !typeof(ApiDefinition.ActionResult).IsAssignableFrom(returnparam);
                    returnparam = typeof(void);
                }
            }

            var routeparams = Regex.Matches(fullroute, @"\{.+?\}", RegexOptions.Compiled).OfType<Match>().Select(m => m.Value.Substring(1, m.Value.Length - 2)).ToArray();
            var parameters = invocation.Method.GetParameters().Select(p => FindParameterTypeAndName(p, routeparams, invocation.Arguments)).ToList();

            var postParameters = parameters.Where(p => p.Item2 == SourceType.Body);
            object postArgument;
            if (postParameters.Count() < 2)
            {
                postArgument = postParameters.FirstOrDefault()?.Item3;
            }
            else
            {
                postArgument = postParameters.ToDictionary(t => t.Item1, t => t.Item3);
            }

            var routeParameters = parameters.Where(p => p.Item2 == SourceType.Route);
            if (routeParameters != null && routeParameters.Count() > 0)
            {
                foreach (var param in routeParameters)
                {
                    var value = param.Item3.ToString();
                    fullroute = fullroute.Replace($"{{{param.Item1}}}", value);
                }
            }

            var queryParameters = parameters.Where(p => p.Item2 == SourceType.Query);
            if (queryParameters != null && queryParameters.Count() > 0)
            {
                fullroute += "?" + string.Join("&", queryParameters.Select(q =>
                {
                    return $"{q.Item1}={System.Net.WebUtility.UrlEncode(q.Item3?.ToString() ?? string.Empty)}";
                }));
            }

            List<KeyValuePair<string, IEnumerable<string>>> headers = new List<KeyValuePair<string, IEnumerable<string>>>();
            var headerParameters = parameters.Where(p => p.Item2 == SourceType.Header);
            foreach (var param in headerParameters)
            {
                var key = param.Item1;
                var value = param.Item3 ?? string.Empty;
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

            Task returntask = null;
            var method = FindHttpMethod(invocation.Method, parameters);
            if (isaction || noreturn)
            {
                switch (method)
                {
                    case HttpMethod.Get:
                        returntask = jsonHCS.GetRawAsync(fullroute, headers);
                        break;
                    case HttpMethod.Post:
                        returntask = jsonHCS.PostToRawAsync(fullroute, postArgument, headers);
                        break;
                    case HttpMethod.Put:
                        returntask = jsonHCS.PutToRawAsync(fullroute, postArgument, headers);
                        break;
                    case HttpMethod.Delete:
                        returntask = jsonHCS.DeleteToRawAsync(fullroute, headers);
                        break;
                    default:
                        break;
                }
            }
            else
            {
                var type = FindReturnType(invocation.Method);
                if (type == ReturnType.Object)
                {
                    switch (method)
                    {
                        case HttpMethod.Get:
                            returntask = ConvertTask(jsonHCS.GetJsonAsync(fullroute, returnparam, headers), returnparam);
                            break;
                        case HttpMethod.Post:
                            returntask = ConvertTask(jsonHCS.PostToJsonAsync(fullroute, postArgument, returnparam, headers), returnparam);
                            break;
                        case HttpMethod.Put:
                            returntask = ConvertTask(jsonHCS.PutToJsonAsync(fullroute, postArgument, returnparam, headers), returnparam);
                            break;
                        case HttpMethod.Delete:
                            returntask = ConvertTask(jsonHCS.DeleteToJsonAsync(fullroute, returnparam, headers), returnparam);
                            break;
                        default:
                            break;
                    }
                }
                else if (type == ReturnType.Data)
                {
                    switch (method)
                    {
                        case HttpMethod.Get:
                            if (returnparam == typeof(System.IO.Stream))
                            {
                                returntask = jsonHCS.GetStreamAsync(fullroute);
                            }
                            else if (returnparam == typeof(System.IO.MemoryStream))
                            {
                                returntask = jsonHCS.GetMemoryStreamAsync(fullroute);
                            }
                            else
                            {
                                throw new Exception("RawDataAttribute can only be used with stream types");
                            }
                            break;
                        case HttpMethod.Post:
                            throw new Exception("RawDataAttribute can only be used with Get requests, please create an issue with a use case for implementation");
                        case HttpMethod.Put:
                            throw new Exception("RawDataAttribute can only be used with Get requests, please create an issue with a use case for implementation");
                        case HttpMethod.Delete:
                            throw new Exception("RawDataAttribute can only be used with Get requests, please create an issue with a use case for implementation");
                        default:
                            break;
                    }
                }
                else if (type == ReturnType.JObject)
                {
                    switch (method)
                    {
                        case HttpMethod.Get:
                            returntask = jsonHCS.GetJObjectAsync(fullroute, headers);
                            break;
                        case HttpMethod.Post:
                            returntask = jsonHCS.PostToJObjectAsync(fullroute, postArgument, headers);
                            break;
                        case HttpMethod.Put:
                            returntask = jsonHCS.PutToJObjectAsync(fullroute, postArgument, headers);
                            break;
                        case HttpMethod.Delete:
                            returntask = jsonHCS.DeleteToJObjectAsync(fullroute, headers);
                            break;
                        default:
                            break;
                    }
                }
                else if (type == ReturnType.Response)
                {
                    switch (method)
                    {
                        case HttpMethod.Get:
                            returntask = jsonHCS.GetRawAsync(fullroute, headers);
                            break;
                        case HttpMethod.Post:
                            returntask = jsonHCS.PostToRawAsync(fullroute, postArgument, headers);
                            break;
                        case HttpMethod.Put:
                            returntask = jsonHCS.PutToRawAsync(fullroute, postArgument, headers);
                            break;
                        case HttpMethod.Delete:
                            returntask = jsonHCS.DeleteToRawAsync(fullroute, headers);
                            break;
                        default:
                            break;
                    }
                }
                else if (type == ReturnType.String)
                {
                    switch (method)
                    {
                        case HttpMethod.Get:
                            returntask = jsonHCS.GetStringAsync(fullroute, headers);
                            break;
                        case HttpMethod.Post:
                            returntask = jsonHCS.PostToStringAsync(fullroute, postArgument, headers);
                            break;
                        case HttpMethod.Put:
                            returntask = jsonHCS.PutToStringAsync(fullroute, postArgument, headers);
                            break;
                        case HttpMethod.Delete:
                            returntask = jsonHCS.DeleteToStringAsync(fullroute, headers);
                            break;
                        default:
                            break;
                    }
                }
            }
            if (isaction)
            {
                if (isgenericaction)
                {
                    returntask = (Task)this.GetType().GetMethod("GetActionResultT").MakeGenericMethod(returnparam).Invoke(this, new object[] { returntask });
                }
                else if (isIAction)
                {
                    returntask = GetIActionResult(returntask as Task<System.Net.Http.HttpResponseMessage>);
                }
                else
                {
                    returntask = GetActionResult(returntask as Task<System.Net.Http.HttpResponseMessage>);
                }
            }
            if (istask)
            {
                invocation.ReturnValue = returntask;
            }
            else if (!noreturn)
            {
                returntask.Wait();
                invocation.ReturnValue = returntask.GetType().GetProperty("Result", BindingFlags.Instance | BindingFlags.Public)?.GetValue(returntask);
            }
        }

        private string GetRoute(IInvocation invocation)
        {
            var route = new List<string> { baseUrl };
            {
                Type target = invocation.TargetType;
                while (target != null)
                {
                    route.Add(GetRoute(target.GetTypeInfo()));
                    target = (target.IsNested) ? target = target.DeclaringType : target = null;
                }
            }
            route.Add(GetRoute(invocation.Method));
            var fullroute = string.Join("/", route.Where(s => s != null).Select(s => s.Trim('/')));
            fullroute = fullroute.Replace("[controller]", invocation.TargetType.Name.Replace("Controller", ""));
            return fullroute;
        }

        static string GetRoute(MemberInfo data)
        {
            var route = data.GetCustomAttributes(true).FirstOrDefault(a => (a.GetType().Name == "RouteAttribute" || a.GetType().Name == "HttpGetAttribute" || a.GetType().Name == "HttpPostAttribute" || a.GetType().Name == "HttpPutAttribute" || a.GetType().Name == "HttpDeleteAttribute"));
            return route?.GetType().GetProperty("Template")?.GetValue(route) as string;
        }

        Task ConvertTask(Task<object> task, Type type)
        {
            return (Task)this.GetType().GetMethod("Convert").MakeGenericMethod(type).Invoke(null, new object[] { task });
        }

        public async static Task<T> Convert<T>(Task<object> task)
        {
            var result = await task;
            return (T)result;
        }

        static object GetParameterValue(ParameterInfo p, object[] args)
        {
            return (args.Length > p.Position ? args[p.Position] : null ?? p.DefaultValue);
        }

        static T GetAttribute<T>(MemberInfo data) where T : Attribute
        {
            return (T)data.GetCustomAttributes(typeof(T), false).FirstOrDefault();
        }

        static bool HasAttribute(ICustomAttributeProvider data, Type type)
        {
            return data.GetCustomAttributes(type, false).FirstOrDefault() != null;
        }

        static IEnumerable<object> GetAttributes(ICustomAttributeProvider data, string name)
        {
            return data.GetCustomAttributes(false).Where(a => a.GetType().Name == name);
        }

        static object GetAttribute(ICustomAttributeProvider data, string name)
        {
            return GetAttributes(data, name).FirstOrDefault();
        }

        static bool HasAttribute(ICustomAttributeProvider data, string name)
        {
            return GetAttribute(data, name) != null;
        }

        enum HttpMethod { Get, Post, Put, Delete }
        enum ReturnType { String, Response, JObject, Data, Object }
        enum SourceType { Body, Form, Header, Query, Route, Services }

        static HttpMethod FindHttpMethod(ICustomAttributeProvider data, List<Tuple<string, SourceType, object>> parameters)
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
            if (parameters.Any(p => p.Item2 == SourceType.Body || p.Item2 == SourceType.Form))
            {
                return HttpMethod.Post;
            }
            return HttpMethod.Get;
        }

        static ReturnType FindReturnType(ICustomAttributeProvider data)
        {
            if (HasAttribute(data, typeof(RawStringAttribute)))
            {
                return ReturnType.String;
            }
            else if (HasAttribute(data, typeof(RawDataAttribute)))
            {
                return ReturnType.Data;
            }
            else if (HasAttribute(data, typeof(RawResponseAttribute)))
            {
                return ReturnType.Response;
            }
            else if (HasAttribute(data, typeof(RawJObjectAttribute)))
            {
                return ReturnType.JObject;
            }
            else
            {
                return ReturnType.Object;
            }
        }

        static Tuple<string, SourceType, object> FindParameterTypeAndName(ParameterInfo parameter, string[] queryParameters, object[] values)
        {
            if (HasAttribute(parameter, "FromBodyAttribute"))
            {
                return Tuple.Create(parameter.Name, SourceType.Body, GetParameterValue(parameter, values));
            }
            if (HasAttribute(parameter, "FromFormAttribute"))
            {
                return Tuple.Create(GetParameterName("FromFormAttribute", parameter), SourceType.Form, GetParameterValue(parameter, values));
            }
            if (HasAttribute(parameter, "FromHeaderAttribute"))
            {
                return Tuple.Create(GetParameterName("FromHeaderAttribute", parameter), SourceType.Header, GetParameterValue(parameter, values));
            }
            if (HasAttribute(parameter, "FromQueryAttribute"))
            {
                return Tuple.Create(GetParameterName("FromQueryAttribute", parameter), SourceType.Query, GetParameterValue(parameter, values));
            }
            if (HasAttribute(parameter, "FromRouteAttribute"))
            {
                return Tuple.Create(GetParameterName("FromRouteAttribute", parameter), SourceType.Route, GetParameterValue(parameter, values));
            }
            if (HasAttribute(parameter, "FromServicesAttribute"))
            {
                return Tuple.Create(parameter.Name, SourceType.Services, GetParameterValue(parameter, values));
            }
            //implicit
            if (!IsSimple(parameter.ParameterType))
            {
                return Tuple.Create(parameter.Name, SourceType.Body, GetParameterValue(parameter, values));
            }
            if (queryParameters.Contains(parameter.Name))
            {
                return Tuple.Create(parameter.Name, SourceType.Route, GetParameterValue(parameter, values));
            }
            return Tuple.Create(parameter.Name, SourceType.Query, GetParameterValue(parameter, values));
        }

        static string GetParameterName(string attribute, ParameterInfo parameter)
        {
            var a = GetAttribute(parameter, attribute);
            var name = a.GetType().GetProperty("Name")?.GetValue(a) as string ?? parameter.Name;
            return name;
        }

        static bool IsSimple(Type type)
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

        async Task<ApiDefinition.ActionResult> GetActionResult(Task<System.Net.Http.HttpResponseMessage> response)
        {
            return new ActionResultSupport.GenericResult<object>(await response);
        }

        async Task<ApiDefinition.IActionResult> GetIActionResult(Task<System.Net.Http.HttpResponseMessage> response)
        {
            return new ActionResultSupport.GenericResult<object>(await response);
        }

        public async Task<ApiDefinition.ActionResult<T>> GetActionResultT<T>(Task<System.Net.Http.HttpResponseMessage> response)
        {
            return new ActionResultSupport.GenericResult<T>(await response);
        }
    }
}
