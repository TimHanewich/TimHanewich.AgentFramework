using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AgentFramework
{
    public class Program
    {
        public static void Main(string[] args)
        {
            TestAsync().Wait();
        }

        public static async Task TestAsync()
        {
            Agent a = new Agent();
            a.Credentials = JsonConvert.DeserializeObject<AzureOpenAICredentials>(System.IO.File.ReadAllText(@"C:\Users\timh\Downloads\AgentFramework\credentials.json"));

            //Add system message
            a.Messages.Add(new Message(Role.system, "You are a helpful assistant."));

            

            //Add tool: check weather
            Tool tool = new Tool("check_temperature", "Check the temperature for a given location.");
            tool.Parameters.Add(new ToolInputParameter("location", "The name of the location you want to check the temperature of, i.e. 'Seattle, WA' or 'Atlanta, GA'."));
            a.Tools.Add(tool);


            //Add welcoming message
            string opening_msg = "Hello! I'm here to help. What can I do for you?";
            a.Messages.Add(new Message(Role.assistant, opening_msg));
            Console.WriteLine(opening_msg);

            while (true)
            {
                //Collect input
                Console.WriteLine();
                string? input = null;
                while (input == null)
                {
                    Console.Write("> ");
                    input = Console.ReadLine();
                }

                //Append message
                a.Messages.Add(new Message(Role.user, input));

                //Prompt
                Console.WriteLine();
                Console.Write("Thinking...");
                Message response = await a.PromptAsync();

                //Write
                Console.WriteLine();
                Console.WriteLine();
                Console.WriteLine(response.Content);

                //Add to message array
                a.Messages.Add(response);
            }
            


        }
    }
}