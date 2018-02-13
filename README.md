# JsonHCS.Net
JsonHCS (Json Http Client Simplified) for .Net is a HTTP Client wrapper with Json support.
JSON parsing is done by the [Newtonsoft.Json](https://www.nuget.org/packages/Newtonsoft.Json/11.0.1-beta3) library.

<a href="https://www.nuget.org/packages/JsonHCS.Net/">
  <img src="https://img.shields.io/nuget/v/JsonHCS.Net.svg" alt="Nuget version">
</a>

## Support

Supported platforms: .Net Standard 1.1+

Supported http requests:
- GET
- POST
- PUT
- DELETE

## Usage

- Include the package in NuGet: https://www.nuget.org/packages/JsonHCS.Net/

- Add the right usings

```cs
using JsonHCSNet;
```

- Construct with your preferred options

```cs
var settings = new JsonHCS_Settings()
            {
                CookieSupport = true,                   //I want to support sessions and thus cookies
                AddDefaultAcceptHeaders = true,         //Adds default acceptance headers for json types
                UserAgent = "MyAwesomeSampleAgent"      //Because why not, this is usually ignored anyways
            };
using (JsonHCS client = new JsonHCS(settings))
{
    //use client
}
```

- Use it!

```cs
Console.WriteLine("Get<ExpectedResponce> ToString:");
var ERobj = await client.GetJsonAsync<ExpectedResponce>(Url); //Gets json from url and parses as the ExpectedResponce class or returns null if responce not successful
Console.WriteLine(ERobj);
```

A more complete sample can be found in the source.

## Issues

[Submit an issue](https://github.com/Levi--G/JsonHCS.Net/issues)