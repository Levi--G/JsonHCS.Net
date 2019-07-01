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
}
