using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using AgentFramework;
using Spectre.Console;
using TimHanewich.MicrosoftGraphHelper;
using TimHanewich.MicrosoftGraphHelper.Outlook;
using System.Web;
using System.Collections.Specialized;

namespace AIDA
{
    public class Program
    {
        public static void Main(string[] args)
        {
            RunAsync().Wait();
        }

        public static async Task RunAsync()
        {

            #region "credentials"

            //Check Config directory for config files
            string ConfigDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AIDA");
            if (System.IO.Directory.Exists(ConfigDirectory) == false)
            {
                System.IO.Directory.CreateDirectory(ConfigDirectory);
            }

            //Get AzureOpenAICredentials.json
            AzureOpenAICredentials azoai;
            string AzureOpenAICredentialsPath = Path.Combine(ConfigDirectory, "AzureOpenAICredentials.json");
            if (System.IO.File.Exists(AzureOpenAICredentialsPath) == false)
            {
                //Write the file
                System.IO.File.WriteAllText(AzureOpenAICredentialsPath, JsonConvert.SerializeObject(new AzureOpenAICredentials(), Formatting.Indented));
                
                Console.WriteLine("Your Azure OpenAI secrets were not provided! Please enter your Azure OpenAI details here: " + AzureOpenAICredentialsPath);
                return;
            }
            else
            {
                string content = System.IO.File.ReadAllText(AzureOpenAICredentialsPath);
                AzureOpenAICredentials? azoaicreds = JsonConvert.DeserializeObject<AzureOpenAICredentials>(content);
                if (azoaicreds == null)
                {
                    Console.WriteLine("Was unable to parse valid Azure OpenAI credentials out of file '" + AzureOpenAICredentialsPath + "'. Please fix the errors and try again.");
                    return;
                }
                else
                {
                    if (azoaicreds.URL == "" || azoaicreds.ApiKey == "")
                    {
                        Console.WriteLine("The Azure OpenAI credentials in '" + AzureOpenAICredentialsPath + "' were not populated. Please add your Azure OpenAI details and try again.");
                        return;
                    }
                    else
                    {
                        azoai = azoaicreds;
                    }
                }
            }

            //Try to get microsoft graph credentials from file (if they're there)
            MicrosoftGraphHelper? mgh = null;
            string GraphTokenPath = Path.Combine(ConfigDirectory, "MicrosoftGraphTokens.json");
            if (System.IO.File.Exists(GraphTokenPath))
            {
                MicrosoftGraphTokenPayload? TokenPayload = JsonConvert.DeserializeObject<MicrosoftGraphTokenPayload>(System.IO.File.ReadAllText(GraphTokenPath));
                if (TokenPayload != null)
                {
                    mgh = new MicrosoftGraphHelper();
                    mgh.LastReceivedTokenPayload = TokenPayload;
                }
            }

            //If we were unable to get the microsoft graph credentials from file, ask if they want to sign in
            if (mgh == null)
            {
                SelectionPrompt<string> WantToSignInToGraph = new SelectionPrompt<string>();
                WantToSignInToGraph.Title("I was not able to find access to your Microsoft Graph credentials. Do you want to sign in to Microsoft Outlook so I can access your calendar?");
                WantToSignInToGraph.AddChoices("Yes", "No");
                string WantToSignInToGraphAnswer = AnsiConsole.Prompt(WantToSignInToGraph);
                if (WantToSignInToGraphAnswer == "Yes")
                {
                    //Assemble the authroization URL
                    mgh = new MicrosoftGraphHelper();
                    mgh.Tenant = "consumers";
                    mgh.ClientId = Guid.Parse("e32b77a3-67df-411b-927b-f05cc6fe8d5d");
                    mgh.RedirectUrl = "https://www.google.com/";
                    mgh.Scope.Add("User.Read");
                    mgh.Scope.Add("Calendars.ReadWrite");
                    mgh.Scope.Add("Mail.Read");
                    string url = mgh.AssembleAuthorizationUrl();

                    //Ask them to sign in
                    AnsiConsole.MarkupLine("Great! Please go to the following URL and sign in, granting the necessary permissions:");
                    AnsiConsole.MarkupLine("[gray][italic]" + url + "[/][/]");
                    Console.WriteLine();
                    AnsiConsole.MarkupLine("After you do, it will redirect you to a URL. Copy + Paste that URL back to me.");
                    
                    //Collect code by stripping it out of the redirect URL
                    string? GraphAuthCode = null;
                    while (GraphAuthCode == null)
                    {

                        //Ask for full URL it redirected them to
                        Console.Write("Full URL it redirects you to (copy + paste): ");
                        string? full_url = Console.ReadLine();
                        while (full_url == null)
                        {
                            full_url = Console.ReadLine();
                        }

                        //Clip out the 'code' parameter
                        Uri AuthRedirect = new Uri(full_url);
                        NameValueCollection nvc = HttpUtility.ParseQueryString(AuthRedirect.Query); //Parse the query portion (where the parameters are in the URL) into a name value collection, splitting each param up
                        GraphAuthCode = nvc["code"];
                        if (GraphAuthCode == null)
                        {
                            AnsiConsole.MarkupLine("I'm sorry, I couldn't find the necessary 'code' parameter in that URL. Are you sure you are copying + pasting the full URL? Or perhaps it isn't working. Please try again.");
                        }
                    }

                    //Now that we have the graph auth code, redeem it for a bearer token
                    AnsiConsole.Markup("Authenticating with Microsoft Graph... ");
                    bool GraphAuthenticationSuccessful = false;
                    try
                    {
                        await mgh.GetAccessTokenAsync(GraphAuthCode);
                        AnsiConsole.MarkupLine("[green]success![/]");
                        GraphAuthenticationSuccessful = true;
                    }
                    catch (Exception ex)
                    {
                        AnsiConsole.MarkupLine("[red]" + "redeeming code for bearer token failed! Msg: " + ex.Message + "[/]");
                    }

                    //Write to file if successful, but clear out if not
                    if (GraphAuthenticationSuccessful)
                    {
                        AnsiConsole.Markup("Storing Graph credentials for future use... ");
                        System.IO.File.WriteAllText(GraphTokenPath, JsonConvert.SerializeObject(mgh.LastReceivedTokenPayload, Formatting.Indented));
                        AnsiConsole.MarkupLine("[green]saved[/]!");
                    }
                    else
                    {
                        AnsiConsole.MarkupLine("Authenticating with the Microsoft Graph was unsuccesful. Proceeding with AIDA without Microsoft Graph capabilities for now. Outlook-related tools will not work.");
                        mgh = null;
                    }
                }
            }


            #endregion
            
            //Create the agent
            Agent a = new Agent();
            a.Credentials = azoai;

            //Add system message
            a.Messages.Add(new Message(Role.system, "You are AIDA, Artificial Intelligence Desktop Assistant. Your role is to be a friendly and helpful assistant. Speak in a playful, lighthearted, and fun manner.\n\nThe current date and time is " + DateTime.Now.ToString()));

            //Add tool: check weather
            Tool tool = new Tool("check_temperature", "Check the temperature for a given location.");
            a.Tools.Add(tool);

            //Add tool: save text file
            Tool tool_savetxtfile = new Tool("save_txt_file", "Save a text file to the user's computer.");
            tool_savetxtfile.Parameters.Add(new ToolInputParameter("file_name", "The name of the file, WITHOUT the '.txt' file extension at the end."));
            tool_savetxtfile.Parameters.Add(new ToolInputParameter("file_content", "The content of the .txt file (raw text)."));
            a.Tools.Add(tool_savetxtfile);

            //Add tool: read text file
            Tool tool_readtxtfile = new Tool("read_txt_file", "Read the contents of a .txt file from the user's computer");
            tool_readtxtfile.Parameters.Add(new ToolInputParameter("file_path", "The path to the file on the computer, for example 'C:\\Users\\timh\\Downloads\\notes.txt' or '.\\notes.txt' or 'notes.txt'"));
            a.Tools.Add(tool_readtxtfile);

            //Add tool: schedule reminder (via outlook)
            Tool tool_schedulereminder = new Tool("schedule_reminder", "Schedule a reminder on the user's Outlook Calendar.");
            tool_schedulereminder.Parameters.Add(new ToolInputParameter("name", "The name of the reminder (what the user will be reminded of)."));
            tool_schedulereminder.Parameters.Add(new ToolInputParameter("datetime", "The date and time of the reminder, in the format of the following example: 3/2/2025 12:02:38 PM"));
            a.Tools.Add(tool_schedulereminder);

            //Add welcoming message
            string opening_msg = "Hello! I'm here to help. What can I do for you?";
            a.Messages.Add(new Message(Role.assistant, opening_msg));
            Console.WriteLine(opening_msg);

            //Infinite chat
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
                        else if (tc.ToolName == "read_txt_file")
                        {
                            //Get file path
                            string file_path = "?";
                            JProperty? prop_file_path = tc.Arguments.Property("file_path");
                            if (prop_file_path != null)
                            {
                                file_path = prop_file_path.Value.ToString();
                            }

                            //Does file exist?
                            if (System.IO.File.Exists(file_path))
                            {
                                //Read the file content
                                string content = System.IO.File.ReadAllText(file_path);

                                //Return it
                                tool_call_response_payload = content;
                            }
                            else
                            {
                                tool_call_response_payload = "File with path '" + file_path + "' does not exist!";
                            } 
                        }
                        else if (tc.ToolName == "schedule_reminder")
                        {

                            if (mgh != null) //if we are logged in to Microsoft Graph, we can do it!
                            {
                                //Get name of the reminder
                                string? ReminderName = null;
                                JProperty? prop_name = tc.Arguments.Property("name");
                                if (prop_name != null)
                                {
                                    ReminderName = prop_name.Value.ToString();
                                }

                                //Get the datetime
                                DateTime? ReminderDateTime = null;
                                JProperty? prop_datetime = tc.Arguments.Property("datetime");
                                if (prop_datetime != null)
                                {
                                    string ai_provided_datetime = prop_datetime.Value.ToString();
                                    try
                                    {
                                        ReminderDateTime = DateTime.Parse(ai_provided_datetime);
                                    }
                                    catch (Exception ex)
                                    {
                                        tool_call_response_payload = "Unable to parse '" + ai_provided_datetime + " datetime provided by AI into a valid DateTime. Msg: " + ex.Message;
                                    }
                                }


                                //Call to function
                                if (ReminderName != null && ReminderDateTime != null)
                                {
                                    tool_call_response_payload = await ScheduleReminder(mgh, ReminderName, ReminderDateTime.Value, GraphTokenPath);
                                }
                                else
                                {
                                    tool_call_response_payload = "Due to being unable to collect and parse at least one of the necessary parameters to make a new calendar reminder, cancelling attempt to set reminder.";
                                }
                            }
                            else
                            {
                                tool_call_response_payload = "Unable to schedule reminder because we are not logged in to Microsoft Outlook!";
                            }                            
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
                

                
            } //END INFINITE CHAT
            


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

        public static async Task<string> ScheduleReminder(MicrosoftGraphHelper mgh, string reminder_name, DateTime time, string GraphTokenLocalFilePath)
        {
            //Check if credentials need to be refreshed
            if (mgh.AccessTokenHasExpired())
            {

                //Refresh
                try
                {
                    await mgh.RefreshAccessTokenAsync();
                }
                catch (Exception ex)
                {
                    return "Unable to set reminder due to refreshing of Microsoft Graph Access tokens failing. Error message: " + ex.Message;
                }

                //Save updated credentials to file
                System.IO.File.WriteAllText(GraphTokenLocalFilePath, JsonConvert.SerializeObject(mgh.LastReceivedTokenPayload, Formatting.Indented));
            }

            //Set subject
            OutlookEvent ev = new OutlookEvent();
            ev.Subject = reminder_name;
            
            //Set start and end time time
            TimeZoneInfo estZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
            DateTime utcTime = TimeZoneInfo.ConvertTimeToUtc(time, estZone);
            ev.StartUTC = utcTime;
            ev.EndUTC = ev.StartUTC.AddMinutes(15); //15 minute appointment

            //Schedule
            try
            {
                await mgh.CreateOutlookEventAsync(ev);
            }
            catch (Exception ex)
            {
                return "Attempt to schedule reminder failed. There was an issue when creating it. Error message: " + ex.Message;
            }
            
            return "Reminder successfully scheduled.";
        }




    }
}