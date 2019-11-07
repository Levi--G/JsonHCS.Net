﻿using Newtonsoft.Json;
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
        JsonHCS jsonHCS;

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
            return jsonHCS.DeserializeJObject(await GetStringAsync());
        }

        public override async Task<T1> GetJsonAsync<T1>()
        {
            return jsonHCS.DeserializeJson<T1>(await GetStringAsync());
        }

        public override async Task<object> GetJsonAsync()
        {
            return jsonHCS.DeserializeJson(await GetStringAsync());
        }

        public override Task<T> GetResultAsync()
        {
            return GetJsonAsync<T>();
        }

        public override Task<string> GetStringAsync()
        {
            return JsonHCS.ReadContentAsString(Response);
        }
    }
}
