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

            ParameterInfo[] parameters = invocation.Method.GetParameters();
            var postParameters = parameters.Where(p => HasAttribute(p, "FromBodyAttribute")).ToArray();
            var routeparams = Regex.Matches(fullroute, @"\{.+\}", RegexOptions.Compiled).OfType<Match>().Select(m => m.Value.Substring(1, m.Value.Length - 2)).ToArray();
            var routeParameters = parameters.Except(postParameters).Where(p => routeparams.Contains(p.Name)).ToArray();
            var queryParameters = parameters.Except(postParameters).Except(routeParameters).ToArray();

            object postArgument;
            if (postParameters.Length < 2)
            {
                postArgument = GetParameterValues(postParameters, invocation.Arguments).FirstOrDefault();
            }
            else
            {
                postArgument = GetParameterValues(postParameters, invocation.Arguments).Select((p, i) => Tuple.Create(postParameters[i].Name, p)).ToDictionary(t => t.Item1, t => t.Item2);
            }

            if (routeParameters != null && routeParameters.Length > 0)
            {
                foreach (var param in routeParameters)
                {
                    var value = GetParameterValue(param, invocation.Arguments).ToString();
                    fullroute = fullroute.Replace($"{{{param.Name}}}", value);
                }
            }

            if (queryParameters != null && queryParameters.Length > 0)
            {
                fullroute += "?" + string.Join("&", queryParameters.Select(q =>
                {
                    var v = GetParameterValue(q, invocation.Arguments);
                    if (v == null) { return null; }
                    return $"{q.Name}={System.Net.WebUtility.UrlEncode(v.ToString())}";
                }).Where(s => s != null));
            }

            Task returntask = null;

            var method = FindHttpMethod(invocation.Method);
            if (noreturn)
            {
                switch (method)
                {
                    case HttpMethod.Get:
                        returntask = jsonHCS.GetRawAsync(fullroute);
                        break;
                    case HttpMethod.Post:
                        returntask = jsonHCS.PostAsync(fullroute, postArgument);
                        break;
                    case HttpMethod.Put:
                        returntask = jsonHCS.PutAsync(fullroute, postArgument);
                        break;
                    case HttpMethod.Delete:
                        returntask = jsonHCS.DeleteAsync(fullroute);
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
                            returntask = ConvertTask(jsonHCS.GetJsonAsync(fullroute, returnparam), returnparam);
                            break;
                        case HttpMethod.Post:
                            returntask = ConvertTask(jsonHCS.PostToJsonAsync(fullroute, postArgument, returnparam), returnparam);
                            break;
                        case HttpMethod.Put:
                            returntask = ConvertTask(jsonHCS.PutToJsonAsync(fullroute, postArgument, returnparam), returnparam);
                            break;
                        case HttpMethod.Delete:
                            returntask = ConvertTask(jsonHCS.DeleteToJsonAsync(fullroute, returnparam), returnparam);
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
                            returntask = jsonHCS.GetJObjectAsync(fullroute);
                            break;
                        case HttpMethod.Post:
                            returntask = jsonHCS.PostToJObjectAsync(fullroute, postArgument);
                            break;
                        case HttpMethod.Put:
                            returntask = jsonHCS.PutToJObjectAsync(fullroute, postArgument);
                            break;
                        case HttpMethod.Delete:
                            returntask = jsonHCS.DeleteToJObjectAsync(fullroute);
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
                            returntask = jsonHCS.GetRawAsync(fullroute);
                            break;
                        case HttpMethod.Post:
                            returntask = jsonHCS.PostToRawAsync(fullroute, postArgument);
                            break;
                        case HttpMethod.Put:
                            returntask = jsonHCS.PutToRawAsync(fullroute, postArgument);
                            break;
                        case HttpMethod.Delete:
                            returntask = jsonHCS.DeleteToRawAsync(fullroute);
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
                            returntask = jsonHCS.GetStringAsync(fullroute);
                            break;
                        case HttpMethod.Post:
                            returntask = jsonHCS.PostToStringAsync(fullroute, postArgument);
                            break;
                        case HttpMethod.Put:
                            returntask = jsonHCS.PutToStringAsync(fullroute, postArgument);
                            break;
                        case HttpMethod.Delete:
                            returntask = jsonHCS.DeleteToStringAsync(fullroute);
                            break;
                        default:
                            break;
                    }
                }
            }
            if (istask)
            {
                invocation.ReturnValue = returntask;
            }
            else if (!noreturn)
            {
                returntask.Wait();
                invocation.ReturnValue = returntask.GetType().GetProperty("Result", BindingFlags.Public).GetValue(returntask);
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

        static IEnumerable<object> GetParameterValues(ParameterInfo[] parameters, object[] args)
        {
            return parameters.Select(p => GetParameterValue(p, args));
        }

        static object GetParameterValue(ParameterInfo p, object[] args)
        {
            return (args.Length > p.Position ? args[p.Position] : null ?? p.DefaultValue);
        }

        static T GetAttribute<T>(MemberInfo data) where T : Attribute
        {
            return (T)data.GetCustomAttributes(typeof(T)).FirstOrDefault();
        }

        static bool HasAttribute(ICustomAttributeProvider data, string name)
        {
            return data.GetCustomAttributes(false).FirstOrDefault(a => a.GetType().Name == name) != null;
        }

        static bool HasAttribute(ICustomAttributeProvider data, Type type)
        {
            return data.GetCustomAttributes(type, false).FirstOrDefault() != null;
        }

        enum HttpMethod { Get, Post, Put, Delete }
        enum ReturnType { String, Response, JObject, Data, Object }

        static HttpMethod FindHttpMethod(ICustomAttributeProvider data)
        {
            if (HasAttribute(data, "HttpPostAttribute"))
            {
                return HttpMethod.Post;
            }
            else if (HasAttribute(data, "HttpPutAttribute"))
            {
                return HttpMethod.Put;
            }
            else if (HasAttribute(data, "HttpDeleteAttribute"))
            {
                return HttpMethod.Delete;
            }
            else
            {
                return HttpMethod.Get;
            }
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
    }
}
