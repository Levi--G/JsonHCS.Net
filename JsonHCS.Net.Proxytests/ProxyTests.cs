using JsonHCSNet;
using JsonHCSNet.Proxies;
using JsonHCSNet.Proxies.ApiDefinition;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
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

            [Route("{id}")]
            public abstract Post GetSync(int id);

            [Route("{id}")]
            public abstract IActionResult GetIAction(int id);

            [Route("{id}")]
            public abstract ActionResult<Post> GetAction(int id);

            public abstract Task<Post[]> GetPosts([FromQuery] int userId);

            [HttpPost]
            public abstract Task<Post> AddPost([FromBody]PostBase value);

            [HttpPut("{id}")]
            public abstract Task<Post> Put([FromBody]Post value, int id);

            [HttpDelete("{id}")]
            public abstract Task Delete(int id);

            //same as previous tests but without implied attributes
            public abstract Task<Post[]> GetAllMin();
            public abstract Task<Post[]> GetPostsMin(int userId);
            public abstract Task<Post> AddPostMin(PostBase value);

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

        [TestMethod]
        public void TestSynchronous()
        {
            Assert.AreEqual(GetTestAPI().GetSync(1).Id, 1);
        }

        [TestMethod]
        public void TestIActionResult()
        {
            var result = GetTestAPI().GetIAction(2);
            Assert.IsTrue(result.IsSuccess);
            Assert.AreEqual(result.GetJsonAsync<API.Post>().Result.Id, 2);
        }

        [TestMethod]
        public void TestActionResult()
        {
            var result = GetTestAPI().GetAction(3);
            Assert.IsTrue(result.IsSuccess);
            Assert.AreEqual(result.GetResultAsync().Result.Id, 3);
        }

        [TestMethod]
        public void TestImpliedAttributes()
        {
            Assert.IsTrue(GetTestAPI().GetAllMin().Result.Length == 100);
            Assert.IsTrue(this.GetTestAPI().GetPostsMin(2).Result[0].UserId == 2);
            Assert.IsTrue(GetTestAPI().AddPostMin(new API.PostBase() { Body = "", Title = "Title", UserId = 1 }).Result.Id == 101);
        }
    }
}
