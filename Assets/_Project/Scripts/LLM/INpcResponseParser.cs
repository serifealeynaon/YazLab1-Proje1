namespace YazLab.LLM
{
    public interface INpcResponseParser
    {
        bool TryParseDecision(string rawJson, out NpcDecision decision, out string error);
        bool TryParseReply(string rawJson, out string reply, out string error);
    }
}
