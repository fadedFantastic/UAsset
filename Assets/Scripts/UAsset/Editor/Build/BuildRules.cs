using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace UAsset.Editor
{
    /// <summary>
    /// 资源加载方式类型。
    /// </summary>
    public enum LoadType : byte
    {
        /// <summary>
        /// 使用文件方式加载。
        /// </summary>
        LoadFromFile = 0,

        /// <summary>
        /// 使用内存方式加载。
        /// </summary>
        LoadFromMemory,

        /// <summary>
        /// 使用内存快速解密方式加载。
        /// </summary>
        LoadFromMemoryAndQuickDecrypt,

        /// <summary>
        /// 使用内存解密方式加载。
        /// </summary>
        LoadFromMemoryAndDecrypt,

        /// <summary>
        /// 使用二进制方式加载。
        /// </summary>
        LoadFromBinary,

        /// <summary>
        /// 使用二进制快速解密方式加载。
        /// </summary>
        LoadFromBinaryAndQuickDecrypt,

        /// <summary>
        /// 使用二进制解密方式加载。
        /// </summary>
        LoadFromBinaryAndDecrypt
    }
    
    public enum NameBy
    {
        Explicit, // 显示指定bundle名
        Path,     // 文件路径为bundle名（每个文件打独立bundle）
        Directory, // 最后一层目录为bundle名（文件夹下所有的文件打成一个bundle）
        TopDirectory // 规则路径的顶层目录为bundle名（文件夹下所有的文件打成一个bundle）
    }

    [Serializable]
    public class RuleAsset
    {
        public string assetName;
        public string bundle;
        public string guid;
        public string resourceVariant;
    }

    [Serializable]
    public class RuleBundle
    {
        public string name;
        public LoadType loadType = LoadType.LoadFromFile;
        public bool packed = true;
        public string[] assets;
        public string variant;
        public string fileSystem; //todo
        public string tag;

        public bool IsLoadFromBinary => loadType == LoadType.LoadFromBinary || 
                                        loadType == LoadType.LoadFromBinaryAndQuickDecrypt || 
                                        loadType == LoadType.LoadFromBinaryAndDecrypt;
    }

    [Serializable]
    public class BuildRule
    {
        public bool valid = true;

        [NonSerialized] public string prefixPath = "";

        [Tooltip("搜索路径")] public string searchPath;

        public string GetFullSearchPath()
        {
            return Path.Combine(prefixPath, searchPath);
        }

        [Tooltip("搜索通配符，多个之间请用,(逗号)隔开")] public string searchPattern = "*.prefab";
        
        [Tooltip("资源标签(非重要资源，打了标签热更时会忽略)")] public string tag = string.Empty;

        [Tooltip("命名规则")] public NameBy nameBy = NameBy.Path;

        [Tooltip("Explicit的名称")] public string assetBundleName;

        public LoadType loadType;

        public bool packed;

        public string[] GetAssets()
        {
            if (!valid) return new string[0];

            var _searchPath = GetFullSearchPath();
            var patterns = searchPattern.Split(new[] {','}, StringSplitOptions.RemoveEmptyEntries);
            if (!Directory.Exists(_searchPath))
            {
                Debug.LogWarning("Rule searchPath not exist:" + _searchPath);
                return new string[0];
            }

            var getFiles = new List<string>();
            foreach (var item in patterns)
            {
                var files = Directory.GetFiles(_searchPath, item, SearchOption.AllDirectories);
                foreach (var file in files)
                {
                    if (Directory.Exists(file)) continue;
                    var ext = Path.GetExtension(file).ToLower();
                    if ((ext == ".fbx" || ext == ".anim") && !item.Contains(ext)) continue;
                    if (!BuildRules.ValidateAsset(file)) continue;
                    var asset =  PathManager.GetRegularPath(file);
                    getFiles.Add(asset);
                }
            }

            return getFiles.ToArray();
        }
    }

    public class BuildRules : ScriptableObject
    {
        public static string ruleConfigPath = "Assets/Scripts/Framework/Manager/ResourceSystem/ResourceCustomConfigs/Config.asset";
        
        private readonly Dictionary<string, string> _asset2Bundles = new Dictionary<string, string>();

        private readonly Dictionary<string, BuildRule>
            _bundlesRule = new Dictionary<string, BuildRule>(); //记录这个bundle用了哪条规则

        private readonly Dictionary<string, string[]> _conflicted = new Dictionary<string, string[]>();
        private readonly Dictionary<string, string> _duplicated = new Dictionary<string, string>(); //被相同bundle依赖的资源
        private readonly Dictionary<string, HashSet<string>> _tracker = new Dictionary<string, HashSet<string>>();

        //暂时先支持一个变体路径也省的乱
        public string variantPath = null;
        public List<BuildRule> variantRules = new List<BuildRule>();
        public List<string> variantDirNames;
        
        public List<BuildRule> rules = new List<BuildRule>();
        [Header("Assets")] public RuleAsset[] ruleAssets = new RuleAsset[0];
        public RuleBundle[] ruleBundles = new RuleBundle[0];

        // 为了解决Bundle和文件夹同名冲突
        public string abExtName = "ab";

        #region API

        public string GetVariantPath()
        {
            if (string.IsNullOrEmpty(variantPath))
            {
                return variantPath;
            }
            var path = Path.Combine(Application.dataPath, variantPath);
            path = PathManager.GetRegularPath(path);
            return path;
        }

        /// <summary>
        /// 获取变体目录名
        /// </summary>
        /// <returns></returns>
        private List<string> GetVariantDirNames()
        {
            if (variantPath == null)
            {
                throw new Exception("No Variant Path");
            }
            
            var path = Path.Combine(Application.dataPath, variantPath);
            var directory = new DirectoryInfo(path);
            return directory
                .GetDirectories()
                .Select(subDir => subDir.Name)
                .ToList();
        }

        //variantPath 第一层子目录作为变体种类
        public string[] GetVariants()
        {
            if (string.IsNullOrEmpty(variantPath))
            {
                return new string[0];
            }

            var path = GetVariantPath();
            if (string.IsNullOrEmpty(path))
            {
                return new string[0];
            }
            var vs = Directory.GetDirectories(path);
            for (int i = 0; i < vs.Length; i++)
            {
                if (vs[i].StartsWith(path))
                {
                    vs[i] = vs[i].Substring(path.Length + 1);
                }
            }

            return vs;
        }
        private string GetVariant(string s)
        {
            var variantPath = GetVariantPath();
            if (string.IsNullOrEmpty(variantPath)) return string.Empty;
            var path = PathManager.GetRegularPath(Path.GetFullPath(s));
            if (string.IsNullOrEmpty(path)) return "";
            if (!path.StartsWith(variantPath)) return "";
            var vs = Directory.GetDirectories(variantPath);
            for (int i = 0; i < vs.Length; i++)
            {
                var v = PathManager.GetRegularPath(vs[i]);
                if (path.StartsWith(v))
                {
                    return v.Substring(variantPath.Length + 1);
                }
            }
            return string.Empty;
        }

        public void Apply()
        {
            try
            {
                Clear();
                CollectAssets();
                AnalysisAssets();
                OptimizeAssets();
                MakeAssetList();
                MakeBundleList();
                Save();
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                UnityEditor.EditorUtility.ClearProgressBar();
            }
        }

        public AssetBundleBuild[] GetBuilds()
        {
            var builds = new List<AssetBundleBuild>();
            foreach (var bundle in ruleBundles)
            {
                builds.Add(new AssetBundleBuild
                {
                    assetNames = bundle.assets,
                    assetBundleName = bundle.name,
                    assetBundleVariant = bundle.variant
                });
            }

            return builds.ToArray();
        }

        #endregion

        #region Private

        internal static bool ValidateAsset(string asset)
        {
            asset = PathManager.GetRegularPath(asset);
            if (!asset.StartsWith("Assets/")) return false;

            var ext = Path.GetExtension(asset).ToLower();
            if (ext == "*.dll" && !asset.StartsWith("Assets/ResourcesAssets/")) return false;
            if (ext == ".cs" || ext == ".meta" || ext == ".js" || ext == ".boo")
            {
                return false;
            }
            //排除LightMap
            var isAsset = ext == ".asset";
            if (isAsset)
            {
                return !(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(asset) is LightingDataAsset);
            }

            return true;
        }

        private static bool IsScene(string asset)
        {
            return asset.EndsWith(".unity");
        }
        
        private string GetOriginPath(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;
            var n2v = path.Split('.');
            if (n2v.Length > 0)
            {
                return $"{n2v[0]}_{abExtName}";
            }
            return null;
        }
        
        private string GetVariantFromPath(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;
            var n2v = path.Split('.');
            if (n2v.Length > 1)
            {
                return n2v[1];
            }
            return null;
        }

        private string MakeBundleName(string name, string variant)
        {
            if (string.IsNullOrEmpty(name)) return null;
            
            var assetBundle = PathManager.GetRegularPath(name);
            assetBundle = assetBundle.Replace(' ', '_');
            assetBundle = assetBundle.Replace('.', '_');
            
            if (string.IsNullOrEmpty(variant))
                return assetBundle;
            
            return assetBundle + "." + variant;
        }

        /// <summary>
        /// 记录依赖于该asset的所有bundle
        /// </summary>
        private void Track(string asset, string bundle)
        {
            HashSet<string> assets;
            if (!_tracker.TryGetValue(asset, out assets))
            {
                assets = new HashSet<string>();
                _tracker.Add(asset, assets);
            }

            assets.Add(bundle);
        }

        private Dictionary<string, List<string>> GetBundles()
        {
            var bundles = new Dictionary<string, List<string>>();
            foreach (var item in _asset2Bundles)
            {
                var bundle = item.Value;
                List<string> list;
                if (!bundles.TryGetValue(bundle, out list))
                {
                    list = new List<string>();
                    bundles[bundle] = list;
                }

                if (!list.Contains(item.Key)) list.Add(item.Key);
            }

            return bundles;
        }

        private void Clear()
        {
            _bundlesRule.Clear();
            _tracker.Clear();
            _duplicated.Clear();
            _conflicted.Clear();
            _asset2Bundles.Clear();
        }

        private void Save()
        {
            UnityEditor.EditorUtility.ClearProgressBar();
            UnityEditor.EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
        }

        private void OptimizeAssets()
        {
            int i = 0, max = _conflicted.Count;
            foreach (var item in _conflicted)
            {
                if (UnityEditor.EditorUtility.DisplayCancelableProgressBar(string.Format("优化冲突{0}/{1}", i, max), item.Key,
                    i / (float) max)) break;
                var list = item.Value;
                foreach (var asset in list)
                    if (!IsScene(asset))
                        _duplicated.Add(asset, item.Key + "_ext");
                i++;
            }

            i = 0;
            max = _duplicated.Count;
            foreach (var item in _duplicated)
            {
                if (UnityEditor.EditorUtility.DisplayCancelableProgressBar(string.Format("优化冗余{0}/{1}", i, max), item.Key,
                    i / (float) max)) break;
                OptimizeAsset(item);
                i++;
            }
        }

        private void AnalysisAssets()
        {
            var getBundles = GetBundles();
            int i = 0, max = getBundles.Count;
            foreach (var item in getBundles)
            {
                var bundle = item.Key;
                if (UnityEditor.EditorUtility.DisplayCancelableProgressBar(string.Format("分析依赖{0}/{1}", i, max), bundle,
                    i / (float) max)) break;
                var assetPaths = getBundles[bundle];
                if (assetPaths.Exists(IsScene) && !assetPaths.TrueForAll(IsScene))
                    _conflicted.Add(bundle, assetPaths.ToArray());
                var dependencies = AssetDatabase.GetDependencies(assetPaths.ToArray(), true);
                if (dependencies.Length > 0)
                    foreach (var asset in dependencies)
                        if (ValidateAsset(asset) && !IsScene(asset))
                            Track(asset, bundle);
                i++;
            }

            foreach (var assets in _tracker)
            {
                if (assets.Value.Count > 1) //如果资源被两个以上bundle包含
                {
                    string bundleName;
                    _asset2Bundles.TryGetValue(assets.Key, out bundleName);
                    if (string.IsNullOrEmpty(bundleName))
                    {
                        // 为被多个bundle依赖的asset生成bundle名，并存储
                        _duplicated.Add(assets.Key, HashSetToString(assets.Value));
                    }
                }
            }
        }
 
        /// <summary>
        /// 拼接所有bundle名计算出hash值
        /// </summary>
        /// <param name="hashset">bundle名容器</param>
        private string HashSetToString(HashSet<string> hashset)
        {
            var s = new StringBuilder();
            foreach (var val in hashset)
            {
                s.Append(val);
                s.Append("/");
            }
            using (var md5 = MD5.Create())
            {
                var hash = md5.ComputeHash(Encoding.Default.GetBytes(s.ToString()));
                return "Shared/" + new Guid(hash).ToString();
            }
        }

        private void CollectAssets()
        {
            for (int i = 0, max = rules.Count; i < max; i++)
            {
                var rule = rules[i];
                if (UnityEditor.EditorUtility.DisplayCancelableProgressBar(string.Format("收集资源{0}/{1}", i, max), rule.searchPath,
                    i / (float) max))
                    break;
                rule.prefixPath = "Assets/";
                ApplyRule(rule);
            }

            var vs = GetVariants();
            foreach (var v in vs)
            {
                for (int i = 0, max = variantRules.Count; i < max; i++)
                {
                    var rule = variantRules[i];
                    if (UnityEditor.EditorUtility.DisplayCancelableProgressBar(string.Format("收集资源{0}/{1}", i, max),
                        rule.searchPath,
                        i / (float) max))
                        break;
                    rule.prefixPath = Path.Combine("Assets", variantPath, v);
                    ApplyRule(rule);
                }
            }
            
            variantDirNames = GetVariantDirNames();
        }

        private void MakeBundleList()
        {
            
            var getBundles = GetBundles();
            ruleBundles = new RuleBundle[getBundles.Count];
            var i = 0;
            foreach (var item in getBundles)
            {
                ruleBundles[i] = new RuleBundle
                {
                    assets = item.Value.ToArray(),
                    
                };
                ruleBundles[i].name = GetOriginPath(item.Key);
                ruleBundles[i].variant = GetVariantFromPath(item.Key);
                if (_bundlesRule.ContainsKey(item.Key))
                {
                    ruleBundles[i].packed = _bundlesRule[item.Key].packed;
                    ruleBundles[i].loadType = _bundlesRule[item.Key].loadType;
                    ruleBundles[i].tag = _bundlesRule[item.Key].tag;
                }
                i++;
            }
        }

        private void MakeAssetList()
        {
            var list = new List<RuleAsset>();
            foreach (var item in _asset2Bundles)
            {
                var asset = new RuleAsset
                {
                    assetName = item.Key,
                    guid = AssetDatabase.AssetPathToGUID(item.Key)
                };
                
                
                list.Add(asset);
                asset.bundle = GetOriginPath(item.Value);
                asset.resourceVariant = GetVariantFromPath(item.Value);
            }
            list.Sort((a, b) => string.Compare(a.assetName, b.assetName, StringComparison.Ordinal));
            ruleAssets = list.ToArray();
        }

        private void OptimizeAsset(KeyValuePair<string,string> assetKp)
        {
            var asset = assetKp.Key;
            var depBundle = assetKp.Value;
            if (asset.EndsWith(".shader"))
                _asset2Bundles[asset] = MakeBundleName("shaders",null);
            else
            {
                //直接把被依赖资源单独打包
                // _asset2Bundles[asset] = MakeBundleName(asset, GetVariant(asset));
                
                //优化 把依赖相同的资源打到同一个bundle
                _asset2Bundles[asset] = MakeBundleName(depBundle, GetVariant(asset));
            }
        }

        private void UpdateBundleRule(string path, BuildRule rule)
        {
            if (_bundlesRule.ContainsKey(path))
            {
                _bundlesRule[path] = rule;
            }
            else
            {
                _bundlesRule.Add(path, rule);
            }
        }
        
        private void ApplyRule(BuildRule rule)
        {
            var assets = rule.GetAssets();
            switch (rule.nameBy)
            {
                case NameBy.Explicit:
                {
                    Debug.Assert(!string.IsNullOrEmpty(rule.assetBundleName),$" {rule} 需要指定 assetBundleName");
                    foreach (var asset in assets)
                    {
                        _asset2Bundles[asset] = MakeBundleName(rule.assetBundleName, GetVariant(asset));
                        UpdateBundleRule(_asset2Bundles[asset], rule);
                    }

                    break;
                }
                case NameBy.Path:
                {
                    foreach (var asset in assets)
                    {
                        var assetName = asset;
                        var variant = GetVariant(asset);
                        // 保证变体bundle名一致
                        if (!string.IsNullOrEmpty(variant))
                        {
                            assetName = assetName.Replace($"/{variant}", "");
                        }
                        
                        _asset2Bundles[asset] = MakeBundleName(assetName, variant);
                        UpdateBundleRule(_asset2Bundles[asset], rule);
                    }

                    break;
                }
                case NameBy.Directory:
                {
                    foreach (var asset in assets)
                    {
                        var dir = Path.GetDirectoryName(asset);
                        var variant = GetVariant(asset);
                        // 保证变体bundle名一致
                        if (!string.IsNullOrEmpty(variant))
                        {
                            dir = dir.Replace($"/{variant}", "");
                        }
                        
                        _asset2Bundles[asset] = MakeBundleName(dir, variant);
                        UpdateBundleRule(_asset2Bundles[asset], rule);
                    }

                    break;
                }
                case NameBy.TopDirectory:
                {
                    var rs =  PathManager.GetRegularPath(rule.GetFullSearchPath());
                    var startIndex = rs.Length;
                    foreach (var asset in assets)
                    {
                        var dir = PathManager.GetRegularPath(Path.GetDirectoryName(asset));
                        if (!string.IsNullOrEmpty(dir))
                            if (!dir.Equals(rs))
                            {
                                var pos = dir.IndexOf("/", startIndex, StringComparison.Ordinal);
                                if (pos != -1) dir = dir.Substring(0, pos);
                            }

                        _asset2Bundles[asset] = MakeBundleName(dir, GetVariant(dir));
                        UpdateBundleRule(_asset2Bundles[asset], rule);
                    }

                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        #endregion
    }
}