using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AgentFramework
{
    public class ToolCall
    {
        public string ToolName {get; set;}
        public JObject Arguments {get; set;}
        public string ID {get; set;}
    }
}