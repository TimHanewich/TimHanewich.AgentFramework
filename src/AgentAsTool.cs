using System;
using Newtonsoft.Json.Linq;
using TimHanewich.Foundry.OpenAI.Responses;

namespace TimHanewich.AgentFramework
{
    public class AgentAsTool : ExecutableFunction
    {
        public Agent InnerAgent {get; set;}
        private List<string> InnerAgentResponseCollection;

        public AgentAsTool(Agent agent, string name, string description)
        {
            InnerAgent = agent;
            Name = name;
            Description = description;
            

            //Set up the input parameter - every agent-as-tool accepts a "request" string
            FunctionInputParameter request = new FunctionInputParameter("request", "The request or instruction to send to this agent.");
            InputParameters.Add(request);

            //Set up list for collecting
            InnerAgentResponseCollection = new List<string>();
        }

        public override async Task<string> ExecuteAsync(JObject? arguments = null)
        {
            string request = "";
            if (arguments != null)
            {
                JProperty? prop_request = arguments.Property("request");
                if (prop_request != null)
                {
                    request = prop_request.Value.ToString();
                }
            }

            //Go!
            InnerAgentResponseCollection.Clear();
            InnerAgent.TextResponseReceived += CollectTextResponse;
            await InnerAgent.PromptAsync(request);

            //Concatenate the list
            string ToReturn = "";
            foreach (string item in InnerAgentResponseCollection)
            {
                ToReturn = ToReturn + item + "\n\n";
            }
            InnerAgentResponseCollection.Clear();
            return ToReturn.Trim();
        }

        private void CollectTextResponse(string txt)
        {
            InnerAgentResponseCollection.Add(txt);
        }
    }
}
