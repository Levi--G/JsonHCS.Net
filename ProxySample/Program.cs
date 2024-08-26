using JsonHCSNet;
using JsonHCSNet.Proxies;
using JsonHCSNet.Proxies.ApiDefinition;
using JsonHCSNet.Proxies.Plugins;
using JsonHCSNet.Proxies.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Collections.Generic;
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
            var client = new JsonHCS(new JsonHCS_Settings() { Timeout = 10000, ThrowOnFail = true, CatchErrors = false, AddJsonAcceptHeaders = true });
            JsonHCSProxyGenerator pg = new JsonHCSProxyGenerator(client, new SignalRPlugin(), new ActionResultPlugin(), new BasicPlugin());
            var proxy = pg.CreateInterfaceProxy<ValuesController>("http://localhost:5000/");
            await Task.Delay(5000);
            for (int o = 0; o < 4; o++)
            {
                //Proxy speed comparison:
                Stopwatch s = new Stopwatch();
                s.Start();
                for (int i = 0; i < 400; i++)
                {
                    var all = await client.GetJsonAsync<IEnumerable<string>>("http://localhost:5000/api/values");
                    //Console.WriteLine(all.Count());
                    var one = await client.GetJsonAsync("http://localhost:5000/api/values/2");
                    //Console.WriteLine(one);
                    await client.PostAsync("http://localhost:5000/api/values", "Value");
                    //Console.WriteLine("Done");
                }
                s.Stop();
                Console.WriteLine("Direct:");
                Console.WriteLine(s.ElapsedMilliseconds);
                s.Reset();
                s.Start();
                for (int i = 0; i < 400; i++)
                {
                    var allrequest = await proxy.Get();
                    if (allrequest.IsSuccess)
                    {
                        var all = await allrequest.GetResultAsync();
                        //Console.WriteLine(all.Count());
                    }
                    var one = await (await proxy.Get(2)).GetResultAsync();
                    //Console.WriteLine(one);
                    var post = await proxy.Post("Value");
                    //Console.WriteLine(post.IsSuccess ? "Done" : "Failed");
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

    //definition copied from SignalRHub controller
    //They are 1:1 compatible
    //[Route("api/[controller]")]
    //public abstract class ValuesController : ControllerBase
    //{
    //    // GET api/values
    //    public abstract Task<ActionResult<IEnumerable<string>>> Get();

    //    // GET api/values/5
    //    [HttpGet("{id}")]
    //    public abstract Task<ActionResult<string>> Get(int id);

    //    // POST api/values
    //    [HttpPost]
    //    public abstract Task<IActionResult> Post([FromBody] string value);

    //    // PUT api/values/5
    //    [HttpPut("{id}")]
    //    public abstract Task<IActionResult> Put(int id, [FromBody] string value);

    //    // DELETE api/values/5
    //    [HttpDelete("{id}")]
    //    public abstract Task<IActionResult> Delete(int id);
    //}
    [Route("api/[controller]")]
    public interface ValuesController
    {
        // GET api/values
        Task<ActionResult<IEnumerable<string>>> Get();

        // GET api/values/5
        [HttpGet("{id}")]
        Task<ActionResult<string>> Get(int id);

        // POST api/values
        [HttpPost]
        Task<IActionResult> Post([FromBody] string value);

        // PUT api/values/5
        [HttpPut("{id}")]
        Task<IActionResult> Put(int id, [FromBody] string value);

        // DELETE api/values/5
        [HttpDelete("{id}")]
        Task<IActionResult> Delete(int id);
    }

    //the old sample
    [Route("posts")]
    public abstract class SampleController
    {
        //[Route("")]
        public abstract Task<ActionResult<Post[]>> Get();

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
