using System.IO;
using UnityEditor;
using UnityEngine;

namespace UAsset.Editor
{
    public static class EditorUtility
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