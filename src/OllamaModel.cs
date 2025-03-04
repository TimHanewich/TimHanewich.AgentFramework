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
    public class OllamaModel : IModelConnection
    {
        public string ModelIdentifier {get; set;} //i.e. "llama3.2:3b"

        public OllamaModel()
        {
            ModelIdentifier = "";
        }

        public async Task<InferenceResponse> InvokeInferenceAsync(Message[] messages, Tool[] tools)
        {
            HttpRequestMessage req = new HttpRequestMessage();
            req.Method = HttpMethod.Post;
            req.RequestUri = new Uri("http://localhost:11434/api/chat"); //standard endpoint (assuming Ollama is running)

            JObject body = new JObject();

            //Add ollama specific stuff
            body.Add("model", ModelIdentifier);
            body.Add("stream", false);

            //Add messages
            JArray jmessages = new JArray();
            foreach (Message msg in messages)
            {
                jmessages.Add(msg.ToJSON());
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

            //To return
            InferenceResponse ToReturn = new InferenceResponse();

            //Get prompt tokens
            JToken? prompt_tokens = contentjo.SelectToken("prompt_eval_count");
            if (prompt_tokens != null)
            {
                ToReturn.PromptTokensConsumed = Convert.ToInt32(prompt_tokens.ToString());
            }

            //Get completion tokens
            JToken? completion_tokens = contentjo.SelectToken("eval_count");
            if (completion_tokens != null)
            {
                ToReturn.CompletionTokensConsumed = Convert.ToInt32(completion_tokens.ToString());
            }
            
            //Strip out message portion
            JToken? message = contentjo.SelectToken("message");
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