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

        [Tooltip("搜索通配符，多个之间请用,(逗号)隔开")] public string searchPattern = "*.prefab";
        
        [Tooltip("资源分组标签（打了标签热更时会忽略)")] public string tag = string.Empty;

        [Tooltip("打包规则")] public PackRule packRule = PackRule.PackSeparately;

        [Tooltip("Explicit的名称")] public string assetBundleName;

        [Tooltip("是否进安装包")] public bool packed = true;

        /// <summary>
        /// 获取搜索路径下的资源
        /// </summary>
        /// <returns></returns>
        public string[] GetAssets()
        {
            if (!valid) return Array.Empty<string>();

            var path = GetFullSearchPath();
            var patterns = searchPattern.Split(new[] {','}, StringSplitOptions.RemoveEmptyEntries);
            if (!Directory.Exists(path))
            {
                Debug.LogError("Rule searchPath not exist: " + path);
                return Array.Empty<string>();
            }

            var getFiles = new List<string>();
            foreach (var pattern in patterns)
            {
                var files = Directory.GetFiles(path, pattern, SearchOption.AllDirectories);
                foreach (var file in files)
                {
                    if (Directory.Exists(file)) continue;
                    var ext = Path.GetExtension(file).ToLower();
                    if (!pattern.Contains(ext)) continue;
                    if (!BuildRules.ValidateAsset(file)) continue;
                    var asset = PathManager.GetRegularPath(file);
                    getFiles.Add(asset);
                }
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
        public static string ruleConfigPath = "Assets/Scripts/UAsset/BuildRules.asset";
        
        /// <summary>
        /// 资源和bundle的映射表
        /// </summary>
        private readonly Dictionary<string, string> _asset2Bundles = new Dictionary<string, string>();

        /// <summary>
        /// //记录这个bundle用了哪条规则
        /// </summary>
        private readonly Dictionary<string, BuildRule> _bundlesRule = new Dictionary<string, BuildRule>();

        private readonly Dictionary<string, string[]> _conflicted = new Dictionary<string, string[]>();
        
        /// <summary>
        /// 被相同bundle依赖的资源
        /// </summary>
        private readonly Dictionary<string, string> _duplicated = new Dictionary<string, string>();
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

        // TODO: 这个方法还比较糙
        /// <summary>
        /// 验证资源合法性
        /// </summary>
        /// <param name="assetPath"></param>
        /// <returns></returns>
        internal static bool ValidateAsset(string assetPath)
        {
            assetPath = PathManager.GetRegularPath(assetPath);
            if (!assetPath.StartsWith("Assets/")) return false;

            var ext = Path.GetExtension(assetPath).ToLower();
            if (ext == ".cs" || ext == ".meta" || ext == ".js" || ext == ".boo")
            {
                return false;
            }
            //排除LightMap
            if (ext == ".asset")
            {
                return !(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath) is LightingDataAsset);
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
            var splits = path.Split('.');
            if (splits.Length > 0)
            {
                return $"{splits[0]}_{abExtName}";
            }
            return null;
        }

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
                {
                    foreach (var asset in dependencies)
                    {
                        if (ValidateAsset(asset) && !IsScene(asset))
                            Track(asset, bundle);
                    }
                }
                i++;
            }

            foreach (var assets in _tracker)
            {
                if (assets.Value.Count > 1) //如果资源被两个以上bundle包含
                {
                    _asset2Bundles.TryGetValue(assets.Key, out var bundleName);
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
                return "Shared/" + new Guid(hash);
            }
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
                ruleBundles[i].variant = GetVariantNameFromPath(item.Key);
                if (_bundlesRule.ContainsKey(item.Key))
                {
                    ruleBundles[i].packed = _bundlesRule[item.Key].packed;
                    ruleBundles[i].packByRaw = _bundlesRule[item.Key].packRule == PackRule.PackRawFile;
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
                asset.resourceVariant = GetVariantNameFromPath(item.Value);
            }
            list.Sort((a, b) => string.Compare(a.assetName, b.assetName, StringComparison.Ordinal));
            ruleAssets = list.ToArray();
        }

        private void OptimizeAsset(KeyValuePair<string,string> assetKp)
        {
            var asset = assetKp.Key;
            var depBundle = assetKp.Value;
            if (asset.EndsWith(".shader"))
                _asset2Bundles[asset] = MakeBundleName("shaders", null);
            else
            {
                // 把依赖相同的资源打到同一个bundle
                _asset2Bundles[asset] = MakeBundleName(depBundle, GetVariantName(asset));
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
            switch (rule.packRule)
            {
                case PackRule.PackExplicit:
                {
                    Debug.Assert(!string.IsNullOrEmpty(rule.assetBundleName),$" {rule} 需要指定 assetBundleName");
                    foreach (var asset in assets)
                    {
                        _asset2Bundles[asset] = MakeBundleName(rule.assetBundleName, GetVariantName(asset));
                        UpdateBundleRule(_asset2Bundles[asset], rule);
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
                        UpdateBundleRule(_asset2Bundles[asset], rule);
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
                        UpdateBundleRule(_asset2Bundles[asset], rule);
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
                            if (!dir.Equals(rs))
                            {
                                var pos = dir.IndexOf("/", startIndex, StringComparison.Ordinal);
                                if (pos != -1) dir = dir.Substring(0, pos);
                            }

                        _asset2Bundles[asset] = MakeBundleName(dir, GetVariantName(dir));
                        UpdateBundleRule(_asset2Bundles[asset], rule);
                    }

                    break;
                }
                case PackRule.PackRawFile: // 不处理原生文件
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
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
    }
}