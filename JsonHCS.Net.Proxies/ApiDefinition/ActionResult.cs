using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace JsonHCSNet.Proxies.ApiDefinition
{
    public interface IActionResult
    {
        bool IsSuccess { get; }
        HttpStatusCode StatusCode { get; }
        string ReasonPhrase { get; }
        HttpResponseMessage Response { get; }

        Task<T> GetJsonAsync<T>();
        Task<object> GetJsonAsync();
        Task<JObject> GetJObjectAsync();
        Task<string> GetStringAsync();
        Task<Stream> GetStreamAsync();
        Task<MemoryStream> GetMemoryStreamAsync();
    }

    public abstract class ActionResult : IActionResult
    {
        public abstract bool IsSuccess { get; }
        public abstract HttpStatusCode StatusCode { get; }
        public abstract string ReasonPhrase { get; }
        public abstract HttpResponseMessage Response { get; }

        public abstract Task<T> GetJsonAsync<T>();
        public abstract Task<object> GetJsonAsync();
        public abstract Task<JObject> GetJObjectAsync();
        public abstract Task<string> GetStringAsync();
        public abstract Task<Stream> GetStreamAsync();
        public abstract Task<MemoryStream> GetMemoryStreamAsync();
    }

    public abstract class ActionResult<T> : ActionResult
    {
        public abstract Task<T> GetResultAsync();
    }
}
