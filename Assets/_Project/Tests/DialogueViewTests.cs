using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using YazLab.Game;
using YazLab.UI;

namespace YazLab.Tests
{
    public class DialogueViewTests
    {
        [UnityTest]
        public IEnumerator NewestLineIsNotCutInLongConversation()
        {
            yield return SceneManager.LoadSceneAsync("Level01");
            yield return null;

            Object.FindFirstObjectByType<NpcDialoguePresenter>().Open();
            yield return null;

            var view = Object.FindFirstObjectByType<DialogueView>();
            var log = GameObject.Find("Log").GetComponent<Text>();

            for (int i = 0; i < 12; i++)
            {
                view.AddPlayerLine($"mesaj {i}");
            }

            view.AddNpcLine("EN YENI SATIR");
            Canvas.ForceUpdateCanvases();
            yield return null;

            TextGenerator generator = log.cachedTextGenerator;
            float neededHeight = generator.lineCount * log.fontSize;
            Debug.Log($"[Test] Sohbet satırı {generator.lineCount}, görünen karakter {generator.characterCountVisible}/{log.text.Length}");

            Assert.Greater(neededHeight, log.rectTransform.rect.height, "Metin kutuyu aşmıyor, test geçersiz.");
            Assert.GreaterOrEqual(generator.characterCountVisible, log.text.Length - 1, "En yeni satır kesildi.");
            Assert.IsNotNull(log.GetComponent<RectMask2D>());
        }

        [UnityTest]
        public IEnumerator OldLinesAreDroppedAfterLimit()
        {
            yield return SceneManager.LoadSceneAsync("Level01");
            yield return null;

            Object.FindFirstObjectByType<NpcDialoguePresenter>().Open();
            yield return null;

            var view = Object.FindFirstObjectByType<DialogueView>();
            var log = GameObject.Find("Log").GetComponent<Text>();

            for (int i = 0; i < 40; i++)
            {
                view.AddPlayerLine($"mesaj-{i}-");
            }

            StringAssert.DoesNotContain("mesaj-0-", log.text);
            StringAssert.Contains("mesaj-39-", log.text);
        }
    }
}
