using System;
using UnityEditor;
using UnityEditor.Build.Reporting;

namespace YazLab.EditorTools
{
    public static class BuildScript
    {
        private const string ScenePath = "Assets/_Project/Scenes/Level01.unity";

        [MenuItem("YazLab/Build Mac")]
        public static void BuildMac()
        {
            Build(BuildTarget.StandaloneOSX, "Builds/Mac/YazLabOyun.app");
        }

        [MenuItem("YazLab/Build Windows")]
        public static void BuildWindows()
        {
            Build(BuildTarget.StandaloneWindows64, "Builds/Windows/YazLabOyun.exe");
        }

        private static void Build(BuildTarget target, string path)
        {
            PlayerSettings.productName = "YazLab Oyun";
            PlayerSettings.fullScreenMode = UnityEngine.FullScreenMode.Windowed;
            PlayerSettings.defaultScreenWidth = 1280;
            PlayerSettings.defaultScreenHeight = 720;
            PlayerSettings.runInBackground = true;
            PlayerSettings.insecureHttpOption = InsecureHttpOption.AlwaysAllowed;

            BuildReport report = BuildPipeline.BuildPlayer(new[] { ScenePath }, path, target, BuildOptions.None);

            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new Exception($"Build başarısız: {report.summary.result}, {report.summary.totalErrors} hata");
            }
        }
    }
}
