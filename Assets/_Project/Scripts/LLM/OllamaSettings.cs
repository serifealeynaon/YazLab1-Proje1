using System;
using UnityEngine;

namespace YazLab.LLM
{
    [Serializable]
    public class NpcExample
    {
        public string player;
        public string reply;

        public NpcExample(string player, string reply)
        {
            this.player = player;
            this.reply = reply;
        }
    }

    [Serializable]
    public class NpcSituation
    {
        public string instruction = string.Empty;
        public NpcExample[] examples = new NpcExample[0];
    }

    [Serializable]
    public class OllamaSettings
    {
        public string baseUrl = "http://localhost:11434";
        public string model = "llama3.2:3b";
        public int timeoutSeconds = 45;
        public int invalidResponseRetries = 2;
        [Range(0f, 1.5f)] public float decisionTemperature = 0f;
        public int decisionMaxTokens = 40;
        [Range(0f, 1.5f)] public float replyTemperature = 0.3f;
        public int replyMaxTokens = 60;
        [TextArea(6, 20)] public string decisionPrompt;
        [TextArea(3, 10)] public string replyPrompt;
        public string correctWordMessage = "Doğru kelimeyi söyledim, geçebilir miyim?";
        public NpcSituation hostileSituation = new NpcSituation();
        public NpcSituation correctWordSituation = new NpcSituation();
        public NpcSituation askWordSituation = new NpcSituation();
        public NpcSituation findSealSituation = new NpcSituation();
    }
}
