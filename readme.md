# TimHanewich.AgentFramework
![banner](https://i.imgur.com/BcqRGXg.png)

A lightweight .NET (C#) library for building AI agents on top of the **OpenAI Responses API** via the [TimHanewich.Foundry](https://github.com/TimHanewich/TimHanewich.Foundry) SDK. `TimHanewich.AgentFramework` provides a simple abstraction layer for assembling agents with tools, orchestrating multi-agent workflows, tracking token consumption, and more.

`TimHanewich.AgentFramework` is available [on NuGet](https://www.nuget.org/packages/TimHanewich.AgentFramework). To install in your .NET project, run the following command to download:

```
dotnet add package TimHanewich.AgentFramework
```

## How to use TimHanewich.AgentFramework
It is very easy to build AI agents using a **FoundryResource** (from the [TimHanewich.Foundry](https://github.com/TimHanewich/TimHanewich.Foundry) SDK). For example:

```
using TimHanewich.AgentFramework;
using TimHanewich.Foundry;

FoundryResource fr = new FoundryResource("<endpoint>");
fr.ApiKey = "<key>";

Agent myAgent = new Agent("You are a helpful assistant.");
myAgent.FoundryResource = fr;
myAgent.Model = "gpt-4o-mini";
string response = await myAgent.PromptAsync("Why is the sky blue?");
Console.WriteLine(response); // The sky appears blue primarily due to a phenomenon known as Rayleigh scattering...
```

### Multi-Turn Conversation
The short snippet below demonstrates how a **multi-turn conversation** can be accomplished. The agent automatically maintains conversation history via the Responses API's `PreviousResponseID`:

```
using TimHanewich.AgentFramework;
using TimHanewich.Foundry;

FoundryResource fr = new FoundryResource("<endpoint>");
fr.ApiKey = "<key>";

Agent myAgent = new Agent("You are a helpful assistant.");
myAgent.FoundryResource = fr;
myAgent.Model = "gpt-4o-mini";
while (true)
{
    //Collect input
    Console.Write("> ");
    string? input = null;
    while (input == null){input = Console.ReadLine();}

    //Prompt
    string response = await myAgent.PromptAsync(input);
    Console.WriteLine("\n" + response);
}
```

### Tool Calling
This library supports tool calling through **executable functions** — tools that automatically execute when called by the model. You define a tool by extending the `ExecutableFunction` abstract class:

```
using TimHanewich.AgentFramework;
using TimHanewich.Foundry;

FoundryResource fr = new FoundryResource("<endpoint>");
fr.ApiKey = "<key>";

Agent myAgent = new Agent("You are a helpful assistant.");
myAgent.FoundryResource = fr;
myAgent.Model = "gpt-4o-mini";

//Add tool (RandomUserGenerator is a built-in ExecutableFunction example)
myAgent.Tools.Add(new RandomUserGenerator());

//Prompt - the agent will automatically call the tool and return a final response
string response = await myAgent.PromptAsync("Generate a random user for me.");
Console.WriteLine(response);
```

The `ExecutableFunction` class is abstract — you create your own tools by inheriting from it and implementing `ExecuteAsync`:

```
public class RandomUserGenerator : ExecutableFunction
{
    public RandomUserGenerator()
    {
        Name = "generate_random_user";
        Description = "Generate random user information.";
    }

    public override async Task<string> ExecuteAsync(JObject? arguments)
    {
        HttpClient hc = new HttpClient();
        HttpResponseMessage resp = await hc.GetAsync("https://randomuser.me/api/");
        return await resp.Content.ReadAsStringAsync();
    }
}
```

Tool calls are executed automatically in a loop — the agent will keep calling tools and feeding results back to the model until the model produces a final text response. You can subscribe to the `ExecutableFunctionInvoked` event to monitor tool calls as they happen:

```
myAgent.ExecutableFunctionInvoked += (ExecutableFunction ef, JObject arguments) =>
{
    Console.WriteLine(ef.Name + " invoked: " + arguments.ToString(Formatting.None));
};
```

### Multi-Agent (Agent as Tool)
You can use one agent as a tool for another agent with `AgentAsTool`. This enables **multi-agent orchestration** where a parent agent delegates tasks to sub-agents:

```
using TimHanewich.AgentFramework;
using TimHanewich.Foundry;

FoundryResource fr = new FoundryResource("<endpoint>");
fr.ApiKey = "<key>";

//Set up a sub-agent
Agent AIDA = new Agent("You are AIDA, a smart AI.");
AIDA.FoundryResource = fr;
AIDA.Model = "gpt-4o-mini";

//Set up a concierge agent that delegates to AIDA
Agent concierge = new Agent("You are a concierge. Delegate tasks to AIDA.");
concierge.FoundryResource = fr;
concierge.Model = "gpt-4o-mini";
concierge.Tools.Add(new AgentAsTool(AIDA, "AIDA", "General purpose AI agent."));

string response = await concierge.PromptAsync("Write me a haiku about the ocean.");
Console.WriteLine(response);
```

### Token Tracking
Each agent tracks its own token consumption. You can also get **recursive** token counts that include all sub-agents:

```
Console.WriteLine("Input tokens: " + myAgent.InputTokensConsumed);
Console.WriteLine("Output tokens: " + myAgent.OutputTokensConsumed);

//Recursive (includes sub-agent tokens)
Console.WriteLine("Total input tokens: " + myAgent.InputTokensConsumedRecursive);
Console.WriteLine("Total output tokens: " + myAgent.OutputTokensConsumedRecursive);
```

### Additional Settings
The `Agent` class exposes several optional settings:

```
myAgent.ReasoningEffortLevel = ReasoningEffortLevel.High;  //Control reasoning effort
myAgent.VerbosityLevel = Verbosity.Detailed;               //Control response verbosity
myAgent.WebSearchEnabled = true;                           //Enable built-in web search tool
```