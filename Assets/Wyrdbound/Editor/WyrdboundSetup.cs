using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Wyrdbound.EditorTools
{
    /// <summary>
    /// Keeps the project playable with zero manual setup: ensures the bootstrap scene exists and is in
    /// the build, and provides menu / command-line entry points for building and resetting saves.
    /// </summary>
    [InitializeOnLoad]
    public static class WyrdboundSetup
    {
        const string ScenePath = "Assets/Scenes/Main.unity";

        static WyrdboundSetup()
        {
            EditorApplication.delayCall += () => Ensure(false);
        }

        [MenuItem("Wyrdbound/Ensure Project Setup")]
        public static void EnsureMenu() => Ensure(true);

        public static void Ensure(bool verbose)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            PlayerSettings.productName = "Wyrdbound";
            PlayerSettings.companyName = "Wyrdbound";
            PlayerSettings.defaultScreenWidth = 1920;
            PlayerSettings.defaultScreenHeight = 1080;
            PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;
            PlayerSettings.runInBackground = true;

            if (!File.Exists(ScenePath))
            {
                Directory.CreateDirectory("Assets/Scenes");
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                EditorSceneManager.SaveScene(scene, ScenePath);
                AssetDatabase.Refresh();
                if (verbose) Debug.Log("[Wyrdbound] Created " + ScenePath);
            }

            bool inBuild = false;
            foreach (var s in EditorBuildSettings.scenes) if (s.path == ScenePath) inBuild = true;
            if (!inBuild) EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };

            var active = EditorSceneManager.GetActiveScene();
            if (string.IsNullOrEmpty(active.path) && !active.isDirty && !Application.isBatchMode)
                EditorSceneManager.OpenScene(ScenePath);

            if (verbose) Debug.Log("[Wyrdbound] Project setup verified. Press Play.");
        }

        [MenuItem("Wyrdbound/Build Windows")]
        public static void BuildWindows()
        {
            Ensure(true);
            var opts = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = "Builds/Windows/Wyrdbound.exe",
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };
            var report = BuildPipeline.BuildPlayer(opts);
            Debug.Log("[Wyrdbound] Build " + report.summary.result + " — " + report.summary.totalSize / (1024 * 1024) + " MB, " + report.summary.totalErrors + " errors");
            if (Application.isBatchMode) EditorApplication.Exit(report.summary.result == BuildResult.Succeeded ? 0 : 1);
        }

        [MenuItem("Wyrdbound/Reset Save Data")]
        public static void ResetSave()
        {
            foreach (var f in new[] { "wyrdbound_save.json", "wyrdbound_settings.json" })
            {
                var p = Path.Combine(Application.persistentDataPath, f);
                if (File.Exists(p)) File.Delete(p);
            }
            Debug.Log("[Wyrdbound] Save data cleared at " + Application.persistentDataPath);
        }

        [MenuItem("Wyrdbound/Open Save Folder")]
        public static void OpenSaveFolder() => EditorUtility.RevealInFinder(Application.persistentDataPath);
    }
}
