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

            //messages
            a.Messages.Add(new Message(Role.system, "You are a helpful assistant."));
            a.Messages.Add(new Message(Role.user, "What is the temperature outside in Sarasota, Florida?"));

            //tools
            Tool tool = new Tool("check_temperature", "Check the temperature for a given location.");
            tool.Parameters.Add(new ToolInputParameter("location", "The name of the location you want to check the temperature of, i.e. 'Seattle, WA' or 'Atlanta, GA'."));
            a.Tools.Add(tool);

            //Add tool call message
            Message tcm = new Message();
            tcm.Role = Role.assistant;
            ToolCall tc = new ToolCall();
            tc.ToolName = "check_temperature";
            tc.Arguments = new JObject();
            tc.Arguments.Add("location", "Sarasota, Florida");
            tc.ID = "call_KxLw58XlPkmk31fumktijVqr";
            tcm.ToolCalls = new ToolCall[]{tc};
            a.Messages.Add(tcm);

            //Add tool call response
            Message tcr = new Message();
            tcr.ToolCallID = "call_KxLw58XlPkmk31fumktijVqr";
            tcr.Role = Role.tool;
            tcr.Content = "Temperature in Sarasota, Florida: 72.5 degrees F";
            a.Messages.Add(tcr);

            Message resp = await a.PromptAsync();

            Console.WriteLine(JsonConvert.SerializeObject(resp));


        }
    }
}