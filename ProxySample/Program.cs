using JsonHCSNet;
using JsonHCSNet.Proxies;
using JsonHCSNet.Proxies.ApiDefinition;
using JsonHCSNet.Proxies.Plugins;
using JsonHCSNet.Proxies.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace Sample
{
    class Program
    {
        static void Main(string[] args)
        {
            Run().Wait();
            Console.WriteLine();
            Console.WriteLine("Press ENTER to end demo...");
            Console.ReadLine();
        }

        static async Task Run()
        {
            JsonHCSProxyGenerator pg = new JsonHCSProxyGenerator(null, new SignalRPlugin(), new BasicPlugin());

            //Proxy speed comparison:
            {
                Stopwatch s = new Stopwatch();
                s.Start();
                var client = new JsonHCS();
                for (int i = 0; i < 4; i++)
                {
                    var allPosts = await client.GetJsonAsync("https://jsonplaceholder.typicode.com/posts");
                    var onePost = await client.GetJsonAsync("https://jsonplaceholder.typicode.com/posts/2");
                    await client.PostAsync("https://jsonplaceholder.typicode.com/posts", "{ title: 'foo', body: 'bar', userId: 1 }");
                }
                s.Stop();
                Console.WriteLine("Direct:");
                Console.WriteLine(s.ElapsedMilliseconds);
            }
            {
                Stopwatch s = new Stopwatch();
                s.Start();
                var client = pg.CreateClassProxy<SampleController>("https://jsonplaceholder.typicode.com/");
                for (int i = 0; i < 4; i++)
                {
                    var allPosts = await client.Get();
                    var onePost = await client.GetPost(2);
                    await client.AddPost(new SampleController.Post() { title = "foo", body = "bar", userId = 1 });
                }
                s.Stop();
                Console.WriteLine("Proxy:");
                Console.WriteLine(s.ElapsedMilliseconds);
            }

            //SignalR plugin demo:
            var api = pg.CreateClassProxy<API>("http://localhost:5000/");
            var hub = await api.Connect();
            hub.On.Receive += On_Receive;
            var received = 0;
            using (hub.On.Message.Subscribe(s => { Console.WriteLine($"{DateTime.Now.TimeOfDay}: Subscription was updated to: {s}"); received++; }, () => Console.WriteLine($"{DateTime.Now.TimeOfDay}: Subscription closed!")))
            {
                Console.WriteLine($"{DateTime.Now.TimeOfDay}: Sending Test 1");
                await hub.Send.Broadcast("Test 1");
                Console.WriteLine($"{DateTime.Now.TimeOfDay}: Sending Test 2");
                await hub.Send.Broadcast("Test 2");
                Console.WriteLine($"{DateTime.Now.TimeOfDay}: Sending Test 3");
                await hub.Send.Broadcast("Test 3");
                while (received < 3)
                {
                    await Task.Delay(10);
                }
            }
        }

        private async static Task On_Receive(string arg)
        {
            Console.WriteLine($"{DateTime.Now.TimeOfDay}: Received: {arg}");
            await Task.Delay(1);
        }
    }

    [Route("posts")]
    public abstract class SampleController
    {
        //[Route("")]
        public abstract Task<Post[]> Get();

        [Route("{id}")]
        public abstract Task<Post> GetPost(int id);

        //[HttpPost]
        //[Route("")]
        public abstract Task AddPost(/*[FromBody]*/Post value);

        public class Post
        {
            public int userId { get; set; }
            public int id { get; set; }
            public string title { get; set; }
            public string body { get; set; }
        }
    }

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
}
