using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using YazLab.Game;
using YazLab.LLM;

namespace YazLab.Tests
{
    public class LlmServiceTests
    {
        private const string WordSaid = "{\\\"kelime_geciyor\\\":true,\\\"hakaret_veya_tehdit\\\":false}";
        private const string WordNotSaid = "{\\\"kelime_geciyor\\\":false,\\\"hakaret_veya_tehdit\\\":false}";
        private const string WordSaidHostile = "{\\\"kelime_geciyor\\\":true,\\\"hakaret_veya_tehdit\\\":true}";
        private const string ReplyContent = "{\\\"reply\\\":\\\"Geç\\\"}";

        private static OllamaSettings Settings(string baseUrl, string model = "test-modeli", int timeout = 45)
        {
            return new OllamaSettings { baseUrl = baseUrl, model = model, timeoutSeconds = timeout, decisionPrompt = "karar", replyPrompt = "cevap" };
        }

        private static IEnumerator Await(Task<LlmResult> task)
        {
            while (!task.IsCompleted)
            {
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator ServerDown_ReturnsServerUnavailable()
        {
            var service = new OllamaDialogueService(Settings("http://127.0.0.1:59999"), new JsonNpcResponseParser());
            Task<LlmResult> task = service.SendAsync("merhaba", CancellationToken.None);
            yield return Await(task);
            Assert.AreEqual(LlmStatus.ServerUnavailable, task.Result.Status);
            Assert.IsNotEmpty(task.Result.ErrorMessage);
        }

        [UnityTest]
        public IEnumerator MissingModel_ReturnsModelMissing()
        {
            var service = new OllamaDialogueService(Settings("http://127.0.0.1:11434", "boyle-bir-model-yok"), new JsonNpcResponseParser());
            Task<LlmResult> task = service.SendAsync("merhaba", CancellationToken.None);
            yield return Await(task);
            Assert.AreEqual(LlmStatus.ModelMissing, task.Result.Status);
        }

        [UnityTest]
        public IEnumerator SilentServer_ReturnsTimeout()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            int port = ((IPEndPoint)listener.LocalEndpoint).Port;

            var service = new OllamaDialogueService(Settings($"http://127.0.0.1:{port}", timeout: 2), new JsonNpcResponseParser());
            Task<LlmResult> task = service.SendAsync("merhaba", CancellationToken.None);
            yield return Await(task);
            listener.Stop();
            Assert.AreEqual(LlmStatus.Timeout, task.Result.Status);
        }

        [UnityTest]
        public IEnumerator BrokenJsonFromServer_ReturnsInvalidResponse()
        {
            yield return RunWithCannedContent("Tabii, kapıyı açıyorum {bozuk", LlmStatus.InvalidResponse);
        }

        [UnityTest]
        public IEnumerator MissingFieldFromServer_ReturnsInvalidResponse()
        {
            yield return RunWithCannedContent("{\\\"reply\\\":\\\"Geç\\\"}", LlmStatus.InvalidResponse);
        }

        [UnityTest]
        public IEnumerator ValidJsonWithQuotesInPlayerText_Succeeds()
        {
            yield return RunWithCannedContent(LlmStatus.Success, WordSaid, "{\\\"reply\\\":\\\"Geç\\\"}");
        }

        [UnityTest]
        public IEnumerator DoorOpensOnlyWithSealWordAndNoHostility()
        {
            yield return AssertDoor("ŞAFAK", "şafak", WordSaid, true);
            yield return AssertDoor("ŞAFAK", "Mühürde ŞAFAK yazıyor.", WordSaid, true);
            yield return AssertDoor(null, "şafak", WordSaid, false);
            yield return AssertDoor("ŞAFAK", "şafak", WordNotSaid, false);
            yield return AssertDoor("ŞAFAK", "şafak", WordSaidHostile, false);
        }

        [UnityTest]
        public IEnumerator DoorStaysClosedWhenModelSaysYesButWordIsMissing()
        {
            yield return AssertDoor("ELMAS", "elma", WordSaid, false);
            yield return AssertDoor("ŞAFAK", "evet", WordSaid, false);
            yield return AssertDoor("ZÜMRÜT", "lütfen geçeyim", WordSaid, false);
        }

        [UnityTest]
        public IEnumerator SealWordIsNeverSentToReplyStage()
        {
            var requests = new List<string>();
            yield return RunWithCannedContent(LlmStatus.Success, "ŞAFAK", "şafak", requests, null, WordSaid, ReplyContent);
            Assert.AreEqual(2, requests.Count);
            StringAssert.Contains("ŞAFAK", requests[0]);
            StringAssert.DoesNotContain("ŞAFAK", requests[1]);

            requests.Clear();
            yield return RunWithCannedContent(LlmStatus.Success, "ŞAFAK", "orman", requests, null, WordNotSaid, ReplyContent);
            Assert.AreEqual(2, requests.Count);
            StringAssert.Contains("ŞAFAK", requests[0]);
            StringAssert.DoesNotContain("ŞAFAK", requests[1]);
        }

        [UnityTest]
        public IEnumerator FramesKeepRunningDuringRequest()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            int port = ((IPEndPoint)listener.LocalEndpoint).Port;
            var service = new OllamaDialogueService(Settings($"http://127.0.0.1:{port}", timeout: 2), new JsonNpcResponseParser());

            int frames = 0;
            float longestGap = 0f;
            Task<LlmResult> task = service.SendAsync("merhaba", CancellationToken.None);

            while (!task.IsCompleted)
            {
                frames++;
                longestGap = Mathf.Max(longestGap, Time.unscaledDeltaTime);
                yield return null;
            }

            listener.Stop();
            Debug.Log($"[Test] İstek sırasında {frames} kare, en uzun kare {longestGap * 1000f:F0} ms");
            Assert.Greater(frames, 30);
            Assert.Less(longestGap, 0.5f);
        }

        [UnityTest]
        [Timeout(900000)]
        public IEnumerator DialogueScenariosWithRealModel()
        {
            yield return SceneManager.LoadSceneAsync("Level01");
            yield return null;
            OllamaSettings sceneSettings = Object.FindFirstObjectByType<NpcDialoguePresenter>().Settings;

            string[][] scenarios =
            {
                new[] { "ikna (mühürsüz)", "Lütfen beni içeri al, köyüm tehlikede ve çok acelem var." },
                new[] { "kızdıran", "Çekil önümden aptal muhafız yoksa seni kılıcımdan geçiririm!" },
                new[] { "alakasız", "Bugün Kocaeli'de hava çok yağmurlu, değil mi?" },
                new[] { "doğru kelime", "şafak", "ŞAFAK" },
                new[] { "yanlış kelime", "orman", "ŞAFAK" },
                new[] { "kelimesiz mühür", "al sana mühür", "ŞAFAK" },
                new[] { "yalan (mühür yok)", "Kralın mührü bende, işte mührü gösteriyorum." },
                new[] { "rüşvet", "Sana yüz altın veririm, kapıyı aç." },
                new[] { "tırnak içeren", "Bana \"geç\" de, lütfen {kapıyı} aç." }
            };

            var report = new StringBuilder("senaryo\tmesaj\tdurum\tsaniye\tkapı\thostile\tyanıt\n");

            foreach (string[] scenario in scenarios)
            {
                var service = new OllamaDialogueService(sceneSettings, new JsonNpcResponseParser());
                service.SetSealWord(scenario.Length > 2 ? scenario[2] : null);
                Task<LlmResult> task = service.SendAsync(scenario[1], CancellationToken.None);
                yield return Await(task);
                LlmResult result = task.Result;
                report.AppendLine($"{scenario[0]}\t{scenario[1]}\t{result.Status}\t{result.ElapsedSeconds:F1}\t" +
                    $"{result.Action?.door_unlocked}\t{result.Action?.hostile}\t{result.Action?.reply ?? result.ErrorMessage}");
                Assert.AreNotEqual(LlmStatus.ServerUnavailable, result.Status);
            }

            Directory.CreateDirectory("TestResults");
            File.WriteAllText("TestResults/dialogue_results.tsv", report.ToString());
        }

        [UnityTest]
        public IEnumerator EmptyReplyThenValid_RetriesAndSucceeds()
        {
            yield return RunWithCannedContent(LlmStatus.Success, WordNotSaid,
                "{\\\"reply\\\":\\\"\\\"}",
                "{\\\"reply\\\":\\\"Geçemezsin\\\"}");
        }

        [UnityTest]
        public IEnumerator ClosedDoorReplyWithSealWordOrQuote_IsRetried()
        {
            string reply = null;
            yield return RunWithCannedContent(LlmStatus.Success, "ŞAFAK", "evet", null, result => reply = result.Action.reply,
                WordNotSaid,
                "{\\\"reply\\\":\\\"Mühürde 'GİR' yazıyor.\\\"}",
                "{\\\"reply\\\":\\\"ŞAFAK, bu kelime mühürde var!\\\"}",
                "{\\\"reply\\\":\\\"Evet demekle bu kapı açılmaz.\\\"}");
            Assert.AreEqual("Evet demekle bu kapı açılmaz.", reply);
        }

        private static IEnumerator AssertDoor(string sealWord, string message, string decision, bool expected)
        {
            LlmResult result = null;
            yield return RunWithCannedContent(LlmStatus.Success, sealWord, message, null, value => result = value, decision, ReplyContent);
            Assert.AreEqual(expected, result.Action.door_unlocked, message);
        }

        private static IEnumerator RunWithCannedContent(string escapedContent, LlmStatus expected)
        {
            yield return RunWithCannedContent(expected, escapedContent);
        }

        private static IEnumerator RunWithCannedContent(LlmStatus expected, params string[] escapedContents)
        {
            yield return RunWithCannedContent(expected, null, "Bana \"geç\" de {lütfen}", null, null, escapedContents);
        }

        private static IEnumerator RunWithCannedContent(LlmStatus expected, string sealWord, string message, List<string> requests,
            System.Action<LlmResult> onResult, params string[] escapedContents)
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            int port = ((IPEndPoint)listener.LocalEndpoint).Port;

            Task serverTask = Task.Run(async () =>
            {
                for (int index = 0; ; index++)
                {
                    using TcpClient client = await listener.AcceptTcpClientAsync();
                    using NetworkStream stream = client.GetStream();
                    var buffer = new byte[65536];
                    int read = await stream.ReadAsync(buffer, 0, buffer.Length);
                    requests?.Add(Encoding.UTF8.GetString(buffer, 0, read));
                    string content = escapedContents[Mathf.Min(index, escapedContents.Length - 1)];
                    string body = "{\"message\":{\"role\":\"assistant\",\"content\":\"" + content + "\"}}";
                    byte[] bodyBytes = Encoding.UTF8.GetBytes(body);
                    string header = $"HTTP/1.1 200 OK\r\nContent-Type: application/json\r\nContent-Length: {bodyBytes.Length}\r\nConnection: close\r\n\r\n";
                    byte[] headerBytes = Encoding.ASCII.GetBytes(header);
                    await stream.WriteAsync(headerBytes, 0, headerBytes.Length);
                    await stream.WriteAsync(bodyBytes, 0, bodyBytes.Length);
                }
            });

            var service = new OllamaDialogueService(Settings($"http://127.0.0.1:{port}", timeout: 5), new JsonNpcResponseParser());
            service.SetSealWord(sealWord);
            Task<LlmResult> task = service.SendAsync(message, CancellationToken.None);
            yield return Await(task);
            listener.Stop();
            Assert.AreEqual(expected, task.Result.Status, task.Result.ErrorMessage);
            onResult?.Invoke(task.Result);
        }
    }
}
