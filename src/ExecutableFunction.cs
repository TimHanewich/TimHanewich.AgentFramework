using System;
using Newtonsoft.Json.Linq;
using TimHanewich.Foundry.OpenAI.Responses;

namespace ResponsesAgents
{
    public abstract class ExecutableFunction
    {
        public string Name {get; set;}
        public string Description {get; set;}
        public List<FunctionInputParameter> InputParameters {get; set;}

        public ExecutableFunction()
        {
            Name = string.Empty;
            Description = string.Empty;
            InputParameters = new List<FunctionInputParameter>();
        }

        public abstract Task<string> ExecuteAsync(JObject? arguments = null);
    }
}