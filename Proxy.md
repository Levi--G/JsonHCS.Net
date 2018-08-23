# JsonHCS.Net.Proxies
JsonHCS.Net.Proxies for .Net is a JsonHCS.Net based proxy generator for easy api definitions.

[![NuGet version (JsonHCS.Net.Proxies)](https://img.shields.io/nuget/v/JsonHCS.Net.Proxies.svg)](https://www.nuget.org/packages/JsonHCS.Net.Proxies/)

Please note: this is more a fun project and proof of concept than an actual library to be used in production so do keep this in mind (it might not support many features). However feel free to submit any ideas/issues!

## Support

Supported platforms: .Net Standard 1.5+

Supported api definitions:
- Abstract class
- Class with virtual/abstract properties
- Interface

The following attributes are recognised and used for api generation:
- RouteAttribute
- FromBodyAttribute
- HttpPostAttribute
- HttpGetAttribute
- HttpPutAttribute
- HttpDeleteAttribute

## Usage

- Include the package in NuGet: https://www.nuget.org/packages/JsonHCS.Net.Proxies/

- Add the right usings

```cs
using JsonHCSNet.Proxies;
```

- Construct with your preferred options and use it!

```cs
var settings = new JsonHCS_Settings()
{
    CookieSupport = true,                   //I want to support sessions and thus cookies
    AddDefaultAcceptHeaders = true,         //Adds default acceptance headers for json types
    UserAgent = "MyAwesomeSampleAgent"      //Because why not, this is usually ignored anyways
};
JsonHCSProxyGenerator pg = new JsonHCSProxyGenerator(settings);
var client = pg.CreateClassProxy<SampleController>("https://jsonplaceholder.typicode.com/");

//use by just calling the api definition, the library will take care of any conversions/requests
var allPosts = await client.Get();
var onePost = await client.GetPost(2);
await client.AddPost((new SampleController.Post() { title = "foo", body = "bar", userId = 1 });


//API definition
    [Route("posts")]
    public abstract class SampleController
    {
        [Route("")]
        public abstract Task<Post[]> Get();

        [Route("{id}")]
        public abstract Task<Post> GetPost(int id);

        [HttpPost]
        [Route("")]
        public abstract Task AddPost([FromBody]Post value);

        public class Post
        {
            public int userId { get; set; }
            public int id { get; set; }
            public string title { get; set; }
            public string body { get; set; }
        }
    }
```

A more complete sample can be found in the source.

## Issues

[Submit an issue](https://github.com/Levi--G/JsonHCS.Net/issues)