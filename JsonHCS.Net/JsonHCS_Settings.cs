using System;
using System.Net.Http;
using System.Net.Http.Headers;

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

        public Func<JsonHCS_Settings, HttpClientHandler> ClientHandlerFactory { get; set; } = (settings) => { return new HttpClientHandler(); };

        public Func<JsonHCS_Settings, HttpClientHandler, HttpClient> ClientFactory { get; set; } = (settings, handler) => { return new HttpClient(handler); };
    }
}