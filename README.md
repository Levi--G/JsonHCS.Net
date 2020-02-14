# JsonHCS.Net
JsonHCS (Json Http Client Simplified) for .Net is a HTTP Client wrapper with Json support.
JSON parsing is done by the [Newtonsoft.Json](https://www.nuget.org/packages/Newtonsoft.Json/11.0.1-beta3) library.

For more info about Json.Net.Proxies visit [this](https://github.com/Levi--G/JsonHCS.Net/blob/master/Proxy.md) page.

[![NuGet version (JsonHCS.Net)](https://img.shields.io/nuget/v/JsonHCS.Net.svg)](https://www.nuget.org/packages/JsonHCS.Net/)
[![NuGet version (JsonHCS.Net.Proxies)](https://img.shields.io/nuget/v/JsonHCS.Net.Proxies.svg)](https://www.nuget.org/packages/JsonHCS.Net.Proxies/)

## Support

Supported platforms: .Net Standard 1.1+

Supported requests:
- GET
- POST
- PUT
- DELETE
- File Download + Upload

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
//Use POCO's for data:
Console.WriteLine("Get<ExpectedResponce> ToString:");
ExpectedResponce obj = await client.GetJsonAsync<ExpectedResponce>(Url); //Gets json from url and parses as the ExpectedResponce class
Console.WriteLine(obj);

///Use dynamic
dynamic obj = await client.GetJsonAsync(Url);
Console.WriteLine(obj.firstName);

//Use JObject
var JObj = await client.GetJObjectAsync(Url);
Console.WriteLine(JObj["firstName"]);
```

A more complete sample can be found in the source.

## Issues

[Submit an issue](https://github.com/Levi--G/JsonHCS.Net/issues)