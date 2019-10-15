using Castle.DynamicProxy;
using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;

namespace JsonHCSNet.Proxies.SignalR
{
    class HubConnectionProxy : IInterceptor
    {
        HubConnection connection;
        SendType type;

        public HubConnectionProxy(HubConnection connection, SendType type)
        {
            this.connection = connection;
            this.type = type;
        }

        public void Intercept(IInvocation invocation)
        {
            var att = invocation.Method.GetCustomAttributes(typeof(HubMethodAttribute), false).FirstOrDefault() as HubMethodAttribute;
            var methodname = att?.MethodName ?? invocation.Method.Name;
            if (type == SendType.Send)
            {
                invocation.ReturnValue = connection.SendCoreAsync(methodname, invocation.Arguments);
            }
            else if (type == SendType.Invoke)
            {
                if (invocation.TargetType == typeof(void))
                {
                    invocation.ReturnValue = connection.InvokeCoreAsync(methodname, invocation.Arguments);
                }
                else
                {
                    invocation.ReturnValue = connection.InvokeCoreAsync(methodname, invocation.TargetType, invocation.Arguments);
                }
            }
            else
            {
                throw new NotSupportedException("The chosen SendType was not supported");
            }
        }
    }
}
