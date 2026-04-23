using System;
using Newtonsoft.Json.Linq;

namespace TimHanewich.AgentFramework
{
    public delegate void ExecutableFunctionAction(ExecutableFunction function, JObject arguments);

    public delegate void TokenUsageHandler(int input_tokens, int output_tokens);
}