using System;
using System.Threading;
using System.Threading.Tasks;

namespace TimHanewich.AgentFramework
{
    public interface IModelConnection
    {
        public Task<InferenceResponse> InvokeInferenceAsync(Message[] messages, Tool[] tools, bool json_mode);
    }
}