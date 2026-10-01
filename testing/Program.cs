using System;
using TimHanewich.AgentFramework;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TimHanewich.Foundry;

namespace AgentFrameworkTesting
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            await TestAsync();
        }

        public static async Task TestAsync()
        {
            FoundryResource fr = new FoundryResource("https://____.services.ai.azure.com");
            fr.ApiKey = "";

            Agent MyAgent = new Agent("You are smart.");
            MyAgent.FoundryResource = fr;
            MyAgent.Model = "gpt-5.4-mini";
            MyAgent.WebSearchInvoked += WebSearch;
            MyAgent.WebSearchPageOpened += PageOpened;
            MyAgent.WebSearchEnabled = true;
            MyAgent.TextResponseReceived += TextResponseReceived;

            Console.WriteLine("Prompting...");
            await MyAgent.PromptAsync("Tell me about Tim Hanewich's Scout project. Read one of his articles and summarize it for me.");            
        }

        public static void FunctionInvoked(ExecutableFunction ef, JObject arguments)
        {
            Console.Write(ef.Name + " invoked: " + arguments.ToString(Newtonsoft.Json.Formatting.None) + " ");
        }

        public static void FunctionReceived(ExecutableFunction ef, JObject arguments)
        {
            Console.WriteLine("Complete: " + ef.Name);
        }
        
        public static void InfReq()
        {
            Console.WriteLine("InfReq!");
        }

        public static void InfRec(int i, int o)
        {
            Console.WriteLine("InfRec: " + i.ToString() + ", " + o.ToString());
        }

        public static void WebSearch(string query)
        {
            Console.WriteLine("Search: \"" + query + "\"");
        }

        public static void PageOpened()
        {
            Console.WriteLine("Page opened.");
        }


        public static void TextResponseReceived(string txt)
        {
            Console.WriteLine("Text Response From Model: <" + txt + ">");
        }
    }
}