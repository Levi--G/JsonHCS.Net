using System;
using System.Collections.Generic;
using System.Text;

namespace JsonHCSNet.Proxies.ApiDefinition
{
    [AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    class ApiControllerAttribute : Attribute
    {
    }
}
