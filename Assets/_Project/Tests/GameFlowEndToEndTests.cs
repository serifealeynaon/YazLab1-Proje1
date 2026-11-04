using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using YazLab.Companion;
using YazLab.Game;
using YazLab.Interaction;
using YazLab.UI;

namespace YazLab.Tests
{
    public class GameFlowEndToEndTests
    {
        private const string SealMessage = "Kralın mührü bende, işte mührü gösteriyorum.";
        private string screenshotFolder;

        [UnityTest]
        [Timeout(300000)]
        public IEnumerator FullScenarioFromStartToEnd()
        {
            screenshotFolder = Path.Combine(Application.persistentDataPath, "Screenshots");
            Directory.CreateDirectory(screenshotFolder);

            yield return SceneManager.LoadSceneAsync("Level01");
            yield return new WaitForSecondsRealtime(1f);

            var companion = Object.FindFirstObjectByType<CompanionController>();
            var player = Object.FindFirstObjectByType<CharacterController>();
            var switchTarget = Object.FindFirstObjectByType<SwitchInteractable>();
            var presenter = Object.FindFirstObjectByType<NpcDialoguePresenter>();
            var firstDoor = GameObject.Find("Door_1").GetComponent<Door>();
            var secondDoor = GameObject.Find("Door_2").GetComponent<Door>();
            var seal = Object.FindFirstObjectByType<SealPickup>();

            Assert.IsTrue(companion.Agent.isOnNavMesh, "Companion oyun başında NavMesh üzerinde değil.");
            yield return Capture("01_baslangic");

            companion.Command(switchTarget);
            yield return new WaitForSecondsRealtime(2f);
            yield return Capture("02_companion_yolda");
            yield return WaitUntil(() => firstDoor.IsOpen, 30f, "Birinci kapı açılmadı.");
            yield return new WaitForSecondsRealtime(2f);
            yield return Capture("03_birinci_kapi_acik");

            Teleport(player, new Vector3(0f, 1.1f, 4f));
            yield return WaitUntil(() => Vector3.Distance(companion.transform.position, player.transform.position) < 4f,
                20f, "Companion açılan kapıdan oyuncuya ulaşamadı.");
            yield return Capture("04_companion_eslik_ediyor");

            presenter.Open();
            yield return null;
            var input = GameObject.Find("Input").GetComponent<InputField>();
            var send = GameObject.Find("Send").GetComponent<Button>();

            yield return Send(input, send, "Merhaba, geçmek istiyorum.", capture: true);
            Assert.IsFalse(secondDoor.IsOpen, "Kapı mühür olmadan açıldı.");

            yield return Send(input, send, SealMessage, capture: false);
            Assert.IsFalse(secondDoor.IsOpen, "Kapı mühür alınmadan açıldı.");

            presenter.Close();
            Teleport(player, seal.transform.position + Vector3.up);
            yield return new WaitForSecondsRealtime(1f);
            yield return Capture("06_muhur_yanida");
            seal.Collect();
            Assert.IsFalse(seal.gameObject.activeSelf, "Mühür alındıktan sonra sahnede kaldı.");
            Teleport(player, new Vector3(0f, 1.1f, 4f));
            presenter.Open();
            yield return null;

            for (int attempt = 0; attempt < 3 && !secondDoor.IsOpen; attempt++)
            {
                yield return Send(input, send, SealMessage, capture: false);
            }

            Assert.IsTrue(secondDoor.IsOpen, "Model mührü görünce kapıyı açmadı.");
            yield return Capture("07_muhafiz_kapiyi_acti");

            presenter.Close();
            yield return new WaitForSecondsRealtime(2f);
            Teleport(player, new Vector3(0f, 1.1f, 18f));
            yield return WaitUntil(() => Time.timeScale == 0f, 5f, "Oyun bitiş ekranına geçmedi.");
            yield return Capture("08_bitis_ekrani");

            Time.timeScale = 1f;
        }

        private IEnumerator Send(InputField input, Button send, string message, bool capture)
        {
            yield return WaitUntil(() => send.interactable, 90f, "Gönder düğmesi açılmadı.");
            input.text = message;
            send.onClick.Invoke();
            yield return null;

            if (capture)
            {
                yield return new WaitForSecondsRealtime(0.5f);
                yield return Capture("05_dusunuyor");
            }

            yield return WaitUntil(() => send.interactable, 90f, "Modelden yanıt gelmedi.");
        }

        private IEnumerator Capture(string name)
        {
            if (Application.isBatchMode)
            {
                yield break;
            }

            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(screenshotFolder, name + ".png"));
            yield return new WaitForSecondsRealtime(0.3f);
        }

        private static void Teleport(CharacterController player, Vector3 position)
        {
            player.enabled = false;
            player.transform.position = position;
            player.enabled = true;
        }

        private static IEnumerator WaitUntil(System.Func<bool> condition, float timeout, string failure)
        {
            float deadline = Time.realtimeSinceStartup + timeout;

            while (!condition())
            {
                Assert.Less(Time.realtimeSinceStartup, deadline, failure);
                yield return null;
            }
        }
    }
}
