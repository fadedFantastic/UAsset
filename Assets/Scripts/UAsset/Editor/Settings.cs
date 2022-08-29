using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace UAsset.Editor
{
    public sealed class Settings : ScriptableObject
    {
        private const string kSettingPath = "Assets/Scripts/Settings.asset";
        public static string BundleExtension { get; set; } = ".bundle";

        public bool requestCopy;

        /// <summary>
        ///     采集资源或依赖需要过滤掉的文件
        /// </summary>
        [Header("Bundle")] [Tooltip("采集资源或依赖需要过滤掉的文件")]
        public List<string> excludeFiles = new List<string> 
        {
            ".spriteatlas",
            ".giparams",
            "LightingData.asset"
        }; 

        /// <summary>
        ///     播放器的运行模式。Preload 模式不更新资源，并且打包的时候会忽略分包配置。
        /// </summary>
        [Tooltip("播放器的运行模式")] 
        public ScriptPlayMode scriptPlayMode = ScriptPlayMode.Simulation;
        
        [Header("Encryption")][Tooltip("是否开启加密模式")] 
        public bool encryptionEnabled = true; 
        
        [Tooltip("加密需要过滤的文件")]
        public List<string> encryptionExcludeFiles = new List<string> 
        {
            ".mp4",
            ".bnk",
        };

        public static List<string> ExcludeFiles { get; private set; }
        public static bool EncryptionEnabled { get; private set; }
        public static List<string> EncryptionExcludeFiles { get; private set; }
        

        /// <summary>
        ///     固定的打包输出目录(为了增量打包)
        /// </summary>
        public static string PlatformBuildPath
        {
            get
            {
                var dir = $"{Utility.BuildPath}/{GetPlatformName()}";
                Utility.CreateDirectory(dir);
                return dir;
            }
        }

        /// <summary>
        ///     安装包资源目录, 打包安装包的时候会自动根据分包配置将资源拷贝到这个目录
        /// </summary>
        public static string BuildPlayerDataPath => Application.streamingAssetsPath;

        public void Initialize()
        {
            ExcludeFiles = excludeFiles;
            EncryptionEnabled = encryptionEnabled;
            EncryptionExcludeFiles = encryptionExcludeFiles;
        }

        public static Settings GetDefaultSettings()
        {
            return EditorUtility.FindOrCreateAsset<Settings>(kSettingPath);
        }

        /// <summary>
        ///     获取包含在安装包的资源
        /// </summary>
        /// <returns></returns>
        public List<ManifestBundle> GetBundlesInBuild(BuildVersions versions)
        {
            var bundles = new List<ManifestBundle>();
            foreach (var version in versions.data)
            {
                var manifest = Manifest.LoadFromFile(GetBuildPath(version.file));
                bundles.AddRange(manifest.bundles);
            }

            return bundles;
        }

        /// <summary>
        /// 获取bundle文件路径（增量打包目录）
        /// </summary>
        /// <param name="file">文件名</param>
        /// <returns></returns>
        public static string GetBuildPath(string file)
        {
            return $"{PlatformBuildPath}/{file}";
        }
        
        /// <summary>
        /// 获取bundle包带版本号路径
        /// </summary>
        /// <param name="versions">当前版本文件</param>
        /// <returns></returns>
        public static string GetBuildVersionDir(BuildVersions versions)
        {
            var versionDirName = $"{versions.gameVersion.Replace('.', '_')}_{versions.internalResourceVersion}";
            return $"{Utility.BuildPath}/{versionDirName}/{GetPlatformName()}";
        }

        public static string GetPlatformName()
        {
            switch (EditorUserBuildSettings.activeBuildTarget)
            {
                case BuildTarget.Android:
                    return "Android";
                case BuildTarget.StandaloneOSX:
                    return "OSX";
                case BuildTarget.StandaloneWindows:
                case BuildTarget.StandaloneWindows64:
                    return "Windows";
                case BuildTarget.iOS:
                    return "iOS";
                case BuildTarget.WebGL:
                    return "WebGL";
                default:
                    return Utility.nonsupport;
            }
        }

        public static bool IsExcluded(string path)
        {
            return ExcludeFiles.Exists(path.EndsWith) || path.EndsWith(".cs") || path.EndsWith(".dll");
        }

        public static bool IsEncryptionExcluded(string path)
        {
            return EncryptionExcludeFiles.Exists(path.EndsWith);
        }

        public static IEnumerable<string> GetDependencies(string path)
        {
            var set = new HashSet<string>(AssetDatabase.GetDependencies(path, true));
            set.Remove(path);
            set.RemoveWhere(IsExcluded);
            return set.ToArray();
        }
    }
}