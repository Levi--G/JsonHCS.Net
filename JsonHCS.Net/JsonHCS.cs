using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
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
        }

        #endregion Constructors

        #region Get

        /// <summary>
        /// Gets the raw HttpResponseMessage if successful else returns null
        /// </summary>
        /// <param name="url">The url to GET</param>
        /// <returns></returns>
        public async Task<HttpResponseMessage> GetRawAsync(string url)
        {
            var response = await Client.GetAsync(url);
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
                return null;
            }
        }

        /// <summary>
        /// Gets the responce as string if successful else returns null
        /// </summary>
        /// <param name="url">The url to GET</param>
        /// <returns></returns>
        public async Task<string> GetStringAsync(string url)
        {
            var result = (await GetRawAsync(url))?.Content;
            if (result == null) { return null; }
            return await result.ReadAsStringAsync();
        }

        /// <summary>
        /// Gets the responce as object if successful else returns null
        /// </summary>
        /// <param name="url">The url to GET</param>
        /// <returns></returns>
        public async Task<object> GetJsonAsync(string url)
        {
            var result = await GetStringAsync(url);
            if (result == null) { return null; }
            return JsonConvert.DeserializeObject(result);
        }

        /// <summary>
        /// Gets the responce as T if successful else returns the default value
        /// </summary>
        /// <typeparam name="T">The type to convert to</typeparam>
        /// <param name="url">The url to GET</param>
        /// <returns></returns>
        public async Task<T> GetJsonAsync<T>(string url)
        {
            var result = await GetStringAsync(url);
            if (result == null) { return default(T); }
            return JsonConvert.DeserializeObject<T>(result);
        }

        /// <summary>
        /// Gets the responce as JObject if successful else returns null
        /// </summary>
        /// <param name="url">The url to GET</param>
        /// <returns></returns>
        public async Task<JObject> GetJObjectAsync(string url)
        {
            var result = await GetStringAsync(url);
            if (result == null) { return null; }
            return JObject.Parse(result);
        }

        #endregion Get

        #region Post

        /// <summary>
        /// Posts the message as Json and gets the responce if successful else returns null
        /// </summary>
        /// <param name="url">The url to POST</param>
        /// <returns></returns>
        public async Task<HttpResponseMessage> PostToRawAsync(string url, object data)
        {
            string json = JsonConvert.SerializeObject(data);
            var content = new StringContent(json);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            var response = await Client.PostAsync(url, content);
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
                return null;
            }
        }

        /// <summary>
        /// Posts the message as Json and gets the responce as string if successful else returns null
        /// </summary>
        /// <param name="url">The url to POST</param>
        /// <returns></returns>
        public async Task<string> PostToStringAsync(string url, object data)
        {
            var result = (await PostToRawAsync(url, data))?.Content;
            if (result == null) { return null; }
            return await result.ReadAsStringAsync();
        }

        /// <summary>
        /// Posts the message as Json
        /// </summary>
        /// <param name="url">The url to POST</param>
        /// <returns></returns>
        public async Task PostAsync(string url, object data)
        {
            await PostToRawAsync(url, data);
        }

        /// <summary>
        /// Posts the message as Json and gets the responce as object if successful else returns null
        /// </summary>
        /// <param name="url">The url to POST</param>
        /// <returns></returns>
        public async Task<object> PostToJsonAsync(string url, object data)
        {
            var result = await PostToStringAsync(url, data);
            if (result == null) { return null; }
            return JsonConvert.DeserializeObject(result);
        }

        /// <summary>
        /// Posts the message as Json and gets the responce as T if successful else returns null
        /// </summary>
        /// <typeparam name="T">The type to convert to</typeparam>
        /// <param name="url">The url to POST</param>
        /// <returns></returns>
        public async Task<T> PostToJsonAsync<T>(string url, object data)
        {
            var result = await PostToStringAsync(url, data);
            if (result == null) { return default(T); }
            return JsonConvert.DeserializeObject<T>(result);
        }

        /// <summary>
        /// Posts the message as Json and gets the responce as JObject if successful else returns null
        /// </summary>
        /// <param name="url">The url to POST</param>
        /// <returns></returns>
        public async Task<JObject> PostToJObjectAsync(string url, object data)
        {
            var result = await PostToStringAsync(url, data);
            if (result == null) { return null; }
            return JObject.Parse(result);
        }

        #endregion Post

        #region Put

        /// <summary>
        /// Puts the message as Json and gets the responce if successful else returns null
        /// </summary>
        /// <param name="url">The url to PUT</param>
        /// <returns></returns>
        public async Task<HttpResponseMessage> PutToRawAsync(string url, object data)
        {
            string json = JsonConvert.SerializeObject(data);
            var content = new StringContent(json);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            var response = await Client.PutAsync(url, content);
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
                return null;
            }
        }

        /// <summary>
        /// Puts the message as Json and gets the responce as string if successful else returns null
        /// </summary>
        /// <param name="url">The url to PUT</param>
        /// <returns></returns>
        public async Task<string> PutToStringAsync(string url, object data)
        {
            var result = (await PutToRawAsync(url, data))?.Content;
            if (result == null) { return null; }
            return await result.ReadAsStringAsync();
        }

        /// <summary>
        /// Puts the message as Json
        /// </summary>
        /// <param name="url">The url to PUT</param>
        /// <returns></returns>
        public async Task PutAsync(string url, object data)
        {
            await PutToRawAsync(url, data);
        }

        /// <summary>
        /// Puts the message as Json and gets the responce as object if successful else returns null
        /// </summary>
        /// <param name="url">The url to PUT</param>
        /// <returns></returns>
        public async Task<object> PutToJsonAsync(string url, object data)
        {
            var result = await PutToStringAsync(url, data);
            if (result == null) { return null; }
            return JsonConvert.DeserializeObject(result);
        }

        /// <summary>
        /// Puts the message as Json and gets the responce as T if successful else returns null
        /// </summary>
        /// <typeparam name="T">The type to convert to</typeparam>
        /// <param name="url">The url to PUT</param>
        /// <returns></returns>
        public async Task<T> PutToJsonAsync<T>(string url, object data)
        {
            var result = await PutToStringAsync(url, data);
            if (result == null) { return default(T); }
            return JsonConvert.DeserializeObject<T>(result);
        }

        /// <summary>
        /// Puts the message as Json and gets the responce as JObject if successful else returns null
        /// </summary>
        /// <param name="url">The url to PUT</param>
        /// <returns></returns>
        public async Task<JObject> PutToJObjectAsync(string url, object data)
        {
            var result = await PutToStringAsync(url, data);
            if (result == null) { return null; }
            return JObject.Parse(result);
        }

        #endregion Put

        #region Delete

        /// <summary>
        /// Sends Delete request to the url and returns the responce if successful else returns null
        /// </summary>
        /// <param name="url">The url to DELETE</param>
        /// <returns></returns>
        public async Task<HttpResponseMessage> DeleteToRawAsync(string url)
        {
            var response = await Client.DeleteAsync(url);
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
                return null;
            }
        }

        /// <summary>
        /// Sends Delete request to the url and returns the responce as string if successful else returns null
        /// </summary>
        /// <param name="url">The url to DELETE</param>
        /// <returns></returns>
        public async Task<string> DeleteToStringAsync(string url)
        {
            var result = (await DeleteToRawAsync(url))?.Content;
            if (result == null) { return null; }
            return await result.ReadAsStringAsync();
        }

        /// <summary>
        /// Sends Delete request to the url
        /// </summary>
        /// <param name="url">The url to DELETE</param>
        /// <returns></returns>
        public async Task DeleteAsync(string url)
        {
            await DeleteToRawAsync(url);
        }

        /// <summary>
        /// Sends Delete request to the url and returns the responce as object if successful else returns null
        /// </summary>
        /// <param name="url">The url to DELETE</param>
        /// <returns></returns>
        public async Task<object> DeleteToJsonAsync(string url)
        {
            var result = await DeleteToStringAsync(url);
            if (result == null) { return null; }
            return JsonConvert.DeserializeObject(result);
        }

        /// <summary>
        /// Sends Delete request to the url and returns the responce as T if successful else returns null
        /// </summary>
        /// <typeparam name="T">The type to convert to</typeparam>
        /// <param name="url">The url to DELETE</param>
        /// <returns></returns>
        public async Task<T> DeleteToJsonAsync<T>(string url)
        {
            var result = await DeleteToStringAsync(url);
            if (result == null) { return default(T); }
            return JsonConvert.DeserializeObject<T>(result);
        }

        /// <summary>
        /// Sends Delete request to the url and returns the responce as JObject if successful else returns null
        /// </summary>
        /// <param name="url">The url to DELETE</param>
        /// <returns></returns>
        public async Task<JObject> DeleteToJObjectAsync(string url)
        {
            var result = await DeleteToStringAsync(url);
            if (result == null) { return null; }
            return JObject.Parse(result);
        }

        #endregion Delete

        public void Dispose()
        {
            Client?.Dispose();
            Handler?.Dispose();
        }
    }
}