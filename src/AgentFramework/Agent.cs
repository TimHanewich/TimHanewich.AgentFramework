using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http;
using Newtonsoft.Json.Linq;
using System.Text;
using System.Text.Encodings;

namespace TimHanewich.AgentFramework
{
    public class Agent
    {
        public List<Message> Messages {get; set;}
        public List<Tool> Tools {get; set;}
        public IModelConnection? Model {get; set;}

        //Private trackers
        private int _CumulativePromptTokens;
        public int CumulativePromptTokens
        {
            get
            {
                return _CumulativePromptTokens;
            }
        }
        private int _CumulativeCompletionTokens;
        public int CumulativeCompletionTokens
        {
            get
            {
                return _CumulativeCompletionTokens;
            }
        }

        public Agent()
        {
            Messages = new List<Message>();
            Tools = new List<Tool>();
        }

        //Ask model to generate the next message, given the current context of messages
        public async Task<Message> PromptAsync()
        {
            if (Model == null)
            {
                throw new Exception("Unable to prompt any model as you did not provide a model connection of any kind to this agent!");
            }

            //Invoke the inference via whatever model they provided!
            InferenceResponse ir = await Model.InvokeInferenceAsync(Messages.ToArray(), Tools.ToArray());

            //Increment token consumptions
            _CumulativePromptTokens = _CumulativePromptTokens + ir.PromptTokensConsumed;
            _CumulativeCompletionTokens = CumulativeCompletionTokens + ir.CompletionTokensConsumed;

            //return the message
            return ir.Message;
        }

        
    }
}