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
        private readonly OllamaSettings settings;
        private readonly INpcResponseParser parser;
        private readonly List<OllamaMessage> history = new List<OllamaMessage>();

        public OllamaDialogueService(OllamaSettings settings, INpcResponseParser parser)
        {
            this.settings = settings;
            this.parser = parser;
        }

        public void ResetConversation()
        {
            history.Clear();
        }

        public async Task PreloadAsync(CancellationToken cancellationToken)
        {
            try
            {
                OllamaChatRequest request = BuildRequest(null);
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
                LlmResult result = null;

                for (int attempt = 0; attempt <= settings.invalidResponseRetries; attempt++)
                {
                    result = await SendOnceAsync(playerMessage, startTime, cancellationToken);

                    if (result.Status != LlmStatus.InvalidResponse)
                    {
                        break;
                    }
                }

                return result;
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

        private async Task<LlmResult> SendOnceAsync(string playerMessage, float startTime, CancellationToken cancellationToken)
        {
            OllamaChatRequest request = BuildRequest(playerMessage);
            using UnityWebRequest webRequest = CreateWebRequest(request);
            await WaitAsync(webRequest, cancellationToken);

            float elapsed = Time.realtimeSinceStartup - startTime;

            if (webRequest.result != UnityWebRequest.Result.Success)
            {
                return ToFailure(webRequest, elapsed);
            }

            OllamaChatResponse response = JsonUtility.FromJson<OllamaChatResponse>(webRequest.downloadHandler.text);
            string content = response?.message?.content;
            Debug.Log($"[LLM] Ham yanıt ({elapsed:F1} sn): {content}");

            if (!parser.TryParse(content, out NpcAction action, out string parseError))
            {
                return LlmResult.Failure(LlmStatus.InvalidResponse,
                    $"Model geçersiz yanıt üretti ({parseError}) Lütfen tekrar deneyin.", elapsed);
            }

            Remember(playerMessage, content);
            return LlmResult.Success(action, elapsed);
        }

        private OllamaChatRequest BuildRequest(string playerMessage)
        {
            var messages = new List<OllamaMessage>();

            if (playerMessage != null)
            {
                messages.Add(new OllamaMessage("system", settings.systemPrompt));
                messages.AddRange(history);
                messages.Add(new OllamaMessage("user", playerMessage));
            }

            return new OllamaChatRequest
            {
                model = settings.model,
                messages = messages.ToArray(),
                stream = false,
                options = new OllamaOptions
                {
                    temperature = settings.temperature,
                    num_predict = settings.maxTokens
                }
            };
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

        private void Remember(string playerMessage, string assistantJson)
        {
            history.Add(new OllamaMessage("user", playerMessage));
            history.Add(new OllamaMessage("assistant", assistantJson));

            while (history.Count > settings.maxHistoryMessages)
            {
                history.RemoveAt(0);
            }
        }
    }
}
