using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;

namespace JsonHCSNet
{
    public class JsonHCS_Settings
    {
        /// <summary>
        /// Time in ms before the requests time out
        /// </summary>
        public int Timeout { get; set; } = -1;

        /// <summary>
        /// Adds some default accept headers common to Json requests
        /// </summary>
        public bool AddDefaultAcceptHeaders { get; set; }

        /// <summary>
        /// Adds json-only accept headers (use for ASP.NET core)
        /// </summary>
        public bool AddJsonAcceptHeaders { get; set; }

        public string Host { get; set; }

        public string AcceptLanguage { get; set; }

        public string UserAgent { get; set; } = "JsonHCS.Net";

        public string Referer { get; set; }

        public string Origin { get; set; }

        /// <summary>
        /// Gets or sets the base address of the requests.
        /// </summary>
        public string BaseAddress { get; set; }

        /// <summary>
        /// Enables support for cookies (like sessions) to be stored and resent with requests
        /// </summary>
        public bool CookieSupport { get; set; }

        /// <summary>
        /// Enables underlying UseDefaultCredentials support
        /// </summary>
        public bool? UseDefaultCredentials { get; set; }

        /// <summary>
        /// Throws an Exception upon bad return codes instead of returning null
        /// </summary>
        public bool ThrowOnFail { get; set; }

        /// <summary>
        /// Catches any errors occuring, handy for fire-and-forget situations
        /// </summary>
        public bool CatchErrors { get; set; } = true;

        public JsonSerializerSettings JsonDecodingSettings { get; set; }

        public JsonSerializerSettings JsonEncodingSettings { get; set; }

        public Func<JsonHCS_Settings, HttpClientHandler> ClientHandlerFactory { get; set; } = (settings) => { return new HttpClientHandler(); };

        public Func<JsonHCS_Settings, HttpClientHandler, HttpClient> ClientFactory { get; set; } = (settings, handler) => { return new HttpClient(handler); };

        /// <summary>
        /// Contains middlewares to handle requests before being executed
        /// </summary>
        public IList<IRequestMiddleware> RequestMiddlewares { get; set; } = new List<IRequestMiddleware>();

        /// <summary>
        /// Contains middlewares to handle responses after the requests were executed
        /// </summary>
        public IList<IResponseMiddleware> ResponseMiddlewares { get; set; } = new List<IResponseMiddleware>();
    }
}