using System;
using System.Collections.Generic;
using System.Text;
using Castle.DynamicProxy;

namespace JsonHCSNet.Proxies
{
    public class JsonHCSProxyGenerator
    {
        readonly JsonHCS jsonHCS;
        ProxyGenerator proxyGenerator;

        public JsonHCSProxyGenerator(JsonHCS jsonHCS = null)
        {
            this.jsonHCS = jsonHCS ?? new JsonHCS(true);
            proxyGenerator = new ProxyGenerator();
        }

        public JsonHCSProxyGenerator(JsonHCS_Settings settings) : this(new JsonHCS(settings)) { }

        public T CreateClassProxy<T>(string baseUrl = null) where T : class
        {
            return proxyGenerator.CreateClassProxy<T>(new JsonHCSProxy(jsonHCS, baseUrl));
        }

        public T CreateInterfaceProxyWithoutTarget<T>(string baseUrl = null) where T : class
        {
            return proxyGenerator.CreateInterfaceProxyWithoutTarget<T>(new JsonHCSProxy(jsonHCS, baseUrl));
        }
    }
}
