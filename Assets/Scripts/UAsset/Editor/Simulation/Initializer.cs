using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace UAsset.Editor
{
    public static class Initializer
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RuntimeInitializeOnLoad()
        {
            PathManager.PlatformName = Settings.GetPlatformName();
            var settings = Settings.GetDefaultSettings();
            var path = Settings.GetBuildPath(Versions.Filename);
            var versions = BuildVersions.Load(path);
            versions.offlineMode = settings.scriptPlayMode != ScriptPlayMode.Increment;
            File.WriteAllText(path, JsonUtility.ToJson(versions, true));
            switch (settings.scriptPlayMode)
            {
                case ScriptPlayMode.Simulation:
                    settings.Initialize();
                    Asset.Creator = EditorAsset.Create;
                    RawAsset.Creator = EditorRawAsset.Create;
                    Scene.Creator = EditorScene.Create;
                    Versions.Initializer = () => new EditorInitializeVersions();
                    break;
                case ScriptPlayMode.Preload:
                    PathManager.PlayerDataPath = Path.Combine(Environment.CurrentDirectory, Settings.PlatformBuildPath);
                    break;
                case ScriptPlayMode.Increment:
                    if (settings.requestCopy &&
                        EditorUtility.DisplayDialog("提示", "增量模式启动，是否复制资源到StreamingAssets", "复制"))
                        BuildScript.CopyToStreamingAssets(true);

                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }


        [InitializeOnLoadMethod]
        private static void InitializeOnLoad()
        {
            Settings.GetDefaultSettings().Initialize();
        }
    }
}