using System;
using TimHanewich.AgentFramework;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TimHanewich.Foundry;

namespace AgentFrameworkTesting
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            await TestAsync();
        }

        public static async Task TestAsync()
        {
            FoundryResource fr = new FoundryResource("");
            fr.ApiKey = "";

            Agent AIDA = new Agent("You are AIDA, a smart AI");
            AIDA.FoundryResource = fr;
            AIDA.Model = "gpt-5.4-mini";

            Agent concierge = new Agent("You are a conceierge personal assistant for the user. You will help them accomplish whatever they ask for. But more specifically, you must ensure QUALITY in the response other agents provide. You are NOT intended to answer the user directly, instead you will delegate everything to AIDA. AIDA does everything, you just broker the communication between the user and AIDA to ensure the responses you provide back to the user are proper quality. If they are not, provide that feedback to AIDA to refine further before responding back to user. NOTE: the user does NOT have visibility into your conversation with AIDA, so anything you get from AIDA that you intend to share with the user you must provide back yourself (regurgitate it).");
            concierge.FoundryResource = fr;
            concierge.Model = "gpt-5.4-mini"; 
            concierge.Tools.Add(new AgentAsTool(AIDA, "AIDA", "AIDA agent, general purpose good-at-everything agent but with outputs that must be verified for quality."));
            concierge.ExecutableFunctionInvoked += FunctionInvoked;

            while (true)
            {
                //Get input
                CollectInput:
                Console.Write("> ");
                string? input = null;
                while (input == null)
                {
                    input = Console.ReadLine();
                }

                //Tokens?
                if (input == "/tokens")
                {
                    Console.WriteLine("Concierge Tokens: " + concierge.InputTokensConsumed.ToString("#,##0") +  " in, " + concierge.OutputTokensConsumed.ToString("#,##0") + " out");
                    Console.WriteLine("Concierge Tokens (recursive): " + concierge.InputTokensConsumedRecursive.ToString("#,##0") + " in, " + concierge.OutputTokensConsumedRecursive.ToString("#,##0") + " out");
                    goto CollectInput;
                }


                //Prompt
                Console.Write("Prompting Concierge... ");
                string ans = await concierge.PromptAsync(input);

                //Show
                Console.WriteLine();
                Console.WriteLine(ans);

            }
            
        }

        public static void FunctionInvoked(ExecutableFunction ef, JObject arguments)
        {
            Console.Write(ef.Name + " invoked: " + arguments.ToString(Newtonsoft.Json.Formatting.None) + " ");
        }
    }
}