using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace JsonHCSNet.Proxies.ActionResultSupport
{
    class GenericResult<T> : ApiDefinition.ActionResult<T>
    {
        public GenericResult(HttpResponseMessage Response)
        {
            response = Response;
        }

        public override bool IsSuccess => Response.IsSuccessStatusCode;

        public override HttpStatusCode StatusCode => Response.StatusCode;

        public override string ReasonPhrase => Response.ReasonPhrase;

        HttpResponseMessage response;

        public override HttpResponseMessage Response => response;

        public override async Task<JObject> GetJObjectAsync()
        {
            return DeserializeJObject(await GetStringAsync());
        }

        public override async Task<T1> GetJsonAsync<T1>()
        {
            return DeserializeJson<T1>(await GetStringAsync());
        }

        public override async Task<object> GetJsonAsync()
        {
            return DeserializeJson(await GetStringAsync());
        }

        public override Task<T> GetResultAsync()
        {
            return GetJsonAsync<T>();
        }

        public override Task<string> GetStringAsync()
        {
            return GetStringInternal(Response);
        }

        private static async Task<string> GetStringInternal(HttpResponseMessage response)
        {
            var result = response?.Content;
            if (result == null) { return null; }
            return await result.ReadAsStringAsync();
        }

        private object DeserializeJson(string json, Type type = null)
        {
            if (json == null) { return null; }
            return JsonConvert.DeserializeObject(json, type);
        }

        private T DeserializeJson<T>(string json)
        {
            if (json == null) { return default(T); }
            return JsonConvert.DeserializeObject<T>(json);
        }

        private JObject DeserializeJObject(string json)
        {
            if (json == null) { return null; }
            return JObject.Parse(json);
        }
    }
}
