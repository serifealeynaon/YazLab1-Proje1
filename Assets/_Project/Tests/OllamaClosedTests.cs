using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using YazLab.Game;
using YazLab.Interaction;

namespace YazLab.Tests
{
    public class OllamaClosedTests
    {
        [UnityTest]
        public IEnumerator GameStaysPlayableWhenOllamaIsClosed()
        {
            using (UnityWebRequest probe = UnityWebRequest.Get("http://localhost:11434/api/version"))
            {
                probe.timeout = 2;
                yield return probe.SendWebRequest();
                Assume.That(probe.result, Is.EqualTo(UnityWebRequest.Result.ConnectionError), "Bu test Ollama kapalıyken çalışır.");
            }

            LogAssert.ignoreFailingMessages = true;
            yield return SceneManager.LoadSceneAsync("Level01");
            yield return null;

            var presenter = Object.FindFirstObjectByType<NpcDialoguePresenter>();
            var door = GameObject.Find("Door_2").GetComponent<Door>();
            presenter.Open();
            yield return null;

            var input = GameObject.Find("Input").GetComponent<InputField>();
            var send = GameObject.Find("Send").GetComponent<Button>();
            var log = GameObject.Find("Log").GetComponent<Text>();
            input.text = "Kralın mührü bende.";
            send.onClick.Invoke();
            yield return null;

            float deadline = Time.realtimeSinceStartup + 10f;
            while (!send.interactable && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Debug.Log($"[Test] Ollama kapalıyken ekrandaki metin: {log.text}");
            Assert.IsTrue(send.interactable, "Arayüz kilitli kaldı.");
            StringAssert.Contains("[Sistem]", log.text);
            StringAssert.Contains("Ollama", log.text);
            Assert.IsFalse(door.IsOpen);
        }
    }
}
