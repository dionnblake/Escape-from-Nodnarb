using System;
using System.IO;
using UnityEditor;
using UnityEditor.Android;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace EscapeFromNodnarb.Editor
{
    public static class AndroidBuilder
    {
        public const string OutputPath = "Builds/Android/EscapeFromNodnarb-debug.apk";

        [MenuItem("Escape from Nodnarb/Build Android Debug APK")]
        public static void BuildDebug()
        {
            ConfigureExternalTools();
            ProjectBootstrapper.Configure();
            Directory.CreateDirectory(Path.GetDirectoryName(OutputPath) ?? "Builds/Android");
            if (!EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android))
            {
                throw new InvalidOperationException("Could not switch the active build target to Android.");
            }

            EditorUserBuildSettings.development = true;
            EditorUserBuildSettings.allowDebugging = true;
            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = new[] { ProjectBootstrapper.MainScenePath },
                locationPathName = OutputPath,
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                options = BuildOptions.Development | BuildOptions.AllowDebugging | BuildOptions.CompressWithLz4HC
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;
            string outputPath = Path.GetFullPath(summary.outputPath);
            long outputBytes = File.Exists(outputPath) ? new FileInfo(outputPath).Length : 0L;
            string evidence = "NODNARB_ANDROID_BUILD result=" + summary.result
                + " errors=" + summary.totalErrors
                + " warnings=" + summary.totalWarnings
                + " apkBytes=" + outputBytes
                + " output=" + outputPath;
            Debug.Log(evidence);
            if (summary.result != BuildResult.Succeeded)
            {
                throw new InvalidOperationException(evidence);
            }
        }

        private static void ConfigureExternalTools()
        {
            string sdkPath = ResolveEnvironmentPath("UNITY_ANDROID_SDK_ROOT", "ANDROID_SDK_ROOT");
            if (!string.IsNullOrWhiteSpace(sdkPath))
            {
                AndroidExternalToolsSettings.sdkRootPath = ValidateExternalToolPath("Android SDK", sdkPath);
            }

            string ndkPath = ResolveEnvironmentPath("UNITY_ANDROID_NDK_ROOT", "ANDROID_NDK_ROOT");
            if (!string.IsNullOrWhiteSpace(ndkPath))
            {
                AndroidExternalToolsSettings.ndkRootPath = ValidateExternalToolPath("Android NDK", ndkPath);
            }

            string jdkPath = ResolveEnvironmentPath("UNITY_JAVA_HOME", "JAVA_HOME");
            if (!string.IsNullOrWhiteSpace(jdkPath))
            {
                AndroidExternalToolsSettings.jdkRootPath = ValidateExternalToolPath("JDK", jdkPath);
            }
        }

        private static string ResolveEnvironmentPath(string preferredVariable, string fallbackVariable)
        {
            string path = Environment.GetEnvironmentVariable(preferredVariable);
            return string.IsNullOrWhiteSpace(path)
                ? Environment.GetEnvironmentVariable(fallbackVariable)
                : path;
        }

        private static string ValidateExternalToolPath(string label, string path)
        {
            string fullPath = Path.GetFullPath(path);
            if (!Directory.Exists(fullPath))
            {
                throw new DirectoryNotFoundException(label + " directory does not exist: " + fullPath);
            }

            Debug.Log("NODNARB_ANDROID_TOOL " + label + "=" + fullPath);
            return fullPath;
        }
    }
}
