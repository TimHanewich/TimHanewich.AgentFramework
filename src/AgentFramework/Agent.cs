using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http;
using Newtonsoft.Json.Linq;
using System.Text;
using System.Text.Encodings;

namespace AgentFramework
{
    public class Agent
    {
        public List<Message> Messages {get; set;}
        public List<Tool> Tools {get; set;}
        public AzureOpenAICredentials Credentials {get; set;}

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
            Credentials = new AzureOpenAICredentials();
        }

        //Ask model to generate the next message, given the current context of messages
        public async Task<Message> PromptAsync()
        {
            HttpRequestMessage req = new HttpRequestMessage();
            req.Method = HttpMethod.Post;
            req.RequestUri = new Uri(Credentials.URL);
            req.Headers.Add("api-key", Credentials.ApiKey);

            JObject body = new JObject();

            //Add messages
            JArray messages = new JArray();
            foreach (Message msg in Messages)
            {
                messages.Add(msg.ToJSON());
            }
            body.Add("messages", messages);

            //Add tools
            if (Tools.Count > 0)
            {
                JArray tools = new JArray();
                foreach (Tool tool in Tools)
                {
                    tools.Add(tool.ToJSON());
                }
                body.Add("tools", tools);
            }

            //Make API call
            req.Content = new StringContent(body.ToString(), Encoding.UTF8, "application/json"); //add body to request body
            HttpClient hc = new HttpClient();
            HttpResponseMessage resp = await hc.SendAsync(req);
            string content = await resp.Content.ReadAsStringAsync();
            if (resp.StatusCode != HttpStatusCode.OK)
            {
                throw new Exception("Call to model failed with code '" + resp.StatusCode.ToString() + "'. Msg: " + content);
            }

            JObject contentjo = JObject.Parse(content);

            //Get prompt tokens
            JToken? prompt_tokens = contentjo.SelectToken("usage.prompt_tokens");
            if (prompt_tokens != null)
            {
                _CumulativePromptTokens = _CumulativePromptTokens + Convert.ToInt32(prompt_tokens.ToString());
            }

            //Get completion tokens
            JToken? completion_tokens = contentjo.SelectToken("usage.completion_tokens");
            if (completion_tokens != null)
            {
                _CumulativeCompletionTokens = _CumulativeCompletionTokens + Convert.ToInt32(completion_tokens.ToString());
            }
            
            //Strip out message portion
            JToken? message = contentjo.SelectToken("choices[0].message");
            if (message == null)
            {
                throw new Exception("Property 'message' not in model's response. Full content of response: " + contentjo.ToString());
            }
            JObject ResponseMessage = (JObject)message;
            
            //Parse the message
            Message ToReturn = Message.Parse(ResponseMessage);

            return ToReturn;
        }

        
    }
}