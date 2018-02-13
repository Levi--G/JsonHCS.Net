using System;
using System.Threading.Tasks;
using JsonHCSNet;

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
            var settings = new JsonHCS_Settings()
            {
                CookieSupport = true,                   //I want to support sessions and thus cookies
                AddDefaultAcceptHeaders = true,         //Adds default acceptance headers for json types
                UserAgent = "MyAwesomeSampleAgent"      //Because why not, this is usually ignored anyways
            };
            using (JsonHCS client = new JsonHCS(settings))
            {
                //Use it
                const string Url = "https://www.w3schools.com/jquery/demo_ajax_json.js";    //change to actual url

                Console.WriteLine("Get json string:");
                var json = await client.GetStringAsync(Url);
                Console.WriteLine(json);
                Console.WriteLine("Type: " + json.GetType().FullName);
                Console.WriteLine();

                Console.WriteLine("Get object:");
                var obj = await client.GetJsonAsync(Url);
                Console.WriteLine(obj);
                Console.WriteLine("Type: " + obj.GetType().FullName);
                Console.WriteLine();

                Console.WriteLine("Get dynamic.firstName:");
                dynamic objFN = await client.GetJsonAsync(Url);
                Console.WriteLine(objFN.firstName);
                Console.WriteLine("Type: " + objFN.firstName.GetType().FullName);
                Console.WriteLine();

                Console.WriteLine("Get<ExpectedResponce> ToString:");
                var ERobj = await client.GetJsonAsync<ExpectedResponce>(Url);
                Console.WriteLine(ERobj);
                Console.WriteLine("Type: " + ERobj.GetType().FullName);
                Console.WriteLine();

                Console.WriteLine("Get JObject[\"firstname\"]:");
                var JObj = await client.GetJObjectAsync(Url);
                Console.WriteLine(JObj["firstName"]);
                Console.WriteLine("Type: " + JObj["firstName"].GetType().FullName);
                Console.WriteLine();

            }
        }

        public class ExpectedResponce
        {
            public string firstName { get; set; }
            public string lastName { get; set; }
            public string age { get; set; }

            public override string ToString()
            {
                return firstName + " " + lastName + ": " + age;
            }
        }
    }
}
