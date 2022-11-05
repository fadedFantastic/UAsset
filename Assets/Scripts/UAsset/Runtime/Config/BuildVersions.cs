using System.Collections.Generic;
using UnityEngine;

namespace UAsset
{
    public class BuildVersions : ScriptableObject
    {
        /// <summary>
        ///     时间戳
        /// </summary>
        public long timestamp;
        
        /// <summary>
        ///     是否为离线模式
        /// </summary>
        public bool offlineMode;
        
        /// <summary>
        ///     游戏版本号
        /// </summary>
        public string gameVersion;
        
        /// <summary>
        ///     资源版本号
        /// </summary>
        public int internalResourceVersion;

        /// <summary>
        ///     是否开启加密        
        /// </summary>
        public bool encryptionEnabled;

        /// <summary>
        ///     包体默认变体类型
        /// </summary>
        public string builtinVariant;

        /// <summary>
        ///     语言列表
        /// </summary>
        public string[] variantTypes;

        /// <summary>
        ///     变体版本号
        /// </summary>
        public List<int> variantVersion;

        /// <summary>
        ///     资源更新下载地址 (打包后由upload2server.sh脚本写入)
        /// </summary>
        public string updatePrefixUri;

        public List<string> streamingAssets = new List<string>();

        public List<BuildVersion> data = new List<BuildVersion>();

        private readonly Dictionary<string, BuildVersion> nameWithVersion = new Dictionary<string, BuildVersion>();

        public static BuildVersions Load(string path)
        {
            var asset = Utility.LoadScriptableObjectWithJson<BuildVersions>(path);
            asset.Load();
            return asset;
        }

        private void Load()
        {
            nameWithVersion.Clear();
            foreach (var version in data)
            {
                nameWithVersion[version.name] = version;
            }
        }

        public void Set(string build, string file, long size, long time, string hash, string version, int resVersion)
        {
            if (!nameWithVersion.TryGetValue(build, out var value))
            {
                value = new BuildVersion {name = build, file = file, size = size, hash = hash};
                nameWithVersion.Add(build, value);
                data.Add(value);
            }
            else
            {
                value.file = file;
                value.size = size;
                value.hash = hash;
            }

            timestamp = time;
            gameVersion = version;
            internalResourceVersion = resVersion;
        }
        
        public BuildVersion Get(string build)
        {
            return nameWithVersion.TryGetValue(build, out var value) ? value : null;
        }
    }
}