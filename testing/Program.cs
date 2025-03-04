using System;
using TimHanewich.AgentFramework;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AgentFrameworkTesting
{
    public class Program
    {
        public static void Main(string[] args)
        {
            TimHanewich.AgentFramework.OllamaModel om = new TimHanewich.AgentFramework.OllamaModel();
            om.ModelIdentifier = "qwen2.5:7b";

            Agent a = new Agent();
            a.Model = om;
            a.Messages.Add(new Message(Role.system, "You are a helpful assistant."));


            a.Tools.Add(new Tool("check_temperature", "Check the current temperature."));



            a.Messages.Add(new Message(Role.user, "What is the temperature outside?"));
            Console.WriteLine("Calling...");
            Message m = a.PromptAsync().Result;

            Console.WriteLine(JsonConvert.SerializeObject(m, Formatting.Indented));
        }
    }
}