using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TimHanewich.AgentFramework
{
    public class ToolCall
    {
        public string ToolName {get; set;}
        public JObject Arguments {get; set;}
        public string? ID {get; set;}

        public ToolCall()
        {
            ToolName = "";
            Arguments = new JObject();
            ID = null;
        }

        public static ToolCall Parse(JObject tool_call)
        {
            ToolCall ToReturn = new ToolCall();

            //Get tool name
            JToken? name = tool_call.SelectToken("function.name");
            if (name != null)
            {
                ToReturn.ToolName = name.ToString();
            }

            //Get arguments
            JToken? arguments = tool_call.SelectToken("function.arguments");
            if (arguments != null)
            {
                string arguments_json = arguments.ToString();
                ToReturn.Arguments = JObject.Parse(arguments_json);
            }

            //get tool call ID
            JProperty? id = tool_call.Property("id");
            if (id != null)
            {
                ToReturn.ID = id.Value.ToString();
            }

            return ToReturn;
        }


    }
}