# JsonHCS.Net.Proxies.SignalR
A simple JsonHCS.Net.Proxies plugin with strongly typed SignalR hub support.

[![NuGet version (JsonHCS.Net.Proxies)](https://img.shields.io/nuget/v/JsonHCS.Net.Proxies.SignalR.svg)](https://www.nuget.org/packages/JsonHCS.Net.Proxies/)

This plugin is currently experimental and might contain numerous bugs!

## Support

Supported platforms: .Net Standard 2.0+

## Simple usage

- Include the plugin from NuGet

- Add the plugin to the proxy generator and construct the api

```cs
JsonHCSProxyGenerator pg = new JsonHCSProxyGenerator(null, new SignalRPlugin(), new BasicPlugin());
var api = pg.CreateClassProxy<API>("http://localhost:5000/");
```

- Include the plugin declaration in the API object with any or multiple of its usages:

```cs
    public abstract class API
    {
        //Connect with strongly typed methods and Events
        [Route("hub")]
        public abstract Task<HubConnection<Client, Events>> Connect();

        //Connect a plain HubConnection like normal SignalR
        [Route("hub")]
        public abstract Task<HubConnection> ConnectPlain();

        //Connects and call the specified method
        //implied: [HubMethod("Broadcast", SendType.Invoke)]
        [HubMethod]
        public abstract Task Broadcast(string text);
    }

    public interface Client
    {
        //Calls his method on the Hub, can be a shared interface to prevent mistakes!
        Task Broadcast(string text);
    }

    public interface Events
    {
        //Triggers when "Receive" gets called from the Hub
        event Func<string, Task> Receive;

        //System.Reactive compatible IObservable gets updated when Receive gets called
        //Only supports single argument calls for obvious reasons
        [HubMethod("Receive")]
        IObservable<string> Message { get; }
    }
```

- Usage of HubConnection<,>:

```cs
var api = pg.CreateClassProxy<API>("http://localhost:5000/");

//Connects to the hub
var hub = await api.Connect();

//Use hub.On for events/observables
hub.On.Receive += ...;
var subscription = hub.On.Message.Subscribe(s => Console.WriteLine($"{DateTime.Now.TimeOfDay}: Subscription was updated to: {s}")));

//Use hub.Send or hub.Invoke to Send/Invoke to the hub
await hub.Send.Broadcast("Test 1");
Console.WriteLine($"{DateTime.Now.TimeOfDay}: Sending Test 2");
await hub.Send.Broadcast("Test 2");
Console.WriteLine($"{DateTime.Now.TimeOfDay}: Sending Test 3");
await hub.Send.Broadcast("Test 3");
```

Note: Any connection created will get cached and reused on multiple calls.

A more complete sample can be found in the source.

## Issues

Found an issue? [Submit an issue!](https://github.com/Levi--G/JsonHCS.Net/issues)