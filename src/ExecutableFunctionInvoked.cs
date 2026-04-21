using System;
using Newtonsoft.Json.Linq;

namespace ResponsesAgents
{
    public delegate void ExecutableFunctionInvoked(ExecutableFunction function, JObject arguments);
}