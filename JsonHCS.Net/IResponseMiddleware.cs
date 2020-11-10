using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace JsonHCSNet
{
    public interface IResponseMiddleware
    {
        Task<HttpResponseMessage> HandleResponseAsync(JsonHCS jsonHCS, HttpRequestMessage request, HttpResponseMessage response);
    }
}