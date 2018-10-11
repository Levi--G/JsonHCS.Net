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
        string baseUrl;

        internal JsonHCSProxy(JsonHCS jsonHCS, string baseUrl)
        {
            this.jsonHCS = jsonHCS;
            this.baseUrl = baseUrl;
        }

        public void Intercept(IInvocation invocation)
        {
            var route = new List<string>();
            route.Add(baseUrl);
            Type target = invocation.TargetType;
            while (target != null)
            {
                route.Add(GetRoute(target.GetTypeInfo()));
                target = (target.IsNested) ? target = target.DeclaringType : target = null;
            }
            route.Add(GetRoute(invocation.Method) /*?? invocation.Method.Name*/);
            var fullroute = string.Join("/", route.Where(s => s != null).Select(s => s.Trim('/')));

            var returnparam = invocation.Method.ReturnType;
            Task returntask = null;
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
            //var queryParameters = parameters.Where(p => HasAttribute(p, "FromQueryAttribute"));
            var routeparams = Regex.Matches(fullroute, @"\{.+\}", RegexOptions.Compiled).OfType<Match>().Select(m => m.Value.Substring(1, m.Value.Length - 2)).ToArray();
            var routeParameters = parameters.Except(postParameters).Where(p => routeparams.Contains(p.Name)).ToArray();
            var queryParameters = parameters.Except(postParameters).Except(routeParameters).ToArray();

            var postArgument = GetParameterValues(postParameters, invocation.Arguments).SingleOrDefault();

            if (routeParameters != null && routeParameters.Length > 0)
            {
                foreach (var param in routeParameters)
                {
                    var value = GetParameterValue(param, invocation.Arguments);
                    fullroute = fullroute.Replace($"{{{param.Name}}}", value);
                }
            }

            if (queryParameters != null && queryParameters.Length > 0)
            {
                fullroute += "?" + string.Join("&", GetParameterValues(queryParameters, invocation.Arguments));
            }

            if (HasAttribute(invocation.Method, "HttpPostAttribute"))
            {
                if (noreturn)
                {
                    returntask = jsonHCS.PostAsync(fullroute, postArgument);
                }
                else
                {
                    returntask = (Task)jsonHCS.GetType().GetMethods().Single(m => m.Name == "PostToJsonAsync" && m.ContainsGenericParameters == true).MakeGenericMethod(returnparam).Invoke(jsonHCS, new[] { fullroute, postArgument });
                }
            }
            else if (HasAttribute(invocation.Method, "HttpPutAttribute"))
            {
                if (noreturn)
                {
                    returntask = jsonHCS.PutAsync(fullroute, postArgument);
                }
                else
                {
                    returntask = (Task)jsonHCS.GetType().GetMethods().Single(m => m.Name == "PutToJsonAsync" && m.ContainsGenericParameters == true).MakeGenericMethod(returnparam).Invoke(jsonHCS, new[] { fullroute, postArgument });
                }
            }
            else if (HasAttribute(invocation.Method, "HttpDeleteAttribute"))
            {
                if (noreturn)
                {
                    returntask = jsonHCS.DeleteAsync(fullroute);
                }
                else
                {
                    returntask = (Task)jsonHCS.GetType().GetMethods().Single(m => m.Name == "DeleteToJsonAsync" && m.ContainsGenericParameters == true).MakeGenericMethod(returnparam).Invoke(jsonHCS, new[] { fullroute });
                }
            }
            else
            {
                if (noreturn)
                {
                    returntask = jsonHCS.GetRawAsync(fullroute);
                }
                else
                {
                    returntask = (Task)jsonHCS.GetType().GetMethods().Single(m => m.Name == "GetJsonAsync" && m.ContainsGenericParameters == true).MakeGenericMethod(returnparam).Invoke(jsonHCS, new[] { fullroute });
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

        static IEnumerable<string> GetParameterValues(ParameterInfo[] parameters, object[] args)
        {
            return parameters.Select(p => GetParameterValue(p, args));
        }

        static string GetParameterValue(ParameterInfo p, object[] args)
        {
            return (args.Length > p.Position ? args[p.Position] : null ?? p.DefaultValue).ToString();
        }

        static T GetAttribute<T>(MemberInfo data) where T : Attribute
        {
            return (T)data.GetCustomAttributes(typeof(T)).FirstOrDefault();
        }

        static bool HasAttribute(ICustomAttributeProvider data, string name)
        {
            return data.GetCustomAttributes(false).FirstOrDefault(a => a.GetType().Name == name) != null;
        }

        static string GetRoute(MemberInfo data)
        {
            return data.CustomAttributes.FirstOrDefault(a => a.AttributeType.Name == "RouteAttribute")?.ConstructorArguments.FirstOrDefault().Value as string
                ?? data.CustomAttributes.FirstOrDefault(a => a.AttributeType.Name == "HttpGetAttribute")?.ConstructorArguments.FirstOrDefault().Value as string;
        }
    }
}
