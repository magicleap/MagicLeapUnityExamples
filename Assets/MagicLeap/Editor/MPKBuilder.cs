using UnityEngine;
using UnityEditor;
using System.IO;

namespace MagicLeap
{
    public class MPKBuilder
    {
        private class BuildSettingsCache
        {
            private string appIdentifier;
            private string productName;
            private BuildTargetGroup seletedBuildTargetGroup;
            private bool signPackage;
            private bool isPackageDebuggable;

            public BuildSettingsCache()
            {
                appIdentifier = PlayerSettings.applicationIdentifier;
                productName = PlayerSettings.productName;
                seletedBuildTargetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
                signPackage = PlayerSettings.Lumin.signPackage;
                isPackageDebuggable = UnityEditor.Lumin.UserBuildSettings.isPackageDebuggable;
            }

            public void RestoreSettings()
            {
                PlayerSettings.applicationIdentifier = appIdentifier;
                PlayerSettings.productName = productName;
                EditorUserBuildSettings.selectedBuildTargetGroup = seletedBuildTargetGroup;
                PlayerSettings.Lumin.signPackage = signPackage;
                UnityEditor.Lumin.UserBuildSettings.isPackageDebuggable = isPackageDebuggable;
            }

            ~BuildSettingsCache()
            {
                RestoreSettings();
            }
        }

        public static void BuildMPK()
        {
            MPKBuilder mpkBuilder = new MPKBuilder();
            mpkBuilder.Build();
        }

        private void Build()
        {
            BuildSettingsCache buildSettingsCache = new BuildSettingsCache();
            string sceneName = string.Empty;

            try
            {
                EditorUserBuildSettings.selectedBuildTargetGroup = BuildTargetGroup.Lumin;
                PlayerSettings.Lumin.signPackage = true;

                BuildOptions buildOptions = BuildOptions.None;
                if (System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "--development") != -1)
                {
                    buildOptions |= BuildOptions.Development;
                }

                UnityEditor.Lumin.UserBuildSettings.isPackageDebuggable = true;

                string outDir = "Build";
                int outDirArgIndex = System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "--outdir");
                if (outDirArgIndex != -1)
                {
                    outDir = System.Environment.GetCommandLineArgs()[outDirArgIndex + 1];
                }

                System.IO.FileInfo buildFolder = new System.IO.FileInfo(System.IO.Path.Combine(outDir, BuildTarget.Lumin.ToString()));
                buildFolder.Directory.Create();

                BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
                buildPlayerOptions.target = BuildTarget.Lumin;
                buildPlayerOptions.targetGroup = BuildTargetGroup.Lumin;
                buildPlayerOptions.options = buildOptions;

                EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
                foreach (EditorBuildSettingsScene scene in scenes)
                {
                    if (!scene.enabled)
                    {
                        continue;
                    }

                    sceneName = System.IO.Path.GetFileNameWithoutExtension(scene.path);
                    PlayerSettings.applicationIdentifier = $"com.magicleap.unity.examples.{sceneName.ToLower()}";
                    PlayerSettings.productName = sceneName;
                    buildPlayerOptions.locationPathName = System.IO.Path.Combine(buildFolder.FullName, $"{PlayerSettings.applicationIdentifier}.mpk");
                    buildPlayerOptions.scenes = new string[] { scene.path };

                    if (sceneName == "MediaPlayer")
                    {
                        SetupMediaPlayerExample.AddMediaPlayerExampleData();
                    }
                    else if (sceneName == "MusicService")
                    {
                        string manifestPath = Application.dataPath + "/Plugins/Lumin/manifest.xml";

                        if (File.Exists(manifestPath) && !File.ReadAllText(manifestPath).Contains("ExampleMusicProvider"))
                        {
                            File.Delete(manifestPath);
                        }

                        SetupMusicService.BuildMusicPlayerExample();
                    }

                    UnityEditor.Build.Reporting.BuildReport report = UnityEditor.BuildPipeline.BuildPlayer(buildPlayerOptions);
                    if (report.summary.result == UnityEditor.Build.Reporting.BuildResult.Failed)
                    {
                        throw new System.Exception($"Building {sceneName} failed.");
                    }
                }
            }
            catch (System.Exception)
            {
                throw;
            }
            finally
            {
                if (sceneName == "MediaPlayer")
                {
                    SetupMediaPlayerExample.RemoveExampleSpecificData();
                }
                else if (sceneName == "MusicService")
                {
                    SetupMusicService.RemoveExampleSpecificData();
                }

                buildSettingsCache.RestoreSettings();
            }
        }
    }
}
