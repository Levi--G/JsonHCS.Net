using JsonHCSNet;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace JsonHCSNet.Tests
{
    [TestClass]
    public class JsonTests
    {
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

        JsonHCS GetJsonHCS()
        {
            return new JsonHCS(new JsonHCS_Settings()
            {
                CatchErrors = false,
                ThrowOnFail = true,
                BaseAddress = "https://jsonplaceholder.typicode.com/posts/"
            });
        }

        [TestMethod]
        public void TestGet()
        {
            Assert.IsTrue(this.GetJsonHCS().GetJsonAsync<Post[]>("").Result.Length == 100);
        }

        [TestMethod]
        public void TestGetQuery()
        {
            Assert.IsTrue(this.GetJsonHCS().GetJsonAsync<Post>("2").Result.Id == 2);
        }

        [TestMethod]
        public void TestPost()
        {
            Assert.IsTrue(this.GetJsonHCS().PostToJsonAsync<Post>("", new PostBase { Title = "", Body = "", UserId = 1 }).Result.Id == 101);
        }

        [TestMethod]
        public void TestPut()
        {
            Assert.IsTrue(this.GetJsonHCS().PutToJsonAsync<Post>("2", new Post { Id=2, Body="", Title="", UserId=1 }).Result.Id == 2);
        }

        [TestMethod]
        public void TestDelete()
        {
            this.GetJsonHCS().DeleteAsync("2").Wait();
        }

        [TestMethod]
        public void TestStream()
        {
            var stream = this.GetJsonHCS().GetStreamAsync("2").Result;
            Assert.IsFalse(stream.GetType() == typeof(MemoryStream));
            using (var sr = new StreamReader(stream))
            {
                var s = sr.ReadToEnd();
                GetJsonHCS().DeserializeJson(s);
            }
        }

        [TestMethod]
        public void TestMemoryStream()
        {
            var stream = this.GetJsonHCS().GetMemoryStreamAsync("2").Result;
            Assert.IsTrue(stream.GetType() == typeof(MemoryStream));
            using (var sr = new StreamReader(stream))
            {
                var s = sr.ReadToEnd();
                GetJsonHCS().DeserializeJson(s);
            }
        }
    }
}
