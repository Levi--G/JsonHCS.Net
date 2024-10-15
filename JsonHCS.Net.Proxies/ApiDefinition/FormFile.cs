using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

namespace JsonHCSNet.Proxies.ApiDefinition
{
    public class FormFile : IFormFile
    {
        public FormFile(Stream content, string contentName = null)
        {
            Content = content;
            ContentName = contentName;
        }
        public FormFile(string sourcefilename)
        {
            Content = File.OpenRead(sourcefilename);
            ContentName = Path.GetFileName(sourcefilename);
        }

        public static HttpContent CreateFormDataFromFile(string sourcefilename, string formDataName, string filename = null, MediaTypeHeaderValue contentType = null)
        {
            return CreateFormDataFromStream(File.OpenRead(sourcefilename), formDataName, filename, contentType);
        }

        public static HttpContent CreateFormDataFromStream(Stream content, string formDataName, string filename = null, MediaTypeHeaderValue contentType = null)
        {
            return CreateFormData(new StreamContent(content), formDataName, filename, contentType);
        }

        public static HttpContent CreateFormData(HttpContent content, string formDataName, string filename = null, MediaTypeHeaderValue contentType = null)
        {
            content.Headers.ContentType = content.Headers.ContentType ?? contentType ?? new MediaTypeHeaderValue("application/octet-stream");
            return new MultipartFormDataContent { { content, formDataName, filename } };
        }

        public Stream Content { get; set; }
        public string ContentName { get; set; }
    }

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter, AllowMultiple = false, Inherited = true)]
    public class MediaTypeAttribute : Attribute
    {
        public string MediaType { get; set; }

        public MediaTypeAttribute(string mediaType)
        {
            MediaType = mediaType;
        }
    }

    public interface IFormFile
    {
        Stream Content { get; set; }
        string ContentName { get; set; }
    }
}
