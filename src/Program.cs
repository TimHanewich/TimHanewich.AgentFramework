using System;

namespace AgentFramework
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Tool t = new Tool("search_web", "Search the world wide web for information about a particular topic.");
            t.Parameters.Add(new ToolInputParameter("subject", "The term to search for."));
        }
    }
}