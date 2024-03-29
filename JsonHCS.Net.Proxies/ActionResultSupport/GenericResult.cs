﻿using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace JsonHCSNet.Proxies.ActionResultSupport
{
    class GenericResult<T> : ApiDefinition.ActionResult<T>
    {
        JsonHCS jsonHCS;

        public GenericResult(HttpResponseMessage Response, JsonHCS jsonHCS)
        {
            response = Response;
            this.jsonHCS = jsonHCS;
        }

        public override bool IsSuccess => Response.IsSuccessStatusCode;

        public override HttpStatusCode StatusCode => Response.StatusCode;

        public override string ReasonPhrase => Response.ReasonPhrase;

        HttpResponseMessage response;

        public override HttpResponseMessage Response => response;

        public override async Task<JObject> GetJObjectAsync()
        {
            return jsonHCS.DeserializeJObject(await GetStringAsync().ConfigureAwait(false));
        }

        public override async Task<T1> GetJsonAsync<T1>()
        {
            return jsonHCS.DeserializeJson<T1>(await GetStringAsync().ConfigureAwait(false));
        }

        public override async Task<object> GetJsonAsync()
        {
            return jsonHCS.DeserializeJson(await GetStringAsync().ConfigureAwait(false));
        }

        public override Task<T> GetResultAsync()
        {
            return GetJsonAsync<T>();
        }

        public override Task<string> GetStringAsync()
        {
            return JsonHCS.ReadContentAsString(Response);
        }

        public override async Task<Stream> GetStreamAsync()
        {
            if (Response?.Content == null) { return null; }
            return await Response.Content.ReadAsStreamAsync().ConfigureAwait(false);
        }

        public override async Task<MemoryStream> GetMemoryStreamAsync()
        {
            using (var result = await GetStreamAsync().ConfigureAwait(false))
            {
                if (result == null) { return null; }
                var s = new MemoryStream();
                await result.CopyToAsync(s).ConfigureAwait(false);
                s.Seek(0, SeekOrigin.Begin);
                return s;
            }
        }
    }
}
