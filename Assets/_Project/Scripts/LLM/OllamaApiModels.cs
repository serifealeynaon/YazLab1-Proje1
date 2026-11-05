using System;

namespace YazLab.LLM
{

    [Serializable]
    public class OllamaMessage
    {
        public string role;
        public string content;

        public OllamaMessage(string role, string content)
        {
            this.role = role;
            this.content = content;
        }
    }

    [Serializable]
    public class OllamaPropertyType
    {
        public string type;

        public OllamaPropertyType(string type)
        {
            this.type = type;
        }
    }

    [Serializable]
    public class OllamaDecisionProperties
    {
        public OllamaPropertyType kelime_geciyor = new OllamaPropertyType("boolean");
        public OllamaPropertyType hakaret_veya_tehdit = new OllamaPropertyType("boolean");
    }

    [Serializable]
    public class OllamaDecisionSchema
    {
        public string type = "object";
        public OllamaDecisionProperties properties = new OllamaDecisionProperties();
        public string[] required = { "kelime_geciyor", "hakaret_veya_tehdit" };
    }

    [Serializable]
    public class OllamaReplyProperties
    {
        public OllamaPropertyType reply = new OllamaPropertyType("string");
    }

    [Serializable]
    public class OllamaReplySchema
    {
        public string type = "object";
        public OllamaReplyProperties properties = new OllamaReplyProperties();
        public string[] required = { "reply" };
    }

    [Serializable]
    public class OllamaOptions
    {
        public float temperature;
        public int num_predict;
    }

    [Serializable]
    public class OllamaChatRequest
    {
        public string model;
        public OllamaMessage[] messages;
        public bool stream;
        public string keep_alive = "10m";
        public OllamaOptions options;
    }

    [Serializable]
    public class OllamaDecisionRequest : OllamaChatRequest
    {
        public OllamaDecisionSchema format = new OllamaDecisionSchema();
    }

    [Serializable]
    public class OllamaReplyRequest : OllamaChatRequest
    {
        public OllamaReplySchema format = new OllamaReplySchema();
    }

    [Serializable]
    public class OllamaChatResponse
    {
        public OllamaMessage message;
    }
}
