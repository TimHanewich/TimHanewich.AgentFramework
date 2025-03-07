# TimHanewich.AgentFramework
![banner](https://i.imgur.com/BcqRGXg.png)

I built a lightweight .NET (C#) library for building AI agents with large language models. `TimHanewich.AgentFramework` allows for you to build AI agents with large language models. It has a lightweight and modular design that makes it easy to assemble agents, provide them with tools, manage message queues, monitor token consumption, and more.

`TimHanewich.AgentFramework` is available [on NuGet](https://www.nuget.org/packages/TimHanewich.AgentFramework). To insall in your .NET project, run the following command to download:

```
dotnet add package TimHanewich.AgentFramework
```

## How to use TimHanewich.AgentFramework
It is very easy building AI agents and integrating with **an LLM service like Azure OpenAI or OpenAI directly**. For example:

```
using TimHanwich.AgentFramework;

Agent myAgent = new Agent();
myAgent.Model = new AzureOpenAICredentials(){URL = "<endpoint>", ApiKey = "<key>"};
myAgent.Messages.Add(new Message(Role.system, "You are a helpful assistant."));
myAgent.Messages.Add(new Message(Role.user, "Why is the sky blue?"));
Message aiResponse = await myAgent.PromptAsync();
Console.WriteLine(aiResponse.Content); // The sky appears blue primarily due to a phenomenon known as Rayleigh scattering...
```

You can also instead **use a local model running through Ollama** if you prefer!

```
using TimHanwich.AgentFramework;

Agent myAgent = new Agent();
myAgent.Model = new OllamaModel("llama3.2:3b");
myAgent.Messages.Add(new Message(Role.system, "You are a helpful assistant."));
myAgent.Messages.Add(new Message(Role.user, "Why is the sky blue?"));
Message aiResponse = await myAgent.PromptAsync();
Console.WriteLine(aiResponse.Content); // The sky appears blue to our eyes due to a phenomenon called scattering...
```

### Multi-Turn Conversation
The short snippet below demonstrates how a **multi-turn conversation** can be accomplished:

```
using TimHanewich.AgentFramework;

Agent myAgent = new Agent();
myAgent.Model = new OllamaModel(){ModelIdentifier = "llama3.2:3b"};
myAgent.Messages.Add(new Message(Role.system, "You are a helpful assistant."));
while (true)
{
    //Collect input
    Console.Write("> ");
    string? input = null;
    while (input == null){input = Console.ReadLine();}
    myAgent.Messages.Add(new Message(Role.user, input));

    //Prompt
    Message aiResponse = await myAgent.PromptAsync();
    myAgent.Messages.Add(aiResponse); //Append model's response to message history
    Console.WriteLine("\n" + aiResponse.Content);
}
```

### Tool Calling
This library also supports tool calling (function calling)! As long as the model you are using supports it, you can use tool calling in the following way:

Tool calling with Ollama (model `qwen2.5:7b`):
```
//Set up model
Agent myAgent = new Agent();
myAgent.Model = new OllamaModel(){ModelIdentifier = "qwen2.5:7b"}; //qwen2.5 supports tool calling
myAgent.Messages.Add(new Message(Role.system, "You are a helpful assistant."));

//Add tool
Tool CheckTemperature = new Tool("check_temperature", "Check the temperature for a given city.");
CheckTemperature.Parameters.Add(new ToolInputParameter("location", "The location to check the weather for, i.e. 'Atlanta, GA' or 'Seattle, WA'."));
myAgent.Tools.Add(CheckTemperature);

//Simulate asking question
myAgent.Messages.Add(new Message(Role.user, "What is the temperature in Virginia Beach, VA?"));

//Prompt
Message aiResponse = await myAgent.PromptAsync();
myAgent.Messages.Add(aiResponse); //Add model's response to history of messages
foreach (ToolCall tc in aiResponse.ToolCalls)
{
    Console.WriteLine("Tool '" + tc.ToolName + "' was called with parameters " + tc.Arguments.ToString(Formatting.None));
}
// Tool 'check_temperature' was called with parameters {"location":"Virginia Beach, VA"}

//In the code, handle that tool call (i.e. call to weather API)
//And then give that to the model to now use in its response to the user
float temperature = 45.4f; //we'll pretent this is the temperature in Virgina Beach, VA
myAgent.Messages.Add(new Message(Role.tool, "Temperature in Virginia Beach, VA: " + temperature.ToString() + " degrees F."));

//Now again ask the model to respond (now that it has the data it called for)
Message FinalMsg = await myAgent.PromptAsync();
Console.WriteLine(FinalMsg.Content);
```

Tool calling with Azure OpenAI (`GPT-4o-mini` used in this example):
```
//Set up model
Agent myAgent = new Agent();
myAgent.Model = new AzureOpenAICredentials(){URL = "<endpoint>", ApiKey = "<key>"};
myAgent.Messages.Add(new Message(Role.system, "You are a helpful assistant."));

//Add tool
Tool CheckTemperature = new Tool("check_temperature", "Check the temperature for a given city.");
CheckTemperature.Parameters.Add(new ToolInputParameter("location", "The location to check the weather for, i.e. 'Atlanta, GA' or 'Seattle, WA'."));
myAgent.Tools.Add(CheckTemperature);

//Simulate asking question
myAgent.Messages.Add(new Message(Role.user, "What is the temperature in Virginia Beach, VA?"));

//Prompt
Message aiResponse = await myAgent.PromptAsync();
myAgent.Messages.Add(aiResponse); //Add model's response to history of messages
foreach (ToolCall tc in aiResponse.ToolCalls)
{
    Console.WriteLine("Tool '" + tc.ToolName + "' was called with parameters " + tc.Arguments.ToString(Formatting.None));
}
// Tool 'check_temperature' was called with parameters {"location":"Virginia Beach, VA"}

//In the code, handle that tool call (i.e. call to weather API)
//And then give that to the model to now use in its response to the user
float temperature = 45.4f; //we'll pretent this is the temperature in Virgina Beach, VA
Message ToolResponse = new Message();
ToolResponse.Role = Role.tool;
ToolResponse.Content = "Temperature in Virginia Beach, VA: " + temperature.ToString() + " degrees F.";
ToolResponse.ToolCallID = aiResponse.ToolCalls[0].ID; //OpenAI expects the tool call that provides the data to point exactly to what tool call the data corresponds to (what call it is fulfilling)
myAgent.Messages.Add(ToolResponse);

//Now again ask the model to respond (now that it has the data it called for)
Message FinalMsg = await myAgent.PromptAsync();
Console.WriteLine(FinalMsg.Content);
```
Note that in the Azure OpenAI example above, the only difference is that Azure OpenAI (the OpenAI API service) expects for the tool call response to directly call out what tool call it is responding to my specifying the **ID of the tool call**. This is not something that the Ollama API does (the Ollama API does not even have unique ID's for each tool call).