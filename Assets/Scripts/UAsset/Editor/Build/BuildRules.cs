using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

namespace UAsset.Editor
{
    /// <summary>
    /// 打包规则
    /// </summary>
    public enum PackRule
    {
        PackExplicit,       // 显示指定bundle名
        PackSeparately,     // 文件路径为bundle名（每个文件打独立bundle）
        PackByDirectory,    // 最后一层目录为bundle名（文件夹下所有的文件打成一个bundle）
        PackByTopDirectory, // 规则路径的顶层目录为bundle名（文件夹下所有的文件打成一个bundle)
        PackRawFile         // 原生文件    
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
        public bool packed = true;
        public string[] assets;
        public string variant;
        public string tag;
        public bool packByRaw;

        public bool IsRawFile => packByRaw;
    }

    [Serializable]
    public class BuildRule
    {
        public bool valid = true;

        [NonSerialized] public string prefixPath = "";

        [Tooltip("搜索路径")] public string searchPath;

        [Tooltip("资源分组标签（打了标签热更时会忽略)")] public string tag = string.Empty;

        [Tooltip("打包规则")] public PackRule packRule = PackRule.PackSeparately;

        [Tooltip("Explicit的名称")] public string assetBundleName;

        [Tooltip("是否进安装包")] public bool packed = true;

        [Tooltip("文件过滤规则")] public string filterRule = nameof(CollectAll);

        /// <summary>
        /// 获取搜索路径下的资源
        /// </summary>
        /// <returns></returns>
        public string[] GetAssets()
        {
            if (!valid) return Array.Empty<string>();

            var path = GetFullSearchPath();
            if (!Directory.Exists(path))
            {
                Debug.LogError("Rule searchPath not exist: " + path);
                return Array.Empty<string>();
            }

            var getFiles = new List<string>();
            var files = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories);
            foreach (var file in files)
            {
                var filter = FileFilterRule.GetFilterRuleInstance(filterRule);
                if(!BuildRules.IsValidAsset(file) || !filter.IsCollectAsset(file)) continue;
                
                var asset = PathManager.GetRegularPath(file);
                getFiles.Add(asset);
            }

            return getFiles.ToArray();
        }
        
        /// <summary>
        /// 获取搜索全路径
        /// </summary>
        public string GetFullSearchPath()
        {
            return Path.Combine(prefixPath, searchPath);
        }
    }

    public class BuildRules : ScriptableObject
    {
        /// <summary>
        /// 规则配置文件存储路径
        /// </summary>
        public static string ruleConfigPath = "Assets/Scripts/UAsset/BuildRules.asset";
        
        /// <summary>
        /// 资源和bundle的映射表
        /// </summary>
        private readonly Dictionary<string, string> _asset2Bundles = new Dictionary<string, string>();

        /// <summary>
        /// 记录这个bundle用了哪条规则
        /// </summary>
        private readonly Dictionary<string, BuildRule> _bundlesRule = new Dictionary<string, BuildRule>();

        /// <summary>
        /// 记录场景目录下的资源
        /// </summary>
        private readonly Dictionary<string, string[]> _conflicted = new Dictionary<string, string[]>();
        
        /// <summary>
        /// 被多个bundle依赖的共享资源(asset-bundle名映射表)
        /// </summary>
        private readonly Dictionary<string, string> _shared = new Dictionary<string, string>();
        
        /// <summary>
        /// asset-依赖bundle映射表
        /// </summary>
        private readonly Dictionary<string, HashSet<string>> _tracker = new Dictionary<string, HashSet<string>>();

        //暂时先支持一个变体路径也省的乱
        public string variantRootPath;
        public List<BuildRule> variantRules = new List<BuildRule>();
        public string[] variantDirNames;
        
        public List<BuildRule> rules = new List<BuildRule>();
        [Header("Assets")] public RuleAsset[] ruleAssets;
        public RuleBundle[] ruleBundles;

        /// <summary>
        /// bundle扩展名 (为了解决Bundle和文件夹同名冲突)
        /// </summary>
        public string abExtName = "ab";

        public void Apply()
        {
            try
            {
                Clear();
                CollectAssets();
                AnalysisAssets();
                OptimizeAssets();
                MakeRuleAssetList();
                MakeBundleList();
                Save();
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                UnityEditor.EditorUtility.ClearProgressBar();
            }
        }
        
        /// <summary>
        /// 收集打包资源
        /// </summary>
        private void CollectAssets()
        {
            for (int i = 0, max = rules.Count; i < max; i++)
            {
                var rule = rules[i];
                if (UnityEditor.EditorUtility.DisplayCancelableProgressBar($"收集资源{i}/{max}", rule.searchPath, i / (float) max))
                    break;
                
                rule.prefixPath = "Assets/";
                ApplyRule(rule);
            }

            variantDirNames = GetVariantDirNames();
            foreach (var v in variantDirNames)
            {
                for (int i = 0, max = variantRules.Count; i < max; i++)
                {
                    var rule = variantRules[i];
                    if (UnityEditor.EditorUtility.DisplayCancelableProgressBar($"收集资源{i}/{max}", rule.searchPath, i / (float) max))
                        break;
                    
                    rule.prefixPath = Path.Combine("Assets", variantRootPath, v);
                    ApplyRule(rule);
                }
            }
        }

        /// <summary>
        /// 分析资源
        /// </summary>
        private void AnalysisAssets()
        {
            var getBundles = GetBundles();
            int i = 0, max = getBundles.Count;
            foreach (var item in getBundles)
            {
                var bundle = item.Key;
                if (UnityEditor.EditorUtility.DisplayCancelableProgressBar($"分析依赖{i}/{max}", bundle, i / (float) max)) 
                    break;
                
                var assetPaths = getBundles[bundle];
                if (assetPaths.Exists(IsScene) && !assetPaths.TrueForAll(IsScene))
                    _conflicted.Add(bundle, assetPaths.ToArray());
                
                // 记录bundle依赖的资源
                var dependencies = AssetDatabase.GetDependencies(assetPaths.ToArray(), true);
                if (dependencies.Length > 0)
                {
                    foreach (var asset in dependencies)
                    {
                        if (IsValidAsset(asset) && !IsScene(asset))
                            Track(asset, bundle);
                    }
                }
                i++;
            }

            foreach (var map in _tracker)
            {
                if (map.Value.Count > 1) //如果资源被两个以上bundle包含
                {
                    _asset2Bundles.TryGetValue(map.Key, out var bundleName);
                    if (string.IsNullOrEmpty(bundleName)) // 资源尚未被加进任何bungle
                    {
                        // 为被多个bundle依赖的asset生成bundle名，并存储
                        _shared.Add(map.Key, MakeSharedBundleName(map.Value));
                    }
                }
            }
        }
        
        /// <summary>
        /// 优化资源分析
        /// </summary>
        private void OptimizeAssets()
        {
            int i = 0, max = _conflicted.Count;
            foreach (var item in _conflicted)
            {
                if (UnityEditor.EditorUtility.DisplayCancelableProgressBar($"优化冲突{i}/{max}", item.Key, i / (float) max)) 
                    break;
                
                var list = item.Value;
                foreach (var asset in list)
                {
                    if (!IsScene(asset)) // 场景目录下的非场景资源打成一个共享bundle
                        _shared.Add(asset, item.Key + "_ext");
                }
                i++;
            }

            i = 0; max = _shared.Count;
            foreach (var item in _shared)
            {
                if (UnityEditor.EditorUtility.DisplayCancelableProgressBar($"优化冗余{i}/{max}", item.Key, i / (float) max)) 
                    break;
                
                var asset = item.Key;
                var bundle = item.Value;
                if (asset.EndsWith(".shader"))
                {
                    // 所有被共享的shader资源打成一个bundle
                    _asset2Bundles[asset] = MakeBundleName("shaders", null);
                }
                else
                {
                    // 把依赖相同的资源打到同一个bundle
                    _asset2Bundles[asset] = MakeBundleName(bundle, GetVariantName(asset));
                }

                i++;
            }
        }
        
        /// <summary>
        /// 创建资源列表
        /// </summary>
        private void MakeRuleAssetList()
        {
            var list = new List<RuleAsset>();
            foreach (var item in _asset2Bundles)
            {
                var asset = new RuleAsset
                {
                    assetName = item.Key,
                    guid = AssetDatabase.AssetPathToGUID(item.Key),
                    bundle = GetBundleName(item.Value),
                    resourceVariant = GetVariantNameFromPath(item.Value)
                };

                list.Add(asset);
            }
            list.Sort((a, b) => string.Compare(a.assetName, b.assetName, StringComparison.Ordinal));
            ruleAssets = list.ToArray();
        }
        
        /// <summary>
        /// 创建bundle列表
        /// </summary>
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
                    name = GetBundleName(item.Key),
                    variant = GetVariantNameFromPath(item.Key)
                };
                
                if (_bundlesRule.ContainsKey(item.Key))
                {
                    ruleBundles[i].packed = _bundlesRule[item.Key].packed;
                    ruleBundles[i].packByRaw = _bundlesRule[item.Key].packRule == PackRule.PackRawFile;
                    ruleBundles[i].tag = _bundlesRule[item.Key].tag;
                }
                i++;
            }
        }
        
        /// <summary>
        /// 是否是合法资源
        /// </summary>
        /// <param name="assetPath"></param>
        /// <returns></returns>
        internal static bool IsValidAsset(string assetPath)
        {
            if (assetPath.StartsWith("Assets/") == false && assetPath.StartsWith("Packages/") == false)
            {
                Debug.LogError($"Invalid asset path : {assetPath}");
                return false;
            }
            if (assetPath.Contains("/Gizmos/"))
            {
                Debug.LogWarning($"Cannot pack gizmos asset : {assetPath}");
                return false;
            }

            if (AssetDatabase.IsValidFolder(assetPath))
                return false;

            // 注意：忽略编辑器下的类型资源
            var type = AssetDatabase.GetMainAssetTypeAtPath(assetPath);
            if (type == typeof(LightingDataAsset))
                return false;

            var ext = Path.GetExtension(assetPath);
            if (ext == "" || ext == ".cs" || ext == ".js" || ext == ".boo" || ext == ".meta" || ext == ".cginc")
                return false;

            return true;
        }
        
        /// <summary>
        /// 是否是场景资源
        /// </summary>
        /// <param name="assetPath">资源路径</param>
        /// <returns></returns>
        private static bool IsScene(string assetPath)
        {
            return assetPath.EndsWith(".unity");
        }

        /// <summary>
        /// 记录依赖于该asset的所有bundle
        /// </summary>
        private void Track(string asset, string bundle)
        {
            if (!_tracker.TryGetValue(asset, out var assets))
            {
                assets = new HashSet<string>();
                _tracker.Add(asset, assets);
            }

            assets.Add(bundle);
        }

        /// <summary>
        /// 获取bundle-assets映射表
        /// </summary>
        /// <returns></returns>
        private Dictionary<string, List<string>> GetBundles()
        {
            var bundles = new Dictionary<string, List<string>>();
            foreach (var item in _asset2Bundles)
            {
                var bundle = item.Value;
                if (!bundles.TryGetValue(bundle, out var list))
                {
                    list = new List<string>();
                    bundles[bundle] = list;
                }

                if (!list.Contains(item.Key)) 
                    list.Add(item.Key);
            }

            return bundles;
        }

        /// <summary>
        /// 记录bundle对应的规则
        /// </summary>
        /// <param name="path"></param>
        /// <param name="rule"></param>
        private void RecordBundleRule(string path, BuildRule rule)
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
        
        /// <summary>
        /// 为规则路径下的资源创建bundle名
        /// </summary>
        /// <param name="rule">打包规则</param>
        private void ApplyRule(BuildRule rule)
        {
            var assets = rule.GetAssets();
            switch (rule.packRule)
            {
                case PackRule.PackExplicit:
                {
                    Debug.Assert(!string.IsNullOrEmpty(rule.assetBundleName),$" {rule} 需要指定 assetBundleName");
                    foreach (var asset in assets)
                    {
                        _asset2Bundles[asset] = MakeBundleName(rule.assetBundleName, GetVariantName(asset));
                        RecordBundleRule(_asset2Bundles[asset], rule);
                    }

                    break;
                }
                case PackRule.PackSeparately:
                {
                    foreach (var asset in assets)
                    {
                        var assetPath = asset;
                        var variant = GetVariantName(asset);
                        // 保证变体bundle名一致
                        if (!string.IsNullOrEmpty(variant))
                        {
                            assetPath = assetPath.Replace($"/{variant}", "");
                        }
                        
                        _asset2Bundles[asset] = MakeBundleName(assetPath, variant);
                        RecordBundleRule(_asset2Bundles[asset], rule);
                    }

                    break;
                }
                case PackRule.PackByDirectory:
                {
                    foreach (var asset in assets)
                    {
                        var dir = Path.GetDirectoryName(asset);
                        var variant = GetVariantName(asset);
                        // 保证变体bundle名一致
                        if (!string.IsNullOrEmpty(variant))
                        {
                            dir = dir.Replace($"/{variant}", "");
                        }
                        
                        _asset2Bundles[asset] = MakeBundleName(dir, variant);
                        RecordBundleRule(_asset2Bundles[asset], rule);
                    }

                    break;
                }
                case PackRule.PackByTopDirectory:
                {
                    var rs =  PathManager.GetRegularPath(rule.GetFullSearchPath());
                    var startIndex = rs.Length;
                    foreach (var asset in assets)
                    {
                        var dir = PathManager.GetRegularPath(Path.GetDirectoryName(asset));
                        if (!string.IsNullOrEmpty(dir))
                        {
                            if (!dir.Equals(rs))
                            {
                                var pos = dir.IndexOf("/", startIndex, StringComparison.Ordinal);
                                if (pos != -1) dir = dir.Substring(0, pos);
                            }
                        }

                        _asset2Bundles[asset] = MakeBundleName(dir, GetVariantName(dir));
                        RecordBundleRule(_asset2Bundles[asset], rule);
                    }

                    break;
                }
                case PackRule.PackRawFile:
                    foreach (var asset in assets)
                    {
                        // 注意：原生文件只支持无依赖关系的资源
                        string[] depends = AssetDatabase.GetDependencies(asset, true);
                        if (depends.Length != 1)
                            throw new Exception("RawFile cannot depend by other assets");
                        
                        _asset2Bundles[asset] = MakeBundleName(asset, null);
                        RecordBundleRule(_asset2Bundles[asset], rule);
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        
        private void Clear()
        {
            _bundlesRule.Clear();
            _tracker.Clear();
            _shared.Clear();
            _conflicted.Clear();
            _asset2Bundles.Clear();
        }

        private void Save()
        {
            UnityEditor.EditorUtility.ClearProgressBar();
            UnityEditor.EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
        }
        
        #region 处理变体
        
        /// <summary>
        /// 获取变体路径
        /// </summary>
        /// <returns></returns>
        private string GetVariantPath()
        {
            if (string.IsNullOrEmpty(variantRootPath))
            {
                return variantRootPath;
            }
            var path = Path.Combine(Application.dataPath, variantRootPath);
            path = PathManager.GetRegularPath(path);
            return path;
        }

        /// <summary>
        /// 获取变体目录名（变体根目录下第一层子目录作为变体种类）
        /// </summary>
        /// <returns></returns>
        public string[] GetVariantDirNames()
        {
            if (variantRootPath == null)
            {
                return Array.Empty<string>();
            }
            
            var path = GetVariantPath();
            var directory = new DirectoryInfo(path);
            return directory
                .GetDirectories()
                .Select(subDir => subDir.Name)
                .ToArray();
        }

        /// <summary>
        /// 获取资源的变体名
        /// </summary>
        /// <param name="assetPath">资源路径</param>
        /// <returns>变体则返回变体目录名;非变体返回“”</returns>
        private string GetVariantName(string assetPath)
        {
            var variantPath = GetVariantPath();
            if (string.IsNullOrEmpty(variantPath)) return string.Empty;
            
            var path = PathManager.GetRegularPath(Path.GetFullPath(assetPath));
            if (string.IsNullOrEmpty(path) || !path.StartsWith(variantPath)) return string.Empty;
            
            var subDirs = Directory.GetDirectories(variantPath);
            foreach (var dir in subDirs)
            {
                var d = PathManager.GetRegularPath(dir);
                if (path.StartsWith(d))
                {
                    return d.Substring(variantPath.Length + 1);
                }
            }
            return string.Empty;
        }
        
        /// <summary>
        /// 从路径中获取变体名
        /// </summary>
        /// <param name="path">路径</param>
        /// <returns>变体名</returns>
        private string GetVariantNameFromPath(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;
            var splits = path.Split('.');
            return splits.Length > 1 ? splits[1] : null;
        }
        
        #endregion
        
        #region 创建bundle名相关
        
        /// <summary>
        /// 构建bundle名
        /// </summary>
        /// <param name="path">资源路径</param>
        /// <param name="variant">变体名</param>
        /// <returns></returns>
        private string MakeBundleName(string path, string variant)
        {
            if (string.IsNullOrEmpty(path)) return null;
            
            var assetBundle = PathManager.GetRegularPath(path);
            assetBundle = assetBundle.Replace(' ', '_');
            assetBundle = assetBundle.Replace('.', '_');
            
            if (string.IsNullOrEmpty(variant))
                return assetBundle;
            
            return assetBundle + "." + variant;
        }
        
        /// <summary>
        /// 拼接所有bundle名计算出hash值
        /// </summary>
        /// <param name="hashset">bundle名容器</param>
        private string MakeSharedBundleName(HashSet<string> hashset)
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
                return "Shared/" + new Guid(hash);
            }
        }
        
        /// <summary>
        /// 获取bundle名
        /// </summary>
        /// <param name="path">路径</param>
        /// <returns>bundle名</returns>
        private string GetBundleName(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;
            var splits = path.Split('.');
            return splits.Length > 0 ? $"{splits[0]}_{abExtName}" : null;
        }
        
        #endregion
    }
}