namespace YazLab.LLM
{
    public enum LlmStatus
    {
        Success,
        ServerUnavailable,
        ModelMissing,
        Timeout,
        InvalidResponse,
        ServerError,
        Cancelled
    }

    public class LlmResult
    {
        public LlmStatus Status { get; }
        public NpcAction Action { get; }
        public string ErrorMessage { get; }
        public float ElapsedSeconds { get; }

        public bool IsSuccess => Status == LlmStatus.Success;

        private LlmResult(LlmStatus status, NpcAction action, string errorMessage, float elapsedSeconds)
        {
            Status = status;
            Action = action;
            ErrorMessage = errorMessage;
            ElapsedSeconds = elapsedSeconds;
        }

        public static LlmResult Success(NpcAction action, float elapsedSeconds)
        {
            return new LlmResult(LlmStatus.Success, action, null, elapsedSeconds);
        }

        public static LlmResult Failure(LlmStatus status, string errorMessage, float elapsedSeconds)
        {
            return new LlmResult(status, null, errorMessage, elapsedSeconds);
        }
    }
}
