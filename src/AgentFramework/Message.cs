using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TimHanewich.AgentFramework
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

        public Message(Role role, string content)
        {
            Role = role;
            Content = content;
            ToolCalls = new ToolCall[]{};
        }

        public static Message Parse(JObject message)
        {
            Message ToReturn = new Message();

            //get role
            JProperty? role = message.Property("role");
            if (role != null)
            {
                string rolestr = role.Value.ToString();
                if (rolestr == "system")
                {
                    ToReturn.Role = Role.system;
                }
                else if (rolestr == "user")
                {
                    ToReturn.Role = Role.user;
                }
                else if (rolestr == "assistant")
                {
                    ToReturn.Role = Role.assistant;
                }
                else if (rolestr == "tool")
                {
                    ToReturn.Role = Role.tool;
                }
            }

            //Get content
            JProperty? content = message.Property("content");
            if (content != null)
            {
                if (content.Value.Type != JTokenType.Null)
                {
                    ToReturn.Content = content.Value.ToString();
                }
            }

            //Get tool calls
            JToken? tool_calls = message.SelectToken("tool_calls");
            if (tool_calls != null)
            {
                List<ToolCall> ToolCallsMadeByModel = new List<ToolCall>();
                JArray tool_calls_ja = (JArray)tool_calls;
                foreach (JObject tool_call_jo in tool_calls_ja)
                {
                    ToolCall tc = ToolCall.Parse(tool_call_jo);
                    ToolCallsMadeByModel.Add(tc);
                }
                ToReturn.ToolCalls = ToolCallsMadeByModel.ToArray();
            }

            return ToReturn;
        }

        public JObject ToJSON()
        {
            JObject ToReturn = new JObject();

            //Add role
            ToReturn.Add("role", Role.ToString());

            //add content
            ToReturn.Add("content", Content);

            //Add tool calls
            if (ToolCalls.Length > 0)
            {
                JArray tool_calls = new JArray();
                ToReturn.Add("tool_calls", tool_calls);
                foreach (ToolCall tc in ToolCalls)
                {
                    JObject tool_call = new JObject();

                    //Add type and ID
                    tool_call.Add("type", "function");
                    tool_call.Add("id", tc.ID);

                    //function
                    JObject function = new JObject();
                    tool_call.Add("function", function);
                    function.Add("name", tc.ToolName);
                    function.Add("arguments", tc.Arguments.ToString()); //add arguments as JSON-encoded string (this is how it is supposed to be, per API specification)

                    tool_calls.Add(tool_call);
                }
            }

            //Add tool call ID?
            if (ToolCallID != null)
            {
                ToReturn.Add("tool_call_id", ToolCallID);
            }

            return ToReturn;
        }

    }
}