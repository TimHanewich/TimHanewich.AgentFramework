using System;

namespace AgentFramework
{
    public class AzureOpenAICredentials
    {
        public string URL {get; set;}
        public string ApiKey {get; set;}

        public AzureOpenAICredentials()
        {
            URL = "";
            ApiKey = "";
        }
    }
}