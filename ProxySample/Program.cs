using System;
using System.Diagnostics;
using System.Threading.Tasks;
using JsonHCSNet;
using JsonHCSNet.Proxies;
using Microsoft.AspNetCore.Mvc;

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
            JsonHCSProxyGenerator pg = new JsonHCSProxyGenerator();
            {
                Stopwatch s = new Stopwatch();
                var client = new JsonHCS();
                s.Start();
                for (int i = 0; i < 20; i++)
                {
                    var o = client.PostToRawAsync("https://jsonplaceholder.typicode.com/posts", "{ title: 'foo', body: 'bar', userId: 1 }").Result;
                    var result = await client.GetJsonAsync("https://jsonplaceholder.typicode.com/posts");
                    var result2 = await client.GetJsonAsync("https://jsonplaceholder.typicode.com/posts/2");
                }
                s.Stop();
                Console.WriteLine("Direct:");
                Console.WriteLine(s.ElapsedMilliseconds);
            }
            {
                Stopwatch s = new Stopwatch();
                var client = pg.CreateClassProxy<SampleController>("https://jsonplaceholder.typicode.com/");
                s.Start();
                for (int i = 0; i < 20; i++)
                {
                    var o = client.Post("{ title: 'foo', body: 'bar', userId: 1 }");
                    var result = await client.Get();
                    var result2 = await client.GetPost(2);
                }
                s.Stop();
                Console.WriteLine("Proxy:");
                Console.WriteLine(s.ElapsedMilliseconds);
            }
        }
    }

    [Route("posts")]
    public abstract class SampleController
    {
        [Route("")]
        public abstract Task<object> Get();

        [Route("{id}")]
        public abstract Task<object> GetPost(int id);

        [HttpPost]
        [Route("")]
        public abstract object Post([FromBody]string value);
    }
}
