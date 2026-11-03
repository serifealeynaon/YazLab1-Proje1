using System;
using UnityEngine;

namespace YazLab.LLM
{
    [Serializable]
    public class OllamaSettings
    {
        public string baseUrl = "http://localhost:11434";
        public string model = "llama3.2:3b";
        public int timeoutSeconds = 45;
        public int maxHistoryMessages = 8;
        [Range(0f, 1.5f)] public float temperature = 0.4f;
        public int maxTokens = 120;
        [TextArea(6, 20)] public string systemPrompt;
    }
}
