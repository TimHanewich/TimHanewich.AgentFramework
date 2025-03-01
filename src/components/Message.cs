using System;

namespace AgentFramework
{
    public class Message
    {
        public Role Role {get; set;}
        public string? Content {get; set;} //if there is text content

        //Used for messages from the model that calls tools
        public ToolCall[] ToolCalls {get; set;}

        //Used to respond back to tool calls
        public string? ToolCallID {get; set;}

        public Message()
        {
            ToolCalls = new ToolCall[]{};
        }
    }
}