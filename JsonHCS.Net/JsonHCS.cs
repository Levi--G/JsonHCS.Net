﻿using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace JsonHCSNet
{
    public class JsonHCS : IDisposable
    {
        /// <summary>
        /// Gets the uncerlying HttpClientHandler, USE AT OWN RISK
        /// </summary>
        public HttpClientHandler Handler { get; private set; }

        /// <summary>
        /// Gets the uncerlying HttpClient, USE AT OWN RISK
        /// </summary>
        public HttpClient Client { get; private set; }

        protected JsonHCS_Settings Settings { get; set; }

        public event EventHandler<Exception> OnErrorCaught;

        private List<IRequestMiddleware> requestMiddlewares;
        private List<IResponseMiddleware> responseMiddlewares;

        #region Constructors

        /// <summary>
        /// Makes a JsonHCS with default settings and optional cookie support
        /// </summary>
        /// <param name="cookieSupport">Enable cookie support</param>
        public JsonHCS(bool cookieSupport = false) : this(new JsonHCS_Settings() { CookieSupport = cookieSupport })
        {
        }

        public JsonHCS(JsonHCS_Settings settings)
        {
            Settings = settings;

            Handler = settings.ClientHandlerFactory(settings);

            if (settings.CookieSupport)
            {
                Handler.CookieContainer = new CookieContainer();
            }

            if (settings.UseDefaultCredentials.HasValue)
            {
                Handler.UseDefaultCredentials = settings.UseDefaultCredentials.Value;
            }

            Client = settings.ClientFactory(settings, Handler);

            if (settings.Timeout != -1)
            {
                Client.Timeout = TimeSpan.FromMilliseconds(settings.Timeout);
            }
            if (settings.AddDefaultAcceptHeaders)
            {
                Client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
                Client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("text/javascript"));
                Client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("text/html"));
                Client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("*/*"));
            }
            if (settings.AddJsonAcceptHeaders)
            {
                Client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
                Client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("text/json"));
            }
            if (settings.Host != null)
            {
                Client.DefaultRequestHeaders.Host = settings.Host;
            }
            if (settings.AcceptLanguage != null)
            {
                Client.DefaultRequestHeaders.Add("Accept-Language", settings.AcceptLanguage);
            }
            if (settings.UserAgent != null)
            {
                Client.DefaultRequestHeaders.Add("User-Agent", settings.UserAgent);
            }
            if (settings.Referer != null)
            {
                Client.DefaultRequestHeaders.Add("Referer", settings.Referer);
            }
            if (settings.Origin != null)
            {
                Client.DefaultRequestHeaders.Add("Origin", settings.Origin);
            }
            if (settings.BaseAddress != null)
            {
                Client.BaseAddress = new Uri(settings.BaseAddress);
            }
            if (settings.RequestMiddlewares.Count > 0)
            {
                requestMiddlewares = settings.RequestMiddlewares.ToList();
            }
            if (settings.ResponseMiddlewares.Count > 0)
            {
                responseMiddlewares = settings.ResponseMiddlewares.ToList();
            }
        }

        #endregion Constructors

        #region Get

        /// <summary>
        /// Gets the raw HttpResponseMessage if successful else returns null
        /// </summary>
        /// <param name="url">The url to GET</param>
        /// <param name="headers">Optional headers to send with the request</param>
        /// <returns></returns>
        public Task<HttpResponseMessage> GetRawAsync(string url, IEnumerable<KeyValuePair<string, IEnumerable<string>>> headers = null)
        {
            return RunInternalAsync(() =>
            {
                return SendRequestOrFailAsync(HttpMethod.Get, url, null, headers);
            });
        }

        /// <summary>
        /// Gets the responce as string if successful else returns null
        /// </summary>
        /// <param name="url">The url to GET</param>
        /// <returns></returns>
        public Task<string> GetStringAsync(string url, IEnumerable<KeyValuePair<string, IEnumerable<string>>> headers = null)
        {
            return RunInternalAsync(async () =>
            {
                return await ReadContentAsString(await GetRawAsync(url, headers).ConfigureAwait(false)).ConfigureAwait(false);
            });
        }

        /// <summary>
        /// Gets the responce as object if successful else returns null
        /// </summary>
        /// <param name="url">The url to GET</param>
        /// <returns></returns>
        public Task<object> GetJsonAsync(string url, Type type = null, IEnumerable<KeyValuePair<string, IEnumerable<string>>> headers = null)
        {
            return RunInternalAsync(async () =>
            {
                return DeserializeJson(await GetStringAsync(url, headers).ConfigureAwait(false), type);
            });
        }

        /// <summary>
        /// Gets the responce as T if successful else returns the default value
        /// </summary>
        /// <typeparam name="T">The type to convert to</typeparam>
        /// <param name="url">The url to GET</param>
        /// <returns></returns>
        public Task<T> GetJsonAsync<T>(string url, IEnumerable<KeyValuePair<string, IEnumerable<string>>> headers = null)
        {
            return RunInternalAsync(async () =>
            {
                return DeserializeJson<T>(await GetStringAsync(url, headers).ConfigureAwait(false));
            });
        }

        /// <summary>
        /// Gets the responce as JObject if successful else returns null
        /// </summary>
        /// <param name="url">The url to GET</param>
        /// <returns></returns>
        public Task<JObject> GetJObjectAsync(string url, IEnumerable<KeyValuePair<string, IEnumerable<string>>> headers = null)
        {
            return RunInternalAsync(async () =>
            {
                return DeserializeJObject(await GetStringAsync(url, headers).ConfigureAwait(false));
            });
        }

        #endregion Get

        #region Post

        /// <summary>
        /// Posts any HttpContent to the specified url
        /// </summary>
        /// <param name="url">The url to POST</param>
        /// <param name="data">The data to POST</param>
        /// <returns></returns>
        public Task<HttpResponseMessage> PostContentAsync(string url, HttpContent data, IEnumerable<KeyValuePair<string, IEnumerable<string>>> headers = null)
        {
            return RunInternalAsync(() =>
            {
                return SendRequestOrFailAsync(HttpMethod.Post, url, data, headers);
            });
        }

        /// <summary>
        /// Posts the message as Json and gets the responce if successful else returns null
        /// </summary>
        /// <param name="url">The url to POST</param>
        /// <returns></returns>
        public Task<HttpResponseMessage> PostToRawAsync(string url, object data, IEnumerable<KeyValuePair<string, IEnumerable<string>>> headers = null)
        {
            return RunInternalAsync(async () =>
            {
                string json = SerializeJson(data);
                var content = new StringContent(json);
                content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                return await PostContentAsync(url, content, headers).ConfigureAwait(false);
            });
        }

        /// <summary>
        /// Posts the message as Json and gets the responce as string if successful else returns null
        /// </summary>
        /// <param name="url">The url to POST</param>
        /// <returns></returns>
        public Task<string> PostToStringAsync(string url, object data, IEnumerable<KeyValuePair<string, IEnumerable<string>>> headers = null)
        {
            return RunInternalAsync(async () =>
            {
                return await ReadContentAsString(await PostToRawAsync(url, data, headers).ConfigureAwait(false)).ConfigureAwait(false);
            });
        }

        /// <summary>
        /// Posts the message as Json
        /// </summary>
        /// <param name="url">The url to POST</param>
        /// <returns></returns>
        public Task PostAsync(string url, object data, IEnumerable<KeyValuePair<string, IEnumerable<string>>> headers = null)
        {
            return PostToRawAsync(url, data, headers);
        }

        /// <summary>
        /// Posts the message as Json and gets the responce as object if successful else returns null
        /// </summary>
        /// <param name="url">The url to POST</param>
        /// <returns></returns>
        public Task<object> PostToJsonAsync(string url, object data, Type type = null, IEnumerable<KeyValuePair<string, IEnumerable<string>>> headers = null)
        {
            return RunInternalAsync(async () =>
            {
                return DeserializeJson(await PostToStringAsync(url, data, headers).ConfigureAwait(false), type);
            });
        }

        /// <summary>
        /// Posts the message as Json and gets the responce as T if successful else returns null
        /// </summary>
        /// <typeparam name="T">The type to convert to</typeparam>
        /// <param name="url">The url to POST</param>
        /// <returns></returns>
        public Task<T> PostToJsonAsync<T>(string url, object data, IEnumerable<KeyValuePair<string, IEnumerable<string>>> headers = null)
        {
            return RunInternalAsync(async () =>
            {
                return DeserializeJson<T>(await PostToStringAsync(url, data, headers).ConfigureAwait(false));
            });
        }

        /// <summary>
        /// Posts the message as Json and gets the responce as JObject if successful else returns null
        /// </summary>
        /// <param name="url">The url to POST</param>
        /// <returns></returns>
        public Task<JObject> PostToJObjectAsync(string url, object data, IEnumerable<KeyValuePair<string, IEnumerable<string>>> headers = null)
        {
            return RunInternalAsync(async () =>
            {
                return DeserializeJObject(await PostToStringAsync(url, data, headers).ConfigureAwait(false));
            });
        }

        #endregion Post

        #region Put

        /// <summary>
        /// Posts any HttpContent to the specified url
        /// </summary>
        /// <param name="url">The url to POST</param>
        /// <param name="data">The data to POST</param>
        /// <returns></returns>
        public Task<HttpResponseMessage> PutContentAsync(string url, HttpContent data, IEnumerable<KeyValuePair<string, IEnumerable<string>>> headers = null)
        {
            return RunInternalAsync(() =>
            {
                return SendRequestOrFailAsync(HttpMethod.Put, url, data, headers);
            });
        }

        /// <summary>
        /// Puts the message as Json and gets the responce if successful else returns null
        /// </summary>
        /// <param name="url">The url to PUT</param>
        /// <returns></returns>
        public Task<HttpResponseMessage> PutToRawAsync(string url, object data, IEnumerable<KeyValuePair<string, IEnumerable<string>>> headers = null)
        {
            return RunInternalAsync(() =>
            {
                string json = SerializeJson(data);
                var content = new StringContent(json);
                content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                return PutContentAsync(url, content, headers);
            });
        }

        /// <summary>
        /// Puts the message as Json and gets the responce as string if successful else returns null
        /// </summary>
        /// <param name="url">The url to PUT</param>
        /// <returns></returns>
        public Task<string> PutToStringAsync(string url, object data, IEnumerable<KeyValuePair<string, IEnumerable<string>>> headers = null)
        {
            return RunInternalAsync(async () =>
            {
                return await ReadContentAsString(await PutToRawAsync(url, data, headers).ConfigureAwait(false)).ConfigureAwait(false);
            });
        }

        /// <summary>
        /// Puts the message as Json
        /// </summary>
        /// <param name="url">The url to PUT</param>
        /// <returns></returns>
        public Task PutAsync(string url, object data, IEnumerable<KeyValuePair<string, IEnumerable<string>>> headers = null)
        {
            return PutToRawAsync(url, data, headers);
        }

        /// <summary>
        /// Puts the message as Json and gets the responce as object if successful else returns null
        /// </summary>
        /// <param name="url">The url to PUT</param>
        /// <returns></returns>
        public Task<object> PutToJsonAsync(string url, object data, Type type = null, IEnumerable<KeyValuePair<string, IEnumerable<string>>> headers = null)
        {
            return RunInternalAsync(async () =>
            {
                return DeserializeJson(await PutToStringAsync(url, data, headers).ConfigureAwait(false), type);
            });
        }

        /// <summary>
        /// Puts the message as Json and gets the responce as T if successful else returns null
        /// </summary>
        /// <typeparam name="T">The type to convert to</typeparam>
        /// <param name="url">The url to PUT</param>
        /// <returns></returns>
        public Task<T> PutToJsonAsync<T>(string url, object data, IEnumerable<KeyValuePair<string, IEnumerable<string>>> headers = null)
        {
            return RunInternalAsync(async () =>
            {
                return DeserializeJson<T>(await PutToStringAsync(url, data, headers).ConfigureAwait(false));
            });
        }

        /// <summary>
        /// Puts the message as Json and gets the responce as JObject if successful else returns null
        /// </summary>
        /// <param name="url">The url to PUT</param>
        /// <returns></returns>
        public Task<JObject> PutToJObjectAsync(string url, object data, IEnumerable<KeyValuePair<string, IEnumerable<string>>> headers = null)
        {
            return RunInternalAsync(async () =>
            {
                return DeserializeJObject(await PutToStringAsync(url, data, headers).ConfigureAwait(false));
            });
        }

        #endregion Put

        #region Delete

        /// <summary>
        /// Sends Delete request to the url and returns the responce if successful else returns null
        /// </summary>
        /// <param name="url">The url to DELETE</param>
        /// <returns></returns>
        public Task<HttpResponseMessage> DeleteToRawAsync(string url, IEnumerable<KeyValuePair<string, IEnumerable<string>>> headers = null)
        {
            return RunInternalAsync(() =>
            {
                return SendRequestOrFailAsync(HttpMethod.Delete, url, null, headers);
            });
        }

        /// <summary>
        /// Sends Delete request to the url and returns the responce as string if successful else returns null
        /// </summary>
        /// <param name="url">The url to DELETE</param>
        /// <returns></returns>
        public Task<string> DeleteToStringAsync(string url, IEnumerable<KeyValuePair<string, IEnumerable<string>>> headers = null)
        {
            return RunInternalAsync(async () =>
            {
                return await ReadContentAsString(await DeleteToRawAsync(url, headers).ConfigureAwait(false)).ConfigureAwait(false);
            });
        }

        /// <summary>
        /// Sends Delete request to the url
        /// </summary>
        /// <param name="url">The url to DELETE</param>
        /// <returns></returns>
        public Task DeleteAsync(string url, IEnumerable<KeyValuePair<string, IEnumerable<string>>> headers = null)
        {
            return DeleteToRawAsync(url, headers);
        }

        /// <summary>
        /// Sends Delete request to the url and returns the responce as object if successful else returns null
        /// </summary>
        /// <param name="url">The url to DELETE</param>
        /// <returns></returns>
        public Task<object> DeleteToJsonAsync(string url, Type type = null, IEnumerable<KeyValuePair<string, IEnumerable<string>>> headers = null)
        {
            return RunInternalAsync(async () =>
            {
                return DeserializeJson(await DeleteToStringAsync(url, headers).ConfigureAwait(false), type);
            });
        }

        /// <summary>
        /// Sends Delete request to the url and returns the responce as T if successful else returns null
        /// </summary>
        /// <typeparam name="T">The type to convert to</typeparam>
        /// <param name="url">The url to DELETE</param>
        /// <returns></returns>
        public Task<T> DeleteToJsonAsync<T>(string url, IEnumerable<KeyValuePair<string, IEnumerable<string>>> headers = null)
        {
            return RunInternalAsync(async () =>
            {
                return DeserializeJson<T>(await DeleteToStringAsync(url, headers).ConfigureAwait(false));
            });
        }

        /// <summary>
        /// Sends Delete request to the url and returns the responce as JObject if successful else returns null
        /// </summary>
        /// <param name="url">The url to DELETE</param>
        /// <returns></returns>
        public Task<JObject> DeleteToJObjectAsync(string url, IEnumerable<KeyValuePair<string, IEnumerable<string>>> headers = null)
        {
            return RunInternalAsync(async () =>
            {
                return DeserializeJObject(await DeleteToStringAsync(url, headers).ConfigureAwait(false));
            });
        }

        #endregion Delete

        #region File

        /// <summary>
        /// Gets a raw stream from the url (non buffered)
        /// </summary>
        /// <param name="url">The url to GET</param>
        /// <returns></returns>
        public Task<Stream> GetStreamAsync(string url)
        {
            return RunInternalAsync(async () =>
            {
                var response = await SendRequestOrFailAsync(HttpMethod.Get, url, null, null, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);
                if (response?.Content == null) { return null; }
                return await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
            });
        }

        /// <summary>
        /// Gets and reads bytes from the url
        /// </summary>
        /// <param name="url">The url to GET</param>
        /// <returns></returns>
        public Task<byte[]> GetBytesAsync(string url)
        {
            return RunInternalAsync(async () =>
            {
                using (var response = await GetRawAsync(url).ConfigureAwait(false))
                {
                    if (response?.Content == null) { return null; }
                    return await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
                }
            });
        }

        /// <summary>
        /// Gets a data stream from the url (buffered in a MemoryStream)
        /// </summary>
        /// <param name="url">The url to GET</param>
        /// <returns></returns>
        public Task<MemoryStream> GetMemoryStreamAsync(string url)
        {
            return RunInternalAsync(async () =>
            {
                using (var result = await GetStreamAsync(url).ConfigureAwait(false))
                {
                    if (result == null) { return null; }
                    var s = new MemoryStream();
                    await result.CopyToAsync(s).ConfigureAwait(false);
                    s.Seek(0, SeekOrigin.Begin);
                    return s;
                }
            });
        }

        /// <summary>
        /// Uploads a Stream to the url
        /// </summary>
        /// <param name="url">The url to POST</param>
        /// <param name="uploadStream">The Stream to POST</param>
        /// <returns></returns>
        public Task UploadStreamAsync(string url, Stream uploadStream)
        {
            return RunInternalAsync(async () =>
            {
                await PostContentAsync(url, new StreamContent(uploadStream)).ConfigureAwait(false);
            });
        }

        #endregion File

        #region Helpers

        public Task<HttpResponseMessage> SendRequestOrFailAsync(HttpMethod method, string url, HttpContent content = null, IEnumerable<KeyValuePair<string, IEnumerable<string>>> headers = null, HttpCompletionOption httpCompletionOption = HttpCompletionOption.ResponseContentRead)
        {
            return SendRequestOrFailAsync(MakeRequest(method, url, content, headers), httpCompletionOption);
        }

        public Task<HttpResponseMessage> SendRequestAsync(HttpMethod method, string url, HttpContent content = null, IEnumerable<KeyValuePair<string, IEnumerable<string>>> headers = null, HttpCompletionOption httpCompletionOption = HttpCompletionOption.ResponseContentRead)
        {
            return SendRequestAsync(MakeRequest(method, url, content, headers), httpCompletionOption);
        }

        public async Task<HttpResponseMessage> SendRequestAsync(HttpRequestMessage request, HttpCompletionOption httpCompletionOption = HttpCompletionOption.ResponseContentRead)
        {
            if (requestMiddlewares != null)
            {
                foreach (var mw in requestMiddlewares)
                {
                    request = await mw.HandleRequestAsync(this, request).ConfigureAwait(false);
                }
            }
            var response = request != null ? await Client.SendAsync(request, httpCompletionOption) : null;
            if (responseMiddlewares != null)
            {
                foreach (var mw in responseMiddlewares)
                {
                    response = await mw.HandleResponseAsync(this, request, response).ConfigureAwait(false);
                }
            }
            return response;
        }

        public HttpRequestMessage MakeRequest(HttpMethod method, string url, HttpContent content = null, IEnumerable<KeyValuePair<string, IEnumerable<string>>> headers = null)
        {
            var req = new HttpRequestMessage(method, url);
            if (content != null)
            {
                req.Content = content;
            }
            if (headers != null)
            {
                foreach (var header in headers)
                {
                    req.Headers.Add(header.Key, header.Value);
                }
            }
            return req;
        }

        public async Task<HttpResponseMessage> SendRequestOrFailAsync(HttpRequestMessage request, HttpCompletionOption httpCompletionOption = HttpCompletionOption.ResponseContentRead)
        {
            var response = await SendRequestAsync(request, httpCompletionOption).ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
            {
                return response;
            }
            else
            {
                if (Settings.ThrowOnFail)
                {
                    throw new HttpFailException(response);
                }
                else
                {
                    OnErrorCaught?.Invoke(this, new HttpFailException(response));
                }
            }
            return null;
        }

        private async Task<T> RunInternalAsync<T>(Func<Task<T>> torun)
        {
            if (!Settings.CatchErrors)
            {
                return await torun().ConfigureAwait(false);
            }
            else
            {
                try
                {
                    return await torun().ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    OnErrorCaught?.Invoke(this, e);
                }
            }
            return default(T);
        }

        private async Task RunInternalAsync(Func<Task> torun)
        {
            if (!Settings.CatchErrors)
            {
                await torun().ConfigureAwait(false);
            }
            else
            {
                try
                {
                    await torun().ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    OnErrorCaught?.Invoke(this, e);
                }
            }
        }

        public string SerializeJson(object data)
        {
            return JsonConvert.SerializeObject(data, Settings.JsonEncodingSettings);
        }

        public static async Task<string> ReadContentAsString(HttpResponseMessage response)
        {
            if (response == null) { return null; }
            using (var r = response)
            {
                var result = r.Content;
                if (result == null) { return null; }
                return await result.ReadAsStringAsync().ConfigureAwait(false);
            }
        }

        public object DeserializeJson(string json, Type type = null)
        {
            if (json == null) { return null; }
            return JsonConvert.DeserializeObject(json, type, Settings.JsonDecodingSettings);
        }

        public T DeserializeJson<T>(string json)
        {
            if (json == null) { return default(T); }
            return JsonConvert.DeserializeObject<T>(json, Settings.JsonDecodingSettings);
        }

        public JObject DeserializeJObject(string json)
        {
            if (json == null) { return null; }
            return JObject.Parse(json);
        }

        #endregion Helpers

        public void Dispose()
        {
            Client?.Dispose();
            Handler?.Dispose();
        }
    }
}