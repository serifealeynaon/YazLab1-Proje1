using System.Threading;
using System.Threading.Tasks;

namespace YazLab.LLM
{
    public interface ILlmDialogueService
    {
        Task<LlmResult> SendAsync(string playerMessage, CancellationToken cancellationToken);
        Task PreloadAsync(CancellationToken cancellationToken);
        void ResetConversation();
        void SetContext(string context);
    }
}
