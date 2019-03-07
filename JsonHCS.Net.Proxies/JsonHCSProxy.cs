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
            var returnparam = invocation.Method.ReturnType;
            Task returntask = null;
            bool istask = typeof(Task).IsAssignableFrom(returnparam);
            //while (GetAllTypes(returnparam).Any(t=> t.Name == "Task" || t.Name == "IActionResult"|| t.Name == "ActionResult"))
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

            var postArgument = GetParameterValues(postParameters, invocation.Arguments).FirstOrDefault();

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

        static string GetRoute(MemberInfo data)
        {
            var route = data.CustomAttributes.FirstOrDefault(a => (a.AttributeType.Name == "RouteAttribute" || a.AttributeType.Name == "HttpGetAttribute" || a.AttributeType.Name == "HttpPostAttribute" || a.AttributeType.Name == "HttpPutAttribute" || a.AttributeType.Name == "HttpDeleteAttribute") && a.ConstructorArguments.Count > 0 && a.ConstructorArguments.First().ArgumentType == typeof(string));
            return route?.ConstructorArguments.First().Value as string;
        }

        //static IEnumerable<Type> GetAllTypes(Type type)
        //{
        //    Type baseType = type.GetTypeInfo().BaseType;
        //    if (baseType != null)
        //    {
        //        foreach (var t in GetAllTypes(baseType))
        //        {
        //            yield return t;
        //        }
        //    }
        //    foreach (var t in type.GetInterfaces().SelectMany(i => GetAllTypes(i)))
        //    {
        //        yield return t;
        //    }
        //}
    }
}
