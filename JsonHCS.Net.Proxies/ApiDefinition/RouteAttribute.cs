using System;
using System.Collections.Generic;
using System.Text;

namespace JsonHCSNet.Proxies.ApiDefinition
{
    [AttributeUsage(AttributeTargets.Method)]
    public class RouteAttribute : Attribute
    {
        public string Template { get; set; }

        public RouteAttribute(string template)
        {
            Template = template;
        }
    }

    public class HttpGetAttribute : RouteAttribute
    {
        public HttpGetAttribute(string template) : base(template)
        {
        }
    }

    public class HttpPostAttribute : RouteAttribute
    {
        public HttpPostAttribute(string template) : base(template)
        {
        }
    }

    public class HttpPutAttribute : RouteAttribute
    {
        public HttpPutAttribute(string template) : base(template)
        {
        }
    }

    public class HttpDeleteAttribute : RouteAttribute
    {
        public HttpDeleteAttribute(string template) : base(template)
        {
        }
    }
}
