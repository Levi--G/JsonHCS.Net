using System;
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

        internal void Apply(JsonHCS jsonclient)
        {
            var client = jsonclient.Client;
            if (Timeout != -1)
            {
                client.Timeout = TimeSpan.FromMilliseconds(Timeout);
            }
            if (AddDefaultAcceptHeaders)
            {
                client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("text/javascript"));
                client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("text/html"));
                client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("*/*"));
            }
            if (Host != null)
            {
                client.DefaultRequestHeaders.Host = Host;
            }
            if (AcceptLanguage != null)
            {
                client.DefaultRequestHeaders.Add("Accept-Language", AcceptLanguage);
            }
            if (UserAgent != null)
            {
                client.DefaultRequestHeaders.Add("User-Agent", UserAgent);
            }
            if (Referer != null)
            {
                client.DefaultRequestHeaders.Add("Referer", Referer);
            }
            if (Origin != null)
            {
                client.DefaultRequestHeaders.Add("Origin", Origin);
            }
            if (BaseAddress != null)
            {
                client.BaseAddress = new Uri(BaseAddress);
            }
        }
    }
}