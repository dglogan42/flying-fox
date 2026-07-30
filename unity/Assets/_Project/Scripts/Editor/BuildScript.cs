#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace FlyingFox.EditorTools
{
    /// <summary>
    /// CLI / menu build entry points for CI and local packaging.
    ///
    /// Examples:
    ///   Unity -batchmode -quit -projectPath . -executeMethod FlyingFox.EditorTools.BuildScript.BuildWindows64
    ///   Unity -batchmode -quit -projectPath . -executeMethod FlyingFox.EditorTools.BuildScript.BuildLinux64
    ///   Unity -batchmode -quit -projectPath . -executeMethod FlyingFox.EditorTools.BuildScript.BuildWebGL
    /// </summary>
    public static class BuildScript
    {
        const string GameScene = "Assets/_Project/Scenes/Game.unity";
        const string DefaultOutputRoot = "Builds";

        [MenuItem("Flying Fox/Build/Windows x64")]
        public static void BuildWindows64() =>
            Build(BuildTarget.StandaloneWindows64, "Windows", "FlyingFox.exe");

        [MenuItem("Flying Fox/Build/Linux x64")]
        public static void BuildLinux64() =>
            Build(BuildTarget.StandaloneLinux64, "Linux", "FlyingFox.x86_64");

        [MenuItem("Flying Fox/Build/WebGL")]
        public static void BuildWebGL() =>
            Build(BuildTarget.WebGL, "WebGL", null);

        [MenuItem("Flying Fox/Build/All desktop")]
        public static void BuildAllDesktop()
        {
            BuildWindows64();
            BuildLinux64();
        }

        [MenuItem("Flying Fox/Build/Nintendo Switch (requires SDK)")]
        public static void BuildNintendoSwitch()
        {
#if UNITY_SWITCH
            // Nintendo Switch module present — build NSO/NSP pipeline per Nintendo docs.
            // Output folder is conventionally managed by the Switch build tools.
            Build(BuildTarget.Switch, "Switch", "FlyingFox");
#else
            Debug.LogError(
                "[FlyingFox.Build] Nintendo Switch module not installed.\n" +
                "1) Register at https://developer.nintendo.com/\n" +
                "2) Install NintendoSDK + Unity Switch support\n" +
                "3) Open this project with a Switch-capable Unity Editor\n" +
                "See docs/SWITCH_ESHP.md");
            if (Application.isBatchMode)
                EditorApplication.Exit(2);
#endif
        }

        public static void Build(
            BuildTarget target,
            string folderName,
            string executableName)
        {
            string outputRoot = GetArg("-outputPath") ?? DefaultOutputRoot;
            string outDir = Path.Combine(outputRoot, folderName);
            Directory.CreateDirectory(outDir);

            string location;
            if (target == BuildTarget.WebGL)
                location = outDir;
            else
                location = Path.Combine(outDir, executableName ?? "FlyingFox");

            var scenes = EditorBuildSettings.scenes
                .Where(s => s.enabled)
                .Select(s => s.path)
                .ToArray();

            if (scenes.Length == 0)
            {
                if (!File.Exists(GameScene))
                    throw new FileNotFoundException("No scenes in build settings and Game.unity missing.", GameScene);
                scenes = new[] { GameScene };
                Debug.LogWarning("[FlyingFox.Build] Build settings empty — using Game.unity only.");
            }

            var opts = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = location,
                target = target,
                options = BuildOptions.CompressWithLz4HC,
            };

            // Scripting defines from env / CLI
            string extraDefines = GetArg("-defines") ?? Environment.GetEnvironmentVariable("FF_DEFINES");
            string[] defines = string.IsNullOrWhiteSpace(extraDefines)
                ? Array.Empty<string>()
                : extraDefines.Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries);

            var group = BuildPipeline.GetBuildTargetGroup(target);
            string prior = PlayerSettings.GetScriptingDefineSymbolsForGroup(group);
            try
            {
                if (defines.Length > 0)
                {
                    var merged = prior.Split(';')
                        .Where(d => !string.IsNullOrWhiteSpace(d))
                        .Concat(defines)
                        .Distinct()
                        .ToArray();
                    PlayerSettings.SetScriptingDefineSymbolsForGroup(group, string.Join(";", merged));
                    Debug.Log("[FlyingFox.Build] Defines: " + string.Join(";", merged));
                }

                Debug.Log($"[FlyingFox.Build] Building {target} → {location}");
                var report = BuildPipeline.BuildPlayer(opts);
                var summary = report.summary;

                if (summary.result != BuildResult.Succeeded)
                {
                    Debug.LogError($"[FlyingFox.Build] FAILED: {summary.result} errors={summary.totalErrors}");
                    if (Application.isBatchMode)
                        EditorApplication.Exit(1);
                    throw new Exception($"Build failed: {summary.result}");
                }

                Debug.Log(
                    $"[FlyingFox.Build] OK {target} size={summary.totalSize} bytes " +
                    $"time={summary.totalTime} → {location}");

                WriteBuildInfo(outDir, target, summary);
            }
            finally
            {
                PlayerSettings.SetScriptingDefineSymbolsForGroup(group, prior);
            }

            if (Application.isBatchMode)
                EditorApplication.Exit(0);
        }

        static void WriteBuildInfo(string outDir, BuildTarget target, BuildSummary summary)
        {
            string path = Path.Combine(outDir, "build-info.txt");
            File.WriteAllText(path,
                $"product=FlyingFox\n" +
                $"target={target}\n" +
                $"result={summary.result}\n" +
                $"size={summary.totalSize}\n" +
                $"unity={Application.unityVersion}\n" +
                $"utc={DateTime.UtcNow:O}\n");
        }

        static string GetArg(string name)
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == name)
                    return args[i + 1];
            }
            return null;
        }
    }
}
#endif
