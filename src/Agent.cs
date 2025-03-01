using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http;
using Newtonsoft.Json.Linq;

namespace AgentFramework
{
    public class Agent
    {
        public List<Message> Messages {get; set;}
        public List<Tool> Tools {get; set;}
        public AzureOpenAICredentials Credentials {get; set;}

        public Agent()
        {
            Messages = new List<Message>();
            Tools = new List<Tool>();
            Credentials = new AzureOpenAICredentials();
        }

        //Ask model to generate the next message, given the current context of messages
        public async Task PromptAsync()
        {
            
        }
    }
}