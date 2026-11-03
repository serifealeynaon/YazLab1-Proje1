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
    public class OllamaSchemaProperties
    {
        public OllamaPropertyType reply = new OllamaPropertyType("string");
        public OllamaPropertyType door_unlocked = new OllamaPropertyType("boolean");
        public OllamaPropertyType hostile = new OllamaPropertyType("boolean");
    }

    [Serializable]
    public class OllamaSchema
    {
        public string type = "object";
        public OllamaSchemaProperties properties = new OllamaSchemaProperties();
        public string[] required = { "reply", "door_unlocked", "hostile" };
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
        public OllamaSchema format = new OllamaSchema();
        public OllamaOptions options;
    }

    [Serializable]
    public class OllamaChatResponse
    {
        public OllamaMessage message;
    }
}
