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
            
            //Strip out message portion
            JObject contentjo = JObject.Parse(content);
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