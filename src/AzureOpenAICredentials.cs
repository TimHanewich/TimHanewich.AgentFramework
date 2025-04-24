using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text.Encodings;
using System.Text;
using System.Net.Http;
using System.Net;

namespace TimHanewich.AgentFramework
{
    public class AzureOpenAICredentials : IModelConnection
    {
        public string URL {get; set;}
        public string ApiKey {get; set;}

        public AzureOpenAICredentials()
        {
            URL = "";
            ApiKey = "";
        }

        public AzureOpenAICredentials(string url, string api_key)
        {
            URL = url;
            ApiKey = api_key;
        }

        public async Task<InferenceResponse> InvokeInferenceAsync(Message[] messages, Tool[] tools, bool json_mode)
        {
            HttpRequestMessage req = new HttpRequestMessage();
            req.Method = HttpMethod.Post;
            req.RequestUri = new Uri(URL);
            req.Headers.Add("api-key", ApiKey);

            JObject body = new JObject();

            //Add messages
            JArray jmessages = new JArray();
            foreach (Message msg in messages)
            {
                //Create and add
                JObject ThisMsg = new JObject();
                jmessages.Add(ThisMsg);

                //Add role
                ThisMsg.Add("role", msg.Role.ToString());

                //add content
                ThisMsg.Add("content", msg.Content);

                //Add tool calls
                if (msg.ToolCalls.Length > 0)
                {
                    JArray tool_calls = new JArray();
                    ThisMsg.Add("tool_calls", tool_calls);
                    foreach (ToolCall tc in msg.ToolCalls)
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
                if (msg.ToolCallID != null)
                {
                    ThisMsg.Add("tool_call_id", msg.ToolCallID);
                }
            }
            body.Add("messages", jmessages);

            //Add tools
            if (tools.Length > 0)
            {
                JArray jtools = new JArray();
                foreach (Tool tool in tools)
                {
                    jtools.Add(tool.ToJSON());
                }
                body.Add("tools", jtools);
            }

            //Json mode?
            if (json_mode)
            {
                JObject response_format = new JObject();
                response_format.Add("type", "json_object");
                body.Add("response_format", response_format);
            }

            //Make API call
            req.Content = new StringContent(body.ToString(), Encoding.UTF8, "application/json"); //add body to request body
            HttpClient hc = new HttpClient();
            hc.Timeout = new TimeSpan(24, 0, 0);
            HttpResponseMessage resp = await hc.SendAsync(req);
            string content = await resp.Content.ReadAsStringAsync();
            if (resp.StatusCode != HttpStatusCode.OK)
            {
                throw new Exception("Call to model failed with code '" + resp.StatusCode.ToString() + "'. Msg: " + content);
            }
            JObject contentjo = JObject.Parse(content);

            //To return
            InferenceResponse ToReturn = new InferenceResponse();

            //Get prompt tokens
            JToken? prompt_tokens = contentjo.SelectToken("usage.prompt_tokens");
            if (prompt_tokens != null)
            {
                ToReturn.PromptTokensConsumed = Convert.ToInt32(prompt_tokens.ToString());
            }

            //Get completion tokens
            JToken? completion_tokens = contentjo.SelectToken("usage.completion_tokens");
            if (completion_tokens != null)
            {
                ToReturn.CompletionTokensConsumed = Convert.ToInt32(completion_tokens.ToString());
            }
            
            //Strip out message portion
            JToken? message = contentjo.SelectToken("choices[0].message");
            if (message == null)
            {
                throw new Exception("Property 'message' not in model's response. Full content of response: " + contentjo.ToString());
            }
            JObject ResponseMessage = (JObject)message;
            ToReturn.Message = Message.Parse(ResponseMessage);

            return ToReturn;
        }
    }
}