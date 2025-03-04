using System;

namespace TimHanewich.AgentFramework
{
    //A collection of data that comes back from an API call to an LLM service
    public class InferenceResponse
    {
        public Message Message {get; set;}
        public int PromptTokensConsumed {get; set;}
        public int CompletionTokensConsumed {get; set;}

        public InferenceResponse()
        {
            Message = new Message();
        }
    }
}