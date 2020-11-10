using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace JsonHCSNet
{
    public interface IRequestMiddleware
    {
        Task<HttpRequestMessage> HandleRequestAsync(JsonHCS jsonHCS, HttpRequestMessage request);
    }
}