using System;
using System.Collections.Generic;
using System.Text;

namespace JsonHCSNet.Proxies.ApiDefinition
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public class RouteAttribute : Attribute
    {
        public string Template { get; set; }

        public RouteAttribute(string template)
        {
            Template = template;
        }

        public RouteAttribute() { }
    }

    public class HttpGetAttribute : RouteAttribute
    {
        public HttpGetAttribute(string template) : base(template)
        {
        }

        public HttpGetAttribute() { }
    }

    public class HttpPostAttribute : RouteAttribute
    {
        public HttpPostAttribute(string template) : base(template)
        {
        }

        public HttpPostAttribute() { }
    }

    public class HttpPutAttribute : RouteAttribute
    {
        public HttpPutAttribute(string template) : base(template)
        {
        }

        public HttpPutAttribute() { }
    }

    public class HttpDeleteAttribute : RouteAttribute
    {
        public HttpDeleteAttribute(string template) : base(template)
        {
        }

        public HttpDeleteAttribute() { }
    }
}
