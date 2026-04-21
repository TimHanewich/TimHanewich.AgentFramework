using System;
using Newtonsoft.Json.Linq;
using TimHanewich.Foundry.OpenAI.Responses;

namespace ResponsesAgents
{
    public class AgentAsTool : ExecutableFunction
    {
        public Agent InnerAgent {get; set;}

        public AgentAsTool(Agent agent, string name, string description)
        {
            InnerAgent = agent;
            Name = name;
            Description = description;

            //Set up the input parameter - every agent-as-tool accepts a "request" string
            FunctionInputParameter request = new FunctionInputParameter("request", "The request or instruction to send to this agent.");
            InputParameters.Add(request);
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
            return await InnerAgent.PromptAsync(request);
        }
    }
}
