using System;
using TimHanewich.Foundry;
using TimHanewich.Foundry.OpenAI.Responses;

namespace TimHanewich.AgentFramework
{
    public class Agent
    {
        //private vars
        private string? SystemPrompt {get; set;}                          //eventually "flushed"
        private string? PreviousResponseID {get; set;}
        private int _InputTokensConsumed;
        private int _OutputTokensConsumed;
        
        //public vars
        public FoundryResource? FoundryResource {get; set;}
        public string? Model {get; set;}
        public List<ExecutableFunction> Tools {get; set;}
        public int InputTokensConsumed {get{return _InputTokensConsumed;}}
        public int OutputTokensConsumed {get{return _OutputTokensConsumed;}}

        //Settings
        public ReasoningEffortLevel? ReasoningEffortLevel {get; set;}
        public Verbosity? VerbosityLevel {get; set;}
        public bool WebSearchEnabled {get; set;}
        
        //Events
        public event ExecutableFunctionHandler? ExecutableFunctionInvoked;     //It evoked a function
        public event ExecutableFunctionHandler? ExecutableFunctionReturned;    //An executable function that was invoked (called) returned
        public event Action? InferenceRequested;                               //It is calling to the OpenAI Responses API now for inference
        public event TokenUsageHandler? InferenceReceived;                     //It has receved the resonse from OpenAI API

        public Agent()
        {
            Tools = new List<ExecutableFunction>();
        }

        public Agent(string system_prompt)
        {
            SystemPrompt = system_prompt;
            Tools = new List<ExecutableFunction>();
        }

        public async Task<string> PromptAsync(string prompt)
        {
            //Check
            if (FoundryResource == null)
            {
                throw new Exception("Must set 'FoundryResource' property.");
            }
            if (Model == null)
            {
                throw new Exception("Must set 'Model' parameter.");
            }

            //Setup
            ResponseRequest rr = new ResponseRequest();
            rr.Model = Model;
            rr.Background = false;
            if (ReasoningEffortLevel.HasValue)
            {
                rr.ReasoningEffort = ReasoningEffortLevel;
            }
            if (VerbosityLevel.HasValue)
            {
                rr.VerbosityLevel = VerbosityLevel;
            }

            //Set up first inputs: system prompt
            if (SystemPrompt != null)
            {
                rr.Inputs.Add(new Message(Role.developer, SystemPrompt));
                SystemPrompt = null;
            }

            //Set up first input: user prompt
            rr.Inputs.Add(new Message(Role.user, prompt));

            //Set up tools (functions)
            foreach (ExecutableFunction ef in Tools)
            {
                Function ThisFunc = new Function();
                ThisFunc.Name = ef.Name;
                ThisFunc.Description = ef.Description;
                foreach (FunctionInputParameter fip in ef.InputParameters)
                {
                    ThisFunc.Parameters.Add(fip);
                }
                rr.Tools.Add(ThisFunc);
            }

            //Web search enabled?
            if (WebSearchEnabled)
            {
                rr.Tools.Add(new WebSearchTool());
            }

            //Collect!
            List<string> CollectedResponseToReturn = new List<string>();
            while (true)
            {
                //Set previous response Id
                rr.PreviousResponseID = PreviousResponseID;

                //Call!
                InferenceRequested?.Invoke(); //raise event that we are now requesting inference
                Response resp = await FoundryResource.CreateResponseAsync(rr);
                InferenceReceived?.Invoke(resp.InputTokensConsumed, resp.OutputTokensConsumed); //raise event that inference now received
                _InputTokensConsumed = _InputTokensConsumed + resp.InputTokensConsumed;
                _OutputTokensConsumed = _OutputTokensConsumed + resp.OutputTokensConsumed;

                //Immediately prepare for next go around
                PreviousResponseID = resp.Id;
                rr.Inputs.Clear();
                
                //Handle outputs
                List<FunctionCallOutput> FCOs = new List<FunctionCallOutput>();
                foreach (Exchange ex in resp.Outputs)
                {
                    if (ex is Message msg)
                    {
                        if (msg.Text != null)
                        {
                            CollectedResponseToReturn.Add(msg.Text);
                        }
                    }
                    else if (ex is FunctionCall fc)
                    {
                        foreach (ExecutableFunction ef in Tools)
                        {
                            if (ef.Name == fc.FunctionName)
                            {
                                //Execute
                                ExecutableFunctionInvoked?.Invoke(ef, fc.Arguments);                      //if there are any subscribers (question mark), raise
                                string ToolExecutionResponse = await ef.ExecuteAsync(fc.Arguments);
                                ExecutableFunctionReturned?.Invoke(ef, fc.Arguments);                     //Raise event to let know it returned

                                //Add it back
                                rr.Inputs.Add(new FunctionCallOutput(fc.CallId, ToolExecutionResponse));
                            }
                        }
                    }
                }

                //If there was no function outputs to return... the model is done! Return the message
                if (rr.Inputs.Count == 0)
                {
                    string ToReturn = "";
                    foreach (string s in CollectedResponseToReturn)
                    {
                        ToReturn = ToReturn + s + "\n\n";
                    }
                    ToReturn = ToReturn.Substring(0, ToReturn.Length - 2);
                    return ToReturn;
                }
            }

            

        

        }
    
        //Recursive input tokens consumed (includes sub-agents)
        public int InputTokensConsumedRecursive
        {
            get
            {
                int ToReturn = InputTokensConsumed;
                foreach (ExecutableFunction ef in Tools)
                {
                    if (ef is AgentAsTool aat)
                    {
                        ToReturn = ToReturn + aat.InnerAgent.InputTokensConsumed;
                    }
                }
                return ToReturn;
            }
        }

        //Recursive output tokens consumed (includes sub-agents)
        public int OutputTokensConsumedRecursive
        {
            get
            {
                int ToReturn = OutputTokensConsumed;
                foreach (ExecutableFunction ef in Tools)
                {
                    if (ef is AgentAsTool aat)
                    {
                        ToReturn = ToReturn + aat.InnerAgent.OutputTokensConsumed;
                    }
                }
                return ToReturn;
            }
        }
    }
}