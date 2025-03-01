using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Net;

namespace AgentFramework
{
    public class Program
    {
        public static void Main(string[] args)
        {
            TestAsync().Wait();
        }

        public static async Task TestAsync()
        {
            Agent a = new Agent();
            a.Credentials = JsonConvert.DeserializeObject<AzureOpenAICredentials>(System.IO.File.ReadAllText(@"C:\Users\timh\Downloads\AgentFramework\credentials.json"));

            //Add system message
            a.Messages.Add(new Message(Role.system, "You are a helpful assistant."));

            

            //Add tool: check weather
            Tool tool = new Tool("check_temperature", "Check the temperature for a given location.");
            a.Tools.Add(tool);

            //Add tool: save text file
            Tool tool_savetxtfile = new Tool("save_txt_file", "Save a text file to the user's computer.");
            tool_savetxtfile.Parameters.Add(new ToolInputParameter("file_name", "The name of the file, WITHOUT the '.txt' file extension at the end."));
            tool_savetxtfile.Parameters.Add(new ToolInputParameter("file_content", "The content of the .txt file (raw text)."));
            a.Tools.Add(tool_savetxtfile);

            //Add welcoming message
            string opening_msg = "Hello! I'm here to help. What can I do for you?";
            a.Messages.Add(new Message(Role.assistant, opening_msg));
            Console.WriteLine(opening_msg);

            while (true)
            {
                //Collect input
                Console.WriteLine();
                string? input = null;
                while (input == null)
                {
                    Console.Write("> ");
                    input = Console.ReadLine();
                }
                a.Messages.Add(new Message(Role.user, input));

                //Prompt
                Prompt:
                Console.WriteLine();
                Console.Write("Thinking...");
                Message response = await a.PromptAsync();
                a.Messages.Add(response); //Add response to message array
                Console.WriteLine();
                Console.WriteLine();

                //Write content if there is some
                if (response.Content != null)
                {
                    if (response.Content != "")
                    {
                        Console.WriteLine(response.Content);
                    }
                }

                //Handle tool calls
                if (response.ToolCalls.Length > 0)
                {
                    foreach (ToolCall tc in response.ToolCalls)
                    {
                        Console.Write("Calling tool '" + tc.ToolName + "'... ");
                        string tool_call_response_payload = "";

                        //Call to the tool and save the response from that tool
                        if (tc.ToolName == "check_temperature")
                        {
                            tool_call_response_payload = await CheckTemperature(27.17f, -82.46f);
                        }
                        else if (tc.ToolName == "save_txt_file")
                        {
                            //Get file name
                            string file_name = "dummy.txt";
                            JProperty? prop_file_name = tc.Arguments.Property("file_name");
                            if (prop_file_name != null)
                            {
                                file_name = prop_file_name.Value.ToString() + ".txt";
                            }

                            //Get file content
                            string file_content = "(dummy content)";
                            JProperty? prop_file_content = tc.Arguments.Property("file_content");
                            if (prop_file_content != null)
                            {
                                file_content = prop_file_content.Value.ToString();
                            }

                            //Save file
                            SaveFile(file_name, file_content);

                            //Set success message
                            tool_call_response_payload = "File successfully saved.";
                        }

                        //Append tool response to messages
                        Message ToolResponseMessage = new Message();
                        ToolResponseMessage.Role = Role.tool;
                        ToolResponseMessage.ToolCallID = tc.ID;
                        ToolResponseMessage.Content = tool_call_response_payload;
                        a.Messages.Add(ToolResponseMessage);

                        //Confirm completion of tool call
                        Console.WriteLine("Complete!");
                    }

                    //Prompt right away (do not ask for user for input yet)
                    goto Prompt;
                }
                

                
            }
            


        }


        //////// TOOLS /////////
        
        public static async Task<string> CheckTemperature(float latitude, float longitude)
        {
            string url = "https://api.open-meteo.com/v1/forecast?latitude=" + latitude.ToString() + "&longitude=" + longitude.ToString() + "&current=temperature_2m&temperature_unit=fahrenheit";
            HttpClient hc = new HttpClient();
            HttpResponseMessage resp = await hc.GetAsync(url);
            string content = await resp.Content.ReadAsStringAsync();
            if (resp.StatusCode != HttpStatusCode.OK)
            {
                throw new Exception("Request to open-meteo.com to get temperature return code '" + resp.StatusCode.ToString() + "'. Msg: " + content);
            }
            return content; //Just return the entire body
        }

        public static void SaveFile(string file_name, string file_content)
        {
            string full_path = System.IO.Path.Combine(Environment.CurrentDirectory, file_name);
            System.IO.File.WriteAllText(file_name, file_content);
        }




    }
}