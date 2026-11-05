using System;

namespace YazLab.LLM
{
    [Serializable]
    public class NpcAction
    {
        public string reply;
        public bool door_unlocked;
        public bool hostile;
    }

    [Serializable]
    public class NpcDecision
    {
        public bool kelime_geciyor;
        public bool hakaret_veya_tehdit;
    }

    [Serializable]
    public class NpcReply
    {
        public string reply;
    }
}
