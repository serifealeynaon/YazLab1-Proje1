using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace YazLab.LLM
{
    public class OllamaDialogueService : ILlmDialogueService
    {
        private delegate bool ParseMethod<T>(string content, out T value, out string error);

        private readonly OllamaSettings settings;
        private readonly INpcResponseParser parser;
        private string sealWord;

        public OllamaDialogueService(OllamaSettings settings, INpcResponseParser parser)
        {
            this.settings = settings;
            this.parser = parser;
        }

        public void SetSealWord(string word)
        {
            sealWord = word;
        }

        public async Task PreloadAsync(CancellationToken cancellationToken)
        {
            try
            {
                var request = new OllamaChatRequest { model = settings.model, messages = new OllamaMessage[0] };
                using UnityWebRequest webRequest = CreateWebRequest(request);
                await WaitAsync(webRequest, cancellationToken);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[LLM] Model önceden yüklenemedi: {exception.Message}");
            }
        }

        public async Task<LlmResult> SendAsync(string playerMessage, CancellationToken cancellationToken)
        {
            float startTime = Time.realtimeSinceStartup;

            try
            {
                var (decision, decisionFailure) = await RequestAsync<NpcDecision>(
                    BuildDecisionRequest(playerMessage), parser.TryParseDecision, startTime, cancellationToken);

                if (decisionFailure != null)
                {
                    return decisionFailure;
                }

                bool hostile = decision.hakaret_veya_tehdit;
                bool doorUnlocked = decision.kelime_geciyor && !hostile && MessageContainsSealWord(playerMessage);

                var (reply, replyFailure) = await RequestAsync<string>(
                    BuildReplyRequest(playerMessage, doorUnlocked, hostile), parser.TryParseReply, startTime, cancellationToken);

                if (replyFailure != null)
                {
                    return replyFailure;
                }

                var action = new NpcAction { reply = reply, door_unlocked = doorUnlocked, hostile = hostile };
                return LlmResult.Success(action, Time.realtimeSinceStartup - startTime);
            }
            catch (OperationCanceledException)
            {
                return LlmResult.Failure(LlmStatus.Cancelled, "İstek iptal edildi.",
                    Time.realtimeSinceStartup - startTime);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[LLM] Beklenmeyen hata: {exception}");
                return LlmResult.Failure(LlmStatus.InvalidResponse,
                    "Model yanıtı işlenemedi. Lütfen tekrar deneyin.", Time.realtimeSinceStartup - startTime);
            }
        }

        private bool MessageContainsSealWord(string playerMessage)
        {
            return !string.IsNullOrEmpty(sealWord)
                && playerMessage.ToLowerInvariant().Contains(sealWord.ToLowerInvariant());
        }

        private async Task<(T value, LlmResult failure)> RequestAsync<T>(OllamaChatRequest request, ParseMethod<T> parse,
            float startTime, CancellationToken cancellationToken)
        {
            LlmResult failure = null;

            for (int attempt = 0; attempt <= settings.invalidResponseRetries; attempt++)
            {
                using UnityWebRequest webRequest = CreateWebRequest(request);
                await WaitAsync(webRequest, cancellationToken);
                float elapsed = Time.realtimeSinceStartup - startTime;

                if (webRequest.result != UnityWebRequest.Result.Success)
                {
                    return (default, ToFailure(webRequest, elapsed));
                }

                OllamaChatResponse response = JsonUtility.FromJson<OllamaChatResponse>(webRequest.downloadHandler.text);
                string content = response?.message?.content;
                Debug.Log($"[LLM] Ham yanıt ({elapsed:F1} sn): {content}");

                if (parse(content, out T value, out string parseError))
                {
                    return (value, null);
                }

                failure = LlmResult.Failure(LlmStatus.InvalidResponse,
                    $"Model geçersiz yanıt üretti ({parseError}) Lütfen tekrar deneyin.", elapsed);
            }

            return (default, failure);
        }

        private OllamaChatRequest BuildDecisionRequest(string playerMessage)
        {
            string word = string.IsNullOrEmpty(sealWord) ? "YOK" : sealWord;

            return new OllamaDecisionRequest
            {
                model = settings.model,
                messages = new[]
                {
                    new OllamaMessage("system", settings.decisionPrompt),
                    new OllamaMessage("user", $"Kelime: {word}. Mesaj: {playerMessage}")
                },
                options = new OllamaOptions
                {
                    temperature = settings.decisionTemperature,
                    num_predict = settings.decisionMaxTokens
                }
            };
        }

        private OllamaChatRequest BuildReplyRequest(string playerMessage, bool doorUnlocked, bool hostile)
        {
            NpcSituation situation = SelectSituation(doorUnlocked, hostile);
            var messages = new List<OllamaMessage>
            {
                new OllamaMessage("system", $"{settings.replyPrompt}Durum: {situation.instruction}")
            };

            foreach (NpcExample example in situation.examples)
            {
                messages.Add(new OllamaMessage("user", example.player));
                messages.Add(new OllamaMessage("assistant", JsonUtility.ToJson(new NpcReply { reply = example.reply })));
            }

            messages.Add(new OllamaMessage("user", doorUnlocked ? settings.correctWordMessage : playerMessage));

            return new OllamaReplyRequest
            {
                model = settings.model,
                messages = messages.ToArray(),
                options = new OllamaOptions
                {
                    temperature = settings.replyTemperature,
                    num_predict = settings.replyMaxTokens
                }
            };
        }

        private NpcSituation SelectSituation(bool doorUnlocked, bool hostile)
        {
            if (hostile)
            {
                return settings.hostileSituation;
            }

            if (doorUnlocked)
            {
                return settings.correctWordSituation;
            }

            return string.IsNullOrEmpty(sealWord) ? settings.findSealSituation : settings.askWordSituation;
        }

        private UnityWebRequest CreateWebRequest(OllamaChatRequest request)
        {
            byte[] body = Encoding.UTF8.GetBytes(JsonUtility.ToJson(request));

            return new UnityWebRequest($"{settings.baseUrl}/api/chat", UnityWebRequest.kHttpVerbPOST)
            {
                uploadHandler = new UploadHandlerRaw(body) { contentType = "application/json" },
                downloadHandler = new DownloadHandlerBuffer(),
                timeout = settings.timeoutSeconds
            };
        }

        private static async Task WaitAsync(UnityWebRequest webRequest, CancellationToken cancellationToken)
        {
            UnityWebRequestAsyncOperation operation = webRequest.SendWebRequest();

            while (!operation.isDone)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    webRequest.Abort();
                    cancellationToken.ThrowIfCancellationRequested();
                }

                await Task.Yield();
            }
        }

        private LlmResult ToFailure(UnityWebRequest webRequest, float elapsed)
        {
            Debug.LogWarning($"[LLM] İstek başarısız: {webRequest.responseCode} {webRequest.error}");
            string error = webRequest.error ?? string.Empty;

            if (error.IndexOf("timeout", StringComparison.OrdinalIgnoreCase) >= 0
                || error.IndexOf("timed out", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return LlmResult.Failure(LlmStatus.Timeout,
                    $"Model {settings.timeoutSeconds} saniyede yanıt vermedi. Tekrar deneyebilirsiniz.", elapsed);
            }

            if (webRequest.responseCode == 404)
            {
                return LlmResult.Failure(LlmStatus.ModelMissing,
                    $"'{settings.model}' modeli yüklü değil. Terminalde: ollama pull {settings.model}", elapsed);
            }

            if (webRequest.result == UnityWebRequest.Result.ConnectionError)
            {
                return LlmResult.Failure(LlmStatus.ServerUnavailable,
                    "Ollama sunucusuna ulaşılamıyor. Ollama'yı başlatıp tekrar deneyin.", elapsed);
            }

            return LlmResult.Failure(LlmStatus.ServerError,
                $"Ollama hata döndürdü (HTTP {webRequest.responseCode}). Tekrar deneyebilirsiniz.", elapsed);
        }
    }
}
