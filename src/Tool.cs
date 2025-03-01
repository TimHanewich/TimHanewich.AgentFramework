using System;
using System.Collections.Generic;

namespace AgentFramework
{
    public class Tool
    {
        public string Name {get; set;}
        public string Description {get; set;}
        public List<ToolInputParameter> Parameters {get; set;}

        public Tool()
        {
            Name = "";
            Description = "";
            Parameters = new List<ToolInputParameter>();
        }

        public Tool(string name, string description)
        {
            Name = name;
            Description = description;
            Parameters = new List<ToolInputParameter>();
        }
    }
}