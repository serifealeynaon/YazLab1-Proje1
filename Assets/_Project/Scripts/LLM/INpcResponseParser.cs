namespace YazLab.LLM
{
    public interface INpcResponseParser
    {
        bool TryParse(string rawJson, out NpcAction action, out string error);
    }
}
