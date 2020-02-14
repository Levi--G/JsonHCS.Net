# JsonHCS.Net.Proxies
JsonHCS.Net.Proxies for .Net is a JsonHCS.Net based proxy generator for easy api definitions.

[![NuGet version (JsonHCS.Net.Proxies)](https://img.shields.io/nuget/v/JsonHCS.Net.Proxies.svg)](https://www.nuget.org/packages/JsonHCS.Net.Proxies/)

JsonHCS.Net.Proxies recently got some big updates and should support most basic scenario's now, feel free to suggest new features!

## Support

Supported platforms: .Net Standard 1.5+

Supported api definitions:
- Abstract class
- Class with virtual/abstract properties
- Interface

The following attributes are recognised and used for api generation:
ASP.NET attributes:
- RouteAttribute
- HttpGetAttribute
- HttpPostAttribute
- HttpPutAttribute
- HttpDeleteAttribute
- FromBodyAttribute
- FromQueryAttribute
- FromFormAttribute (TBD)
- FromHeaderAttribute
- FromRouteAttribute

JsonHCS specific:
- RawStringAttribute (Returns an unparsed string, default will try to parse the string as json)

## Simple usage

- Include the package in NuGet: https://www.nuget.org/packages/JsonHCS.Net.Proxies/

- Add the right usings

```cs
//For the proxy:
using JsonHCSNet.Proxies;

//For the attribute definitions (optional)
//recommended for clients (smaller footprint)
using JsonHCSNet.Proxies.ApiDefinition;

//Or if you want to use the AspNetCore.Mvc.Core nuget
//recommended for asp.net core servers using the library
//using Microsoft.AspNetCore.Mvc;
```

- Construct with your preferred options and use it!

```cs
var settings = new JsonHCS_Settings()
{
    CookieSupport = true,                   //To support sessions and thus cookies
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
        //[Route("")]//Optional
		//[HttpGet]//Optional, get is default
        public abstract Task<Post[]> Get();

        [Route("{id}")]
        public abstract Task<Post> GetPost(/*[FromRoute]*/int id);//Optional, FromRoute is implied when name is found in route
		
        //[HttpPost]//Optional, post is implied when using FromBody
        public abstract Task AddPost(/*[FromBody]*/Post value);//FromBody is Optional, is implied when using complex types

        public class Post
        {
            public int userId { get; set; }
            public int id { get; set; }
            public string title { get; set; }
            public string body { get; set; }
        }
    }
```

Note: the optional attributes are not needed when using JsonHCS wich allows for very easy api clients, BUT Asp.Net might still require them!

A more complete sample can be found in the source.

## Plugins

You can now support your own custom types/attributes/requests.
Simply create a class that derives from IProxyPlugin:

```cs
    /// <summary>
    /// Provides ActionResult support
    /// </summary>
    public class ActionResultPlugin : ProxyPlugin
    {
		...
        public override bool IsHandler => true;
		...
        public override Task<T> Handle<T>(PluginManager manager, JsonHCS jsonHCS, string route, List<Parameter> parameters, IInvocation invocation)
        {
            var targetType = typeof(T);
            //Get HttpResponseMessage with default implementation
            var task = manager.Handle<System.Net.Http.HttpResponseMessage>(jsonHCS, route, parameters, invocation);
            Task<T> returntask;
            //implement own usage
            if (targetType.IsConstructedGenericType)
            {
                returntask = this.GetType().GetMethod("GetActionResultT").MakeGenericMethod(targetType.GetGenericArguments().First()).Invoke(this, new object[] { task, jsonHCS }) as Task<T>;
            }
            else if (typeof(ApiDefinition.ActionResult) == targetType)
            {
                returntask = GetActionResult(task, jsonHCS) as Task<T>;
            }
            else
            {
                returntask = GetIActionResult(task, jsonHCS) as Task<T>;
            }
            return returntask;
        }
		...
    }
```

And include it in the plugin list when constructing the ProxyGenerator:

```cs
JsonHCSProxyGenerator pg = new JsonHCSProxyGenerator(null, new ActionResultPlugin(), new BasicPlugin()); //Don't forget to include the BasicPlugin if you need the default implementations
```

Plugins are loaded in order, so the first plugin in the list that is able to handle the request will handle it. It is always best to load specific plugins first to avoid priority issues.

By default the following plugins are loaded:
- ActionResultPlugin
- BasicPlugin

You can remove them by not including these when specifying your own plugins.

## Shared api definition (advanced)

With JsonHCS.Net.Proxies you can share your api definition between your server and client without any code generation!

Make your definition as an abstract class inheriting from Controller or ControllerBase
Then add these usings:

```
#if SERVER
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
#else
using JsonHCSNet.Proxies.ApiDefinition; 
#endif
```

Example csproj (adjust to your situation):
```
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>netstandard1.5</TargetFramework>
  </PropertyGroup>

  <PropertyGroup Condition="$(DefineConstants.Contains(SERVER))">
    <TargetFramework>netstandard2.0</TargetFramework>
    <AssemblyName>Lib.Server</AssemblyName>
  </PropertyGroup>

  <PropertyGroup Condition="'$(Configuration)|$(Platform)'=='Debug|AnyCPU'">
    <DefineConstants>$(DefineConstants);TRACE;DEBUG</DefineConstants>
  </PropertyGroup>

  <PropertyGroup Condition="'$(Configuration)|$(Platform)'=='Release|AnyCPU'">
    <DefineConstants>$(DefineConstants);TRACE;RELEASE</DefineConstants>
  </PropertyGroup>

  <ItemGroup Condition="$(DefineConstants.Contains(SERVER))">
    <PackageReference Include="Microsoft.AspNetCore.Mvc.Core" Version="2.1.3" />
  </ItemGroup>

  <ItemGroup Condition="!$(DefineConstants.Contains(SERVER))">
    <PackageReference Include="JsonHCS.Net.Proxies" Version="1.2.0" />
  </ItemGroup>
</Project>
```

Now you can build (or package) and reference the Library you made twice, one for the client using JsonHCS api definitions and one for the server (you need to tell Visual studio or MSbuild to define SERVER as a compilation constant). In your client you can use the abstract class to generate the proxy as described earlier, in the server you can inherit your implementation from the abstract class you made and no need to define your routes/attributes again as they are set in the abstract class.

Now when you make changes to your definition both api and client will get compile-time checks of valid api implementation/usage without any custom plugins or code generation!

## Issues

Found an issue? [Submit an issue!](https://github.com/Levi--G/JsonHCS.Net/issues)