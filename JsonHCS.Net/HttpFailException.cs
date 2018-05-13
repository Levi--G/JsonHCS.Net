using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;

namespace JsonHCSNet
{
    public class HttpFailException : Exception
    {
        public HttpResponseMessage Response { get; private set; }

        public HttpFailException(HttpResponseMessage response, string message = "Http response did not indicate success!", Exception innerException = null) : base(message, innerException)
        {
            Response = response;
        }
    }
}
