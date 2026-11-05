using System;
using UnityEngine;

namespace YazLab.LLM
{
    public class JsonNpcResponseParser : INpcResponseParser
    {
        private static readonly string[] DecisionFields = { "kelime_geciyor", "hakaret_veya_tehdit" };
        private static readonly string[] ReplyFields = { "reply" };

        public bool TryParseDecision(string rawJson, out NpcDecision decision, out string error)
        {
            decision = null;

            if (!TryRead(rawJson, DecisionFields, out decision, out error))
            {
                return false;
            }

            return true;
        }

        public bool TryParseReply(string rawJson, out string reply, out string error)
        {
            reply = null;

            if (!TryRead(rawJson, ReplyFields, out NpcReply parsed, out error))
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(parsed.reply))
            {
                error = "Yanıttaki 'reply' alanı boş.";
                return false;
            }

            reply = parsed.reply;
            return true;
        }

        private static bool TryRead<T>(string rawJson, string[] fields, out T value, out string error) where T : class
        {
            value = null;
            error = null;

            if (string.IsNullOrWhiteSpace(rawJson))
            {
                error = "Model boş yanıt döndürdü.";
                return false;
            }

            foreach (string field in fields)
            {
                if (!rawJson.Contains($"\"{field}\""))
                {
                    error = $"Yanıtta '{field}' alanı yok.";
                    return false;
                }
            }

            try
            {
                value = JsonUtility.FromJson<T>(rawJson);
            }
            catch (ArgumentException exception)
            {
                error = $"Geçersiz JSON: {exception.Message}";
                return false;
            }

            if (value == null)
            {
                error = "Yanıt okunamadı.";
                return false;
            }

            return true;
        }
    }
}
