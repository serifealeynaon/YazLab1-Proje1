using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using YazLab.Companion;
using YazLab.Interaction;

namespace YazLab.Tests
{
    public class CompanionReliabilityTests
    {
        private const float RunTimeout = 40f;
        private const int RepeatsPerCondition = 3;

        private static readonly Vector3[] StartPositions =
        {
            new Vector3(4f, 0f, -20f),
            new Vector3(-8f, 0f, -23f),
            new Vector3(8f, 0f, -22f),
            new Vector3(-7f, 0f, -8f),
            new Vector3(0f, 0f, -14f)
        };

        private static readonly (string Name, Vector3? Position)[] CrateConditions =
        {
            ("yok", null),
            ("orta", new Vector3(5.6f, 0.5f, -12f)),
            ("delik_oncesi", new Vector3(6.6f, 0.5f, -7.5f))
        };

        [UnityTest]
        [Timeout(900000)]
        public IEnumerator CompanionOpensDoorFromEveryStart()
        {
            Random.InitState(2026);
            var csv = new StringBuilder("start_x,start_z,crate,run,success,seconds,replans\n");
            var durations = new List<float>();
            int total = 0;
            int successes = 0;

            foreach (Vector3 start in StartPositions)
            {
                foreach ((string crateName, Vector3? cratePosition) in CrateConditions)
                {
                    for (int run = 1; run <= RepeatsPerCondition; run++)
                    {
                        yield return SceneManager.LoadSceneAsync("Level01");
                        yield return null;

                        var companion = Object.FindFirstObjectByType<CompanionController>();
                        var target = Object.FindFirstObjectByType<SwitchInteractable>();
                        var door = GameObject.Find("Door_1").GetComponent<Door>();
                        GameObject crate = GameObject.Find("Crate");
                        crate.SetActive(cratePosition.HasValue);

                        if (cratePosition.HasValue)
                        {
                            crate.transform.position = cratePosition.Value;
                        }

                        Vector2 offset = Random.insideUnitCircle;
                        Vector3 runStart = start + new Vector3(offset.x, 0f, offset.y);
                        Assert.IsTrue(companion.Agent.Warp(runStart), $"Başlangıç NavMesh üzerinde değil: {runStart}");
                        yield return null;

                        int replans = 0;
                        companion.StateChanged += state =>
                        {
                            if (state == "Replan")
                            {
                                replans++;
                            }
                        };

                        float startTime = Time.time;
                        companion.Command(target);

                        while (!door.IsOpen && companion.Task != null && Time.time - startTime < RunTimeout)
                        {
                            yield return null;
                        }

                        float seconds = Time.time - startTime;
                        bool success = door.IsOpen;
                        total++;

                        if (success)
                        {
                            successes++;
                            durations.Add(seconds);
                        }

                        csv.AppendLine($"{runStart.x:F1},{runStart.z:F1},{crateName},{run},{success},{seconds:F2},{replans}");
                    }
                }
            }

            float rate = 100f * successes / total;
            float average = durations.Count > 0 ? durations.Average() : 0f;
            csv.AppendLine($"# toplam={total} basari={successes} oran=%{rate:F1} ortalama_sn={average:F2}");
            Directory.CreateDirectory("TestResults");
            File.WriteAllText("TestResults/companion_results.csv", csv.ToString());
            Debug.Log($"[Test] Companion: {successes}/{total}, ortalama {average:F2} sn");
            Assert.AreEqual(total, successes);
        }

        [UnityTest]
        public IEnumerator PlayerCannotReachSwitchButDoorOpensPathToRoomB()
        {
            yield return SceneManager.LoadSceneAsync("Level01");
            yield return null;

            var companion = Object.FindFirstObjectByType<CompanionController>();
            var door = GameObject.Find("Door_1").GetComponent<Door>();
            GameObject lintel = GameObject.Find("Wall_Lintel");
            float lintelBottom = lintel.GetComponent<Collider>().bounds.min.y;
            float playerHeight = Object.FindFirstObjectByType<CharacterController>().height;
            Assert.Less(lintelBottom, playerHeight, "Oyuncu alçak delikten geçebiliyor.");
            Assert.Greater(lintelBottom, companion.Agent.height, "Companion alçak delikten geçemiyor.");

            door.Open();
            yield return new WaitForSeconds(2f);

            var path = new NavMeshPath();
            NavMesh.CalculatePath(companion.transform.position, new Vector3(0f, 0f, -2f), NavMesh.AllAreas, path);
            Assert.AreEqual(NavMeshPathStatus.PathComplete, path.status);
            Vector3 middleOfDoor = new Vector3(0f, 0f, -5f);
            Assert.IsTrue(NavMesh.SamplePosition(middleOfDoor, out NavMeshHit _, 0.3f, NavMesh.AllAreas),
                "Kapı açılınca NavMesh geçidi açılmadı.");
        }
    }
}
