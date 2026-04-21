using System;
using Newtonsoft.Json.Linq;

namespace TimHanewich.AgentFramework
{
    public delegate void ExecutableFunctionInvoked(ExecutableFunction function, JObject arguments);
}