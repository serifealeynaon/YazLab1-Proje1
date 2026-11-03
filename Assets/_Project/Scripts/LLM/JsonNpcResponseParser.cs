using System;
using UnityEngine;

namespace YazLab.LLM
{
    public class JsonNpcResponseParser : INpcResponseParser
    {
        private static readonly string[] RequiredFields = { "reply", "door_unlocked", "hostile" };

        public bool TryParse(string rawJson, out NpcAction action, out string error)
        {
            action = null;
            error = null;

            if (string.IsNullOrWhiteSpace(rawJson))
            {
                error = "Model boş yanıt döndürdü.";
                return false;
            }

            foreach (string field in RequiredFields)
            {
                if (!rawJson.Contains($"\"{field}\""))
                {
                    error = $"Yanıtta '{field}' alanı yok.";
                    return false;
                }
            }

            try
            {
                action = JsonUtility.FromJson<NpcAction>(rawJson);
            }
            catch (ArgumentException exception)
            {
                error = $"Geçersiz JSON: {exception.Message}";
                return false;
            }

            if (action == null || string.IsNullOrWhiteSpace(action.reply))
            {
                action = null;
                error = "Yanıttaki 'reply' alanı boş.";
                return false;
            }

            return true;
        }
    }
}
