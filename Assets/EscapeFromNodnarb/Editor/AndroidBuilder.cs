using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace EscapeFromNodnarb.Editor
{
    public static class AndroidBuilder
    {
        public const string OutputPath = "Builds/Android/EscapeFromNodnarb-debug.apk";
        public const string ReleaseApkOutputPath = "Builds/Android/EscapeFromNodnarb-release-local.apk";
        public const string ReleaseBundleOutputPath = "Builds/Android/EscapeFromNodnarb-release-local.aab";

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

        [MenuItem("Escape from Nodnarb/Build Android Local Release Artifacts")]
        public static void BuildReleaseArtifacts()
        {
            bool previousDevelopment = EditorUserBuildSettings.development;
            bool previousAllowDebugging = EditorUserBuildSettings.allowDebugging;
            bool previousBuildAppBundle = EditorUserBuildSettings.buildAppBundle;
            try
            {
                ConfigureExternalTools();
                ProjectBootstrapper.Configure();
                Directory.CreateDirectory(Path.GetDirectoryName(ReleaseApkOutputPath) ?? "Builds/Android");
                if (!EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android))
                {
                    throw new InvalidOperationException("Could not switch the active build target to Android.");
                }

                EditorUserBuildSettings.development = false;
                EditorUserBuildSettings.allowDebugging = false;
                BuildReleaseArtifact(ReleaseApkOutputPath, false);
                BuildReleaseArtifact(ReleaseBundleOutputPath, true);
            }
            finally
            {
                EditorUserBuildSettings.development = previousDevelopment;
                EditorUserBuildSettings.allowDebugging = previousAllowDebugging;
                EditorUserBuildSettings.buildAppBundle = previousBuildAppBundle;
            }
        }

        private static void BuildReleaseArtifact(string outputPath, bool appBundle)
        {
            EditorUserBuildSettings.buildAppBundle = appBundle;
            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = new[] { ProjectBootstrapper.MainScenePath },
                locationPathName = outputPath,
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                options = BuildOptions.CompressWithLz4HC
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;
            string builtPath = Path.GetFullPath(summary.outputPath);
            long outputBytes = File.Exists(builtPath) ? new FileInfo(builtPath).Length : 0L;
            string artifact = appBundle ? "aab" : "apk";
            string evidence = "NODNARB_ANDROID_RELEASE_BUILD artifact=" + artifact
                + " result=" + summary.result
                + " errors=" + summary.totalErrors
                + " warnings=" + summary.totalWarnings
                + " bytes=" + outputBytes
                + " output=" + builtPath
                + " development=" + EditorUserBuildSettings.development
                + " allowDebugging=" + EditorUserBuildSettings.allowDebugging;
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
                TrySetExternalToolPath("sdkRootPath", ValidateExternalToolPath("Android SDK", sdkPath));
            }

            string ndkPath = ResolveEnvironmentPath("UNITY_ANDROID_NDK_ROOT", "ANDROID_NDK_ROOT");
            if (!string.IsNullOrWhiteSpace(ndkPath))
            {
                TrySetExternalToolPath("ndkRootPath", ValidateExternalToolPath("Android NDK", ndkPath));
            }

            string jdkPath = ResolveEnvironmentPath("UNITY_JAVA_HOME", "JAVA_HOME");
            if (!string.IsNullOrWhiteSpace(jdkPath))
            {
                TrySetExternalToolPath("jdkRootPath", ValidateExternalToolPath("JDK", jdkPath));
            }
        }

        private static void TrySetExternalToolPath(string memberName, string value)
        {
            Type settingsType = FindExternalToolsSettingsType();
            if (settingsType == null)
            {
                Debug.LogWarning("NODNARB_ANDROID_TOOL API unavailable; using Unity's configured external tools.");
                return;
            }

            PropertyInfo property = settingsType.GetProperty(memberName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (property != null && property.CanWrite)
            {
                property.SetValue(null, value, null);
                return;
            }

            FieldInfo field = settingsType.GetField(memberName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (field != null)
            {
                field.SetValue(null, value);
            }
        }

        private static Type FindExternalToolsSettingsType()
        {
            const string fullName = "UnityEditor.Android.AndroidExternalToolsSettings";
            Type type = Type.GetType(fullName + ", UnityEditor.Android.Extensions");
            if (type != null)
            {
                return type;
            }

            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                type = assembly.GetType(fullName, false);
                if (type != null)
                {
                    return type;
                }
            }

            return null;
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
