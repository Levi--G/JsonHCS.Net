using System;
using System.Collections.Generic;
using System.Text;

namespace JsonHCSNet.Proxies.SignalR
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public partial class HubMethodAttribute : Attribute
    {
        public string MethodName { get; set; }

        public SendType Type { get; set; }

        public HubMethodAttribute(string MethodName = null, SendType Type = SendType.Invoke)
        {
            this.MethodName = MethodName;
            this.Type = Type;
        }
    }
}
