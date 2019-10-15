using Castle.DynamicProxy;
using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace JsonHCSNet.Proxies.SignalR
{
    class ListenerProxy : IInterceptor
    {
        HubConnection connection;

        public ListenerProxy(HubConnection connection)
        {
            this.connection = connection;
        }

        object cacheKey = new object();
        Dictionary<string, Invocation> cache = new Dictionary<string, Invocation>();

        enum Method { add, remove }

        public void Intercept(IInvocation invocation)
        {
            //TODO: IAsyncEnumerable support
            var returntype = invocation.Method.ReturnType;
            var name = invocation.Method.GetCustomAttribute<HubMethodAttribute>()?.MethodName;
            string methodname = null;
            if (invocation.Method.Name.StartsWith("get_") || invocation.Method.Name.StartsWith("set_") || invocation.Method.Name.StartsWith("add_") || invocation.Method.Name.StartsWith("remove_"))
            {
                var split = invocation.Method.Name.Split(new[] { '_' }, 2);
                methodname = split.First();
                if(name == null)
                {
                    var prop = split.Last();
                    name = (invocation.TargetType ?? invocation.Method.ReflectedType)?.GetProperty(prop)?.GetCustomAttribute<HubMethodAttribute>()?.MethodName;
                }
                name = name ?? split.Last();
            }
            else
            {
                name = name ?? invocation.Method.Name;
            }
            if (returntype != null && returntype.IsConstructedGenericType && returntype.GetGenericTypeDefinition() == typeof(IObservable<>) && returntype.GenericTypeArguments.Length == 1)
            {
                var t = returntype.GenericTypeArguments.Single();
                invocation.ReturnValue = Activator.CreateInstance(typeof(HubObservable<>).MakeGenericType(t), name, (Action<string, Type[], Delegate>)AddOrCreate, (Action<string, Type[], Delegate>)RemoveOrDelete);
            }
            else if (invocation.Method.Name.StartsWith("add_") || invocation.Method.Name.StartsWith("remove_"))
            {
                if (!Enum.TryParse(methodname, out Method method))
                {
                    return;
                }
                var inv = invocation.Arguments.First();
                Type[] types = inv.GetType().GenericTypeArguments;
                if (types.Last() == typeof(Task))
                {
                    var args = types.Take(types.Length - 1).ToArray();
                    var del = inv as Delegate;
                    if (method == Method.add)
                    {
                        AddOrCreate(name, args, del);
                    }
                    else
                    {
                        RemoveOrDelete(name, args, del);
                    }
                }
            }
        }

        Task Invoke(object[] o, object id)
        {
            Invocation i;
            lock (cacheKey)
            {
                cache.TryGetValue((string)id, out i);
            }
            var task = i?.Delegate?.DynamicInvoke(o) as Task;
            if (task != null)
            {
                return task;
            }
            return Task.CompletedTask;
        }

        void AddOrCreate(string name, Type[] args, Delegate d)
        {
            var id = GetId(name, args);
            lock (cacheKey)
            {
                if (!cache.TryGetValue(id, out var inv))
                {
                    cache[id] = new Invocation()
                    {
                        Delegate = null,
                        Register = connection.On(name, args, Invoke, id)
                    };
                }
                cache[id].Delegate = Delegate.Combine(cache[id].Delegate, d);
            }
        }

        void RemoveOrDelete(string name, Type[] args, Delegate d)
        {
            var id = GetId(name, args);
            lock (cacheKey)
            {
                if (cache.TryGetValue(id, out var inv))
                {
                    cache[id].Delegate = Delegate.Remove(cache[id].Delegate, d);
                    if (cache[id].Delegate == null)
                    {
                        cache[id].Register.Dispose();
                        cache.Remove(id);
                    }
                }
            }
        }

        private static string GetId(string name, Type[] args)
        {
            return $"{name} ({string.Join(",", args.Select(t => t.FullName))})";
        }
    }

    class Invocation
    {
        public Delegate Delegate { get; set; }
        public IDisposable Register { get; set; }
    }
}
