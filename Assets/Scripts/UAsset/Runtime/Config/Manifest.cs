using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace UAsset
{
    public class Manifest : ScriptableObject
    {
        /// <summary>
        ///     清单版本
        /// </summary>
        public int version;
        
        /// <summary>
        ///     资源路径列表
        /// </summary>
        public List<string> dirs = new List<string>();
        
        /// <summary>
        ///     所有资源信息列表
        /// </summary>
        public List<ManifestAsset> assets = new List<ManifestAsset>();
        
        /// <summary>
        ///     所有bundle信息列表
        /// </summary>
        public List<ManifestBundle> bundles = new List<ManifestBundle>();

        /// <summary>
        ///     资源路径对应的该路径下所有资源的id
        /// </summary>
        private Dictionary<string, List<int>> directoryWithAssets = new Dictionary<string, List<int>>();
        
        /// <summary>
        ///     资源路径对应的资源信息
        /// </summary>
        private Dictionary<string, ManifestAsset> nameWithAssets = new Dictionary<string, ManifestAsset>();
        
        public string nameWithAppendHash { get; internal set; }

        /// <summary>
        ///     资源路径是否存在对应的bundle
        /// </summary>
        public bool ContainAsset(string path)
        {
            return nameWithAssets.ContainsKey(path);
        }
        
        public bool TryGetAsset(string path, out ManifestAsset asset)
        {
            return nameWithAssets.TryGetValue(path, out asset);
        }

        /// <summary>
        ///     获取bundle
        /// </summary>
        /// <param name="assetPath">资源路径名</param>
        public ManifestBundle GetBundle(string assetPath)
        {
            return TryGetAsset(assetPath, out var value) ? bundles[value.bundle] : null;
        }

        /// <summary>
        ///     获取bundle
        /// </summary>
        /// <param name="id"></param>
        public ManifestBundle GetBundle(int id)
        {
            return bundles[id];
        }

        /// <summary>
        ///     获取bundle的所有依赖项
        /// </summary>
        public ManifestBundle[] GetDependencies(ManifestBundle bundle)
        {
            return bundle == null
                ? Array.Empty<ManifestBundle>()
                : Array.ConvertAll(bundle.deps, input =>
                {
                    var dep = bundles[input];
                    // 该bundle为变体，且非当前指定变体类型
                    if (dep.IsVariant && dep.variant != Versions.CurrentVariant)
                    {
                        var bundleName = dep.name.Replace($".{dep.variant}", 
                            $".{Versions.CurrentVariant}");
                        
                        var depBundle = GetBundle(bundleName);
                        if (depBundle != null)
                        {
                            return depBundle;
                        }
                        
                        Logger.E("Variant Bundle Not Exist: {0}, Variant: {1}", 
                            bundleName, Versions.CurrentVariant);
                    }
                    return dep;
                });
        }

        /// <summary>
        ///     获取资源依赖的所有资源路径
        /// </summary>
        public string[] GetDependencies(string assetPath)
        {
            return nameWithAssets.TryGetValue(assetPath, out var asset)
                ? Array.ConvertAll(asset.deps, input => assets[input].path)
                : Array.Empty<string>();
        }

        /// <summary>
        ///     拷贝清单文件
        /// </summary>
        public void Copy(Manifest manifest)
        {
            version = manifest.version;
            bundles = manifest.bundles;
            directoryWithAssets = manifest.directoryWithAssets;
            nameWithAppendHash = manifest.nameWithAppendHash;
            dirs = manifest.dirs;
            assets = manifest.assets;
            nameWithAssets = manifest.nameWithAssets;
        }

        /// <summary>
        ///     加载清单文件
        /// </summary>
        public static Manifest LoadFromFile(string path)
        {
            var manifest = Utility.LoadScriptableObjectWithJson<Manifest>(path);
            manifest.name = Path.GetFileNameWithoutExtension(path);
            manifest.OnLoad();
            return manifest;
        }

        public void Load(string path)
        {
            var json = File.ReadAllText(path);
            JsonUtility.FromJsonOverwrite(json, this);
            OnLoad();
        }

        /// <summary>
        ///     加载清单数据
        /// </summary>
        private void OnLoad()
        {
            nameWithAssets.Clear();

            // 创建目录
            foreach (var item in dirs)
            {
                var dir = item;
                if (!directoryWithAssets.TryGetValue(dir, out _))
                {
                    directoryWithAssets.Add(dir, new List<int>());
                }

                int pos;
                while ((pos = dir.LastIndexOf('/')) != -1)
                {
                    dir = dir.Substring(0, pos);
                    if (!directoryWithAssets.TryGetValue(dir, out _))
                    {
                        directoryWithAssets.Add(dir, new List<int>());
                    }
                }
            }
            
            foreach (var asset in assets)
            {
                var dir = dirs[asset.dir];
                var path = $"{dir}/{asset.name}";
                asset.path = path;
                nameWithAssets[path] = asset;
                if (directoryWithAssets.TryGetValue(dir, out var value))
                {
                    value.Add(asset.id);
                }
            }
        }

        /// <summary>
        ///     是否是资源目录
        /// </summary>
        public bool IsDirectory(string path)
        {
            return directoryWithAssets.ContainsKey(path);
        }

        /// <summary>
        ///     获取指定目录下的资源
        /// </summary>
        /// <param name="dir">目录</param>
        /// <param name="recursion">是否递归查找</param>
        /// <returns></returns>
        public string[] GetAssetsWithDirectory(string dir, bool recursion)
        {
            if (!recursion)
            {
                return directoryWithAssets.TryGetValue(dir, out var value)
                    ? value.ConvertAll(i => assets[i].path).ToArray()
                    : Array.Empty<string>();
            }

            var keys = new List<string>();
            foreach (var item in directoryWithAssets.Keys)
            {
                if (item.StartsWith(dir)
                    && (item.Length == dir.Length || item.Length > dir.Length && item[dir.Length] == '/'))
                {
                    keys.Add(item);
                }
            }

            if (keys.Count <= 0)
            {
                return Array.Empty<string>();
            }
            var get = new List<string>();
            foreach (var item in keys)
            {
                get.AddRange(GetAssetsWithDirectory(item, false));
            }
            return get.ToArray();
        }

        public void SaveAssetWithDirectory(string path)
        {
            var filename = Path.GetFileName(path);
            var asset = new ManifestAsset { name = filename, id = assets.Count, path = path };
            assets.Add(asset); 
            
            var dir = Path.GetDirectoryName(path)?.Replace('\\', '/');
            if (!directoryWithAssets.TryGetValue(dir, out var list))
            {
                list = new List<int>();
                directoryWithAssets.Add(dir, list);
                int pos;
                while ((pos = dir.LastIndexOf('/')) != -1)
                {
                    dir = dir.Substring(0, pos);
                    if (!directoryWithAssets.TryGetValue(dir, out _))
                    {
                        directoryWithAssets.Add(dir, new List<int>());
                    }
                }
            }
            list.Add(asset.id);
        }
    }
}