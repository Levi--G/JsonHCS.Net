using JsonHCSNet;
using JsonHCSNet.Proxies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace JsonHCSNet.Proxytests
{
    [TestClass]
    public class ProxyTests
    {
        [Route("posts")]
        public abstract class API
        {
            [Route("")]
            public abstract Task<Post[]> GetAll();

            [Route("{id}")]
            public abstract Task<Post> Get(int id);

            public abstract Task<Post[]> GetPosts([FromQuery] int userId);

            [HttpPost]
            public abstract Task<Post> AddPost([FromBody]PostBase value);

            [HttpPut("{id}")]
            public abstract Task<Post> Put([FromBody]Post value, int id);

            [HttpDelete("{id}")]
            public abstract Task Delete(int id);

            public class PostBase
            {
                public string Title { get; set; }
                public string Body { get; set; }
                public int UserId { get; set; }
            }

            public class Post : PostBase
            {
                public int Id { get; set; }
            }
        }

        JsonHCS GetJsonHCS()
        {
            return new JsonHCS(new JsonHCS_Settings()
            {
                ThrowOnFail = true
            });
        }

        API GetTestAPI()
        {
            JsonHCSProxyGenerator gen = new JsonHCSProxyGenerator(GetJsonHCS());
            return gen.CreateClassProxy<API>("https://jsonplaceholder.typicode.com/");
        }

        [TestMethod]
        public void TestGet()
        {
            Assert.IsTrue(GetTestAPI().GetAll().Result.Length == 100);
        }

        [TestMethod]
        public void TestGetQuery()
        {
            Assert.IsTrue(GetTestAPI().Get(1).Result.Id == 1);
        }

        [TestMethod]
        public void TestGetQueryAttribute()
        {
            Assert.IsTrue(this.GetTestAPI().GetPosts(2).Result[0].UserId == 2);
        }

        [TestMethod]
        public void TestPostAttribute()
        {
            Assert.IsTrue(GetTestAPI().AddPost(new API.PostBase() { Body = "", Title = "Title", UserId = 1 }).Result.Id == 101);
        }

        [TestMethod]
        public void TestPutAttribute()
        {
            Assert.IsTrue(GetTestAPI().Put(new API.Post() { Id = 1, Body = "", Title = "Title", UserId = 1 }, 1).Result.Id == 1);
        }

        [TestMethod]
        public void TestDeleteAttribute()
        {
            GetTestAPI().Delete(1).Wait();
        }
    }
}
