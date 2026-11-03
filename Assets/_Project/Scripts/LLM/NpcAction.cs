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
}
