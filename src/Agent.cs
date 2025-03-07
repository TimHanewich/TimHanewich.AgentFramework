using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http;
using Newtonsoft.Json.Linq;
using System.Text;
using System.Text.Encodings;
using Newtonsoft.Json;
using System.Text.Json.Serialization;

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
        public async Task<Message> PromptAsync(int past_messages_included = 10, bool json_mode = false)
        {
            //Validate that there is a model
            if (Model == null)
            {
                throw new Exception("Unable to prompt any model as you did not provide a model connection of any kind to this agent!");
            }

            //Validate that there is at least one message
            if (Messages.Count == 0)
            {
                throw new Exception("Unable to prompt model: You must have add at least one message before prompting the model!");
            }

            //Validate that they want at least one past message
            if (past_messages_included <= 0)
            {
                throw new Exception("You specified '" + past_messages_included.ToString() + "' past messaged to be included in the prompt to the model. This must be greater than 0.");
            }

            //Collect the correct number of messages
            List<Message> MessagesToPrompt = new List<Message>();
            for (int i = Messages.Count - 1; i >= 0; i--)
            {
                if (MessagesToPrompt.Count < past_messages_included)
                {
                    MessagesToPrompt.Add(Messages[i]); //Add
                }
            }
            MessagesToPrompt.Reverse(); //Flip the order (since we just added them in the opposite order)

            //Find system message... ensure at least that is included
            Message? SystemMessage = null;
            foreach (Message msg in Messages)
            {
                if (SystemMessage == null)
                {
                    if (msg.Role == Role.system)
                    {
                        SystemMessage = msg;
                    }
                }
            }   
            
            //If there indeed was a system message, replace the first message with that system message
            // (it may already be in there as the first item, but just replacing it anyway is fine)
            if (SystemMessage != null)
            {
                MessagesToPrompt[0] = SystemMessage;
            }

            //Invoke the inference via whatever model they provided!
            InferenceResponse ir = await Model.InvokeInferenceAsync(MessagesToPrompt.ToArray(), Tools.ToArray(), json_mode);

            //Increment token consumptions
            _CumulativePromptTokens = _CumulativePromptTokens + ir.PromptTokensConsumed;
            _CumulativeCompletionTokens = CumulativeCompletionTokens + ir.CompletionTokensConsumed;

            //return the message
            return ir.Message;
        }

        
    }
}