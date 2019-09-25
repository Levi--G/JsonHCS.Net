using Castle.DynamicProxy;
using JsonHCSNet.Proxies.Plugins;
using System;
using System.Collections.Generic;
using System.Text;

namespace JsonHCSNet.Proxies
{
    public class JsonHCSProxyGenerator
    {
        readonly JsonHCS jsonHCS;
        ProxyGenerator proxyGenerator;
        PluginManager pluginManager;

        public JsonHCSProxyGenerator(JsonHCS jsonHCS = null, params IProxyPlugin[] plugins)
        {
            this.jsonHCS = jsonHCS ?? new JsonHCS(true);
            if (plugins.Length == 0)
            {
                plugins = new IProxyPlugin[] { new ActionResultPlugin(), new BasicPlugin() };
            }
            proxyGenerator = new ProxyGenerator();
            pluginManager = new PluginManager(plugins);
        }

        public JsonHCSProxyGenerator(JsonHCS_Settings settings) : this(new JsonHCS(settings)) { }

        public T CreateClassProxy<T>(string baseUrl = null, T target = null) where T : class
        {
            if (target == null)
            {
                return proxyGenerator.CreateClassProxy<T>(new JsonHCSProxy(pluginManager, jsonHCS, baseUrl));
            }
            else
            {
                return proxyGenerator.CreateClassProxyWithTarget(target, new JsonHCSProxy(pluginManager, jsonHCS, baseUrl));
            }
        }

        public T CreateInterfaceProxy<T>(string baseUrl = null, T target = null) where T : class
        {
            if (target == null)
            {
                return proxyGenerator.CreateInterfaceProxyWithoutTarget<T>(new JsonHCSProxy(pluginManager, jsonHCS, baseUrl));
            }
            else
            {
                return proxyGenerator.CreateInterfaceProxyWithTarget(target, new JsonHCSProxy(pluginManager, jsonHCS, baseUrl));
            }
        }

        [Obsolete]
        public T CreateInterfaceProxyWithoutTarget<T>(string baseUrl = null) where T : class
        {
            return CreateInterfaceProxy<T>(baseUrl);
        }
    }
}
