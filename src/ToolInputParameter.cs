using System;

namespace TimHanewich.AgentFramework
{
    public class ToolInputParameter
    {
        public string Name {get; set;} //The name of the input parameter (JSON property name)
        public string ParameterType {get; set;} //i.e. string, number
        public string Description {get; set;} //Description of what this parameter is

        public ToolInputParameter()
        {
            Name = "";
            ParameterType = "string";
            Description = "";
        }

        public ToolInputParameter(string name, string description, string parameter_type = "string")
        {
            Name = name;
            Description = description;
            ParameterType = parameter_type;
        }
    }
}