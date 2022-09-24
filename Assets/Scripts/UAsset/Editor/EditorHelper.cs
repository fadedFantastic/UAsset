using System.IO;
using UnityEditor;
using UnityEngine;

namespace UAsset.Editor
{
    public static class EditorHelper
    {
        public static T FindOrCreateAsset<T>(string path) where T : ScriptableObject
        {
            var guilds = AssetDatabase.FindAssets($"t:{typeof(T).FullName}");
            foreach (var guild in guilds)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guild);
                if (string.IsNullOrEmpty(assetPath)) continue;

                var asset = GetOrCreateAsset<T>(assetPath);
                if (asset == null) continue;

                return asset;
            }

            return GetOrCreateAsset<T>(path);
        }

        private static T GetOrCreateAsset<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;

            Utility.CreateFileDirectory(path);
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }
        
        /// <summary>
        /// 加载相关的配置文件
        /// </summary>
        public static TSetting LoadSettingData<TSetting>() where TSetting : ScriptableObject
        {
            var settingType = typeof(TSetting);
            var guids = AssetDatabase.FindAssets($"t:{settingType.Name}");
            if (guids.Length == 0)
            {
                Debug.LogWarning($"Create new {settingType.Name}.asset");
                var setting = ScriptableObject.CreateInstance<TSetting>();
                string filePath = $"Assets/{settingType.Name}.asset";
                AssetDatabase.CreateAsset(setting, filePath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                return setting;
            }
            else
            {
                if (guids.Length != 1)
                {
                    foreach (var guid in guids)
                    {
                        string path = AssetDatabase.GUIDToAssetPath(guid);
                        Debug.LogWarning($"Found multiple file : {path}");
                    }
                    throw new System.Exception($"Found multiple {settingType.Name} files !");
                }

                string filePath = AssetDatabase.GUIDToAssetPath(guids[0]);
                var setting = AssetDatabase.LoadAssetAtPath<TSetting>(filePath);
                return setting;
            }
        }
        
        /// <summary>
        /// 拷贝文件到版本目录
        /// </summary>
        /// <param name="filename">文件名</param>
        /// <param name="destinationDir">目标路径</param>
        public static void Copy(string filename, string destinationDir)
        {
            var from = Settings.GetBuildPath(filename);
            if (File.Exists(from))
            {
                var dest = $"{destinationDir}/{filename}";
                Utility.CreateFileDirectory(dest);
                File.Copy(from, dest, true);
            }
            else
            {
                Debug.LogErrorFormat("File not found: {0}", from);
            }
        }
    }
}