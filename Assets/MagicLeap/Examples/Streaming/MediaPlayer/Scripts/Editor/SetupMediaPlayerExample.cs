// %BANNER_BEGIN%
// ---------------------------------------------------------------------
// %COPYRIGHT_BEGIN%
//
// Copyright (c) 2019-present, Magic Leap, Inc. All Rights Reserved.
// Use of this file is governed by the Developer Agreement, located
// here: https://auth.magicleap.com/terms/developer
//
// %COPYRIGHT_END%
// ---------------------------------------------------------------------
// %BANNER_END%

using UnityEngine;
using UnityEditor;
using UnityEditor.Lumin;
using System;
using System.IO;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace MagicLeap
{
    /// <summary>
    /// Helper script to copy the example stereo videos to the project's streaming assets
    /// </summary>
    public class SetupMediaPlayerExample
    {
        private static SetupMediaPlayerExample _instance;
        private string _stereoVideoExampleAssetPath;

        [MenuItem("Magic Leap/Examples/Streaming/Setup Media Player Example")]
        public static void AddMediaPlayerExampleData()
        {
            if (_instance == null)
            {
                _instance = new SetupMediaPlayerExample();
            }

            _instance._stereoVideoExampleAssetPath = Path.Combine(Application.dataPath, Path.Combine("MagicLeap","Examples", "Streaming", Path.Combine("MediaPlayer", "StreamingAssets")));

            EditorUtility.DisplayProgressBar("Setting up Media Player Example", "Copying example video streaming assets", 0.25f);
            if (!_instance.CopyMediaPlayerExampleStreamingAssets())
            {
                UnityEngine.Debug.Log("Failed to copy example video streaming assets.");
                EditorUtility.ClearProgressBar();
                return;
            }

            EditorUtility.DisplayProgressBar("Setting up Media Player Example", "Refreshing Asset Database", 1.0f);

            AssetDatabase.Refresh();

            UnityEngine.Debug.Log("Successfully setup project for Media Player Example.");
            EditorUtility.ClearProgressBar();
        }

        private bool CopyMediaPlayerExampleStreamingAssets()
        {
            try
            {
                string streamingAssetsPath = Path.Combine(Application.dataPath, Path.Combine("StreamingAssets", "MediaPlayerExample"));
                DirectoryInfo info = new DirectoryInfo(streamingAssetsPath);
                if (info.Exists && info.GetFileSystemInfos().Length != 0)
                {
                    return true;
                }

                Directory.CreateDirectory(Path.Combine(Application.dataPath, "StreamingAssets"));
                Directory.CreateDirectory(streamingAssetsPath);

                string fileName;
                foreach (string file in Directory.GetFiles(_stereoVideoExampleAssetPath))
                {
                    if (file.ToLower().EndsWith(".meta"))
                    {
                        continue;
                    }
                    fileName = Path.GetFileName(file);
                    File.Copy(Path.Combine(_stereoVideoExampleAssetPath, fileName), Path.Combine(streamingAssetsPath, fileName), true);
                }
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogFormat("Exception copying example video streaming assets: {0}", e);
                return false;
            }

            return true;
        }

        [MenuItem("Magic Leap/Examples/Streaming/Clean Media Player Example")]
        public static void RemoveExampleSpecificData()
        {
            if (_instance == null)
            {
                _instance = new SetupMediaPlayerExample();
            }

            EditorUtility.DisplayProgressBar("Cleaning Example Project", "Deleting example video streaming assets", 0.25f);
            if (!_instance.RemoveVideoExampleStreamingAssets())
            {
                UnityEngine.Debug.Log("Failed to delete example video streaming assets.");
                EditorUtility.ClearProgressBar();
                return;
            }

            EditorUtility.DisplayProgressBar("Cleaning Example Project", "Refreshing Asset Database", 1.0f);

            AssetDatabase.Refresh();

            UnityEngine.Debug.Log("Successfully cleaned example video streaming data from project.");
            EditorUtility.ClearProgressBar();
        }

        private bool RemoveVideoExampleStreamingAssets()
        {
            try
            {
                string streamingAssetsPath = Path.Combine(Application.dataPath, Path.Combine("StreamingAssets", "MediaPlayerExample"));
                DirectoryInfo dirInfo = new DirectoryInfo(streamingAssetsPath);
                if (dirInfo.Exists)
                {
                    Directory.Delete(streamingAssetsPath, true);
                }

                string streamingAssetsPathMeta = Path.Combine(Application.dataPath, Path.Combine("StreamingAssets", "MediaPlayerExample.meta"));
                FileInfo fileInfo = new FileInfo(streamingAssetsPathMeta);
                if (fileInfo.Exists)
                {
                    File.Delete(streamingAssetsPathMeta);
                }
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogFormat("Exception deleting example video streaming assets: {0}", e);
                return false;
            }

            return true;
        }
    }
}
