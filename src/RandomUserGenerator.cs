using System;
using System.Net.Http;
using Newtonsoft.Json.Linq;

namespace TimHanewich.AgentFramework
{
    public class RandomUserGenerator : ExecutableFunction
    {
        public RandomUserGenerator()
        {
            Name = "generate_random_user";
            Description = "Generate random user information.";
        }

        public override async Task<string> ExecuteAsync(JObject? arguments)
        {
            HttpClient hc = new HttpClient();
            HttpResponseMessage resp = await hc.GetAsync("https://randomuser.me/api/");
            string content = await resp.Content.ReadAsStringAsync();
            if (resp.StatusCode != System.Net.HttpStatusCode.OK)
            {
                return "Response code from random user API was '" + resp.StatusCode.ToString() + "'. Content: " + content;
            }
            return content;
        }
    }
}