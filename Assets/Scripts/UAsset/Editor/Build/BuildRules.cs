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

    /// <summary>
    /// 收集器类型
    /// </summary>
    public enum CollectorType
    {
        /// <summary>
        /// 收集参与打包的主资源对象，并写入到资源清单的资源列表里（可以通过代码加载）。
        /// </summary>
        MainAssetCollector,
        
        /// <summary>
        /// 收集参与打包的依赖资源对象，目前为了灵活也写入资源列表里（可以通过代码加载）。
        /// 注意：如果依赖资源对象没有被主资源对象引用，则不参与打包构建。
        /// </summary>
        DependAssetCollector
    }

    [Serializable]
    public class RuleAsset
    {
        public string assetName;
        public string bundle;
        public string guid;
        public string resourceVariant;
        public bool rawFile;
        public List<string> dependAssets = new List<string>();
        public CollectorType collectorType;
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
        [Tooltip("是否激活规则")] public bool valid = true;
        
        [Tooltip("搜索路径")] public string searchPath;

        [Tooltip("资源分组标签（打了标签热更时会忽略)")] public string tag = string.Empty;

        [Tooltip("打包规则")] public PackRule packRule = PackRule.PackSeparately;

        [Tooltip("Explicit的名称")] public string assetBundleName;

        [Tooltip("是否进安装包")] public bool packed = true;

        [Tooltip("文件过滤规则")] public string filterRule = nameof(CollectAll);

        [Tooltip("收集器类型")] public CollectorType collectorType = CollectorType.MainAssetCollector;

        /// <summary>
        /// 获取搜索路径下的资源
        /// </summary>
        /// <returns></returns>
        public string[] GetAssets()
        {
            // 检测规则是否激活
            if (!valid) return Array.Empty<string>();

            var path = GetSearchPath();
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
        public string GetSearchPath()
        {
            return searchPath;
        }
    }

    public class BuildRules : ScriptableObject
    {
        /// <summary>
        /// 规则配置文件存储路径
        /// </summary>
        public const string kRuleConfigPath = "Assets/Scripts/UAsset/BuildRules.asset";

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
        /// 收集的所有需打包资源信息
        /// </summary>
        private readonly Dictionary<string, RuleAsset> _collectedRuleAsset = new Dictionary<string, RuleAsset>();

        /// <summary>
        /// 变体文件根目录，暂时只支持一个变体路径
        /// </summary>
        public string variantRootPath;
        /// <summary>
        /// 变体打包规则集合
        /// </summary>
        public List<BuildRule> variantRules = new List<BuildRule>();
        /// <summary>
        /// 变体子文件夹名，文件夹名就是变体种类
        /// </summary>
        public string[] variantDirNames;
        
        /// <summary>
        /// 打包规则集合
        /// </summary>
        public List<BuildRule> rules = new List<BuildRule>();
        /// <summary>
        /// 分析后的打包资源集合
        /// </summary>
        [Header("Assets")] public List<RuleAsset> ruleAssets;
        /// <summary>
        /// 分析后的bundle集合
        /// </summary>
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
                EditorUtility.ClearProgressBar();
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
                if (EditorUtility.DisplayCancelableProgressBar($"收集资源{i}/{max}", rule.searchPath, i / (float) max))
                    break;
                
                ApplyRule(rule);
            }

            variantDirNames = GetVariantDirNames();
            foreach (var v in variantDirNames)
            {
                for (int i = 0, max = variantRules.Count; i < max; i++)
                {
                    var rule = variantRules[i];
                    if (EditorUtility.DisplayCancelableProgressBar($"收集变体资源{i}/{max}", rule.searchPath, i / (float) max))
                        break;
                    
                    rule.searchPath = Path.Combine(variantRootPath, v);
                    ApplyRule(rule);
                }
            }
        }

        /// <summary>
        /// 分析资源
        /// </summary>
        private void AnalysisAssets()
        {
            // 1.检查标记为依赖收集的资源是否被主资源依赖,剔除未被引用的依赖资源
            var removeDependList = new List<string>();
            var allCollectAssets = _collectedRuleAsset.Values.ToList();
            foreach (var item in _collectedRuleAsset)
            {
                if (item.Value.collectorType == CollectorType.DependAssetCollector)
                {
                    if (IsRemoveDependAsset(allCollectAssets, item.Key))
                        removeDependList.Add(item.Key);
                }
            }
            foreach (var item in removeDependList)
            {
                _collectedRuleAsset.Remove(item);
            }

            // 2.遍历所有bundle包，获取所有资源的依赖项，并记录tracker表里
            var getBundles = GetBundles();
            var tracker = new Dictionary<string, HashSet<string>>(); // asset-依赖bundle映射表
            int i = 0, max = getBundles.Count;
            foreach (var item in getBundles)
            {
                var bundle = item.Key;
                if (EditorUtility.DisplayCancelableProgressBar($"分析依赖{i}/{max}", bundle, i / (float) max)) 
                    break;
                
                var assetPaths = item.Value;
                // 场景资源不能和其他资源混合打包，主要是资源包结构不一样
                if (assetPaths.Exists(IsScene) && !assetPaths.TrueForAll(IsScene))
                    _conflicted.Add(bundle, assetPaths.ToArray());
                
                // 记录bundle依赖的资源
                var dependencies = AssetDatabase.GetDependencies(assetPaths.ToArray(), true);
                if (dependencies.Length > 0)
                {
                    foreach (var asset in dependencies)
                    {
                        if (IsValidAsset(asset) && !IsScene(asset))
                        {
                            // 记录依赖于该asset的所有bundle
                            if (!tracker.TryGetValue(asset, out var bundles))
                            {
                                bundles = new HashSet<string>();
                                tracker.Add(asset, bundles);
                            }
                            bundles.Add(bundle);
                        }
                    }
                }
                i++;
            }

            // 3.被多个bundle重复依赖的资源单独打包，并记录到_shared表里
            // 如果不单独打包，资源会被打进依赖它的bundle包里，造成冗余
            foreach (var map in tracker)
            {
                if (map.Value.Count > 1) //如果资源被两个以上bundle包含
                {
                    _collectedRuleAsset.TryGetValue(map.Key, out var ruleAsset);
                    if (ruleAsset == null) // 资源尚未被加进任何bundle
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
            // 1.剥离场景目录下的非场景资源，打成一个共享bundle
            int i = 0, max = _conflicted.Count;
            foreach (var item in _conflicted)
            {
                if (EditorUtility.DisplayCancelableProgressBar($"处理冲突{i}/{max}", item.Key, i / (float) max)) 
                    break;
                
                var list = item.Value;
                foreach (var asset in list)
                {
                    if (!IsScene(asset))
                        _shared.Add(asset, item.Key + "_ext");
                }
                i++;
            }

            // 2.处理所有依赖打包的资源，剥离出所有shader打成单独的包
            i = 0; max = _shared.Count;
            foreach (var item in _shared)
            {
                if (EditorUtility.DisplayCancelableProgressBar($"优化冗余{i}/{max}", item.Key, i / (float) max)) 
                    break;
                
                var asset = item.Key;
                var bundle = item.Value;
                if (asset.EndsWith(".shader"))
                {
                    // 所有被共享的shader资源打成一个bundle
                    CollectRuleAsset(asset, MakeBundleName("shaders", null));
                }
                else
                {
                    // 把依赖相同的资源打到同一个bundle
                    CollectRuleAsset(asset, MakeBundleName(bundle, GetVariantName(asset)));
                }

                i++;
            }
        }
        
        /// <summary>
        /// 创建资源列表
        /// </summary>
        private void MakeRuleAssetList()
        {
            ruleAssets = _collectedRuleAsset.Values.ToList();
            ruleAssets.Sort((a, b) => 
                string.Compare(a.assetName, b.assetName, StringComparison.Ordinal));
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
        /// 是否是需要被移除的依赖打包资源
        /// </summary>
        /// <param name="allCollectAssets">收集的所有打包资源</param>
        /// <param name="dependAssetPath">依赖收集的资源路径</param>
        /// <returns></returns>
        private static bool IsRemoveDependAsset(List<RuleAsset> allCollectAssets, string dependAssetPath)
        {
            foreach (var collectAsset in allCollectAssets)
            {
                if (collectAsset.collectorType != CollectorType.MainAssetCollector || collectAsset.rawFile) continue;
                
                if (collectAsset.dependAssets.Contains(dependAssetPath))
                    return false;
            }

            Logger.I($"发现未被依赖的资源并自动移除 : {dependAssetPath}");
            return true;
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
                Logger.E($"Invalid asset path : {assetPath}");
                return false;
            }
            if (assetPath.Contains("/Gizmos/"))
            {
                Logger.W($"Cannot pack gizmos asset : {assetPath}");
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
        /// 获取bundle名-assets映射表
        /// </summary>
        /// <returns></returns>
        private Dictionary<string, List<string>> GetBundles()
        {
            var bundles = new Dictionary<string, List<string>>();
            foreach (var item in _collectedRuleAsset)
            {
                var bundle = item.Value.bundle;
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
                        var bundleName = MakeBundleName(rule.assetBundleName, GetVariantName(asset));
                        CollectRuleAsset(asset, bundleName, rule.collectorType, GetVariantName(asset));
                        RecordBundleRule(bundleName, rule);
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
                        var bundleName = MakeBundleName(assetPath, variant);
                        CollectRuleAsset(asset, bundleName, rule.collectorType, variant);
                        RecordBundleRule(bundleName, rule);
                    }

                    break;
                }
                case PackRule.PackByDirectory:
                {
                    foreach (var asset in assets)
                    {
                        var dir = Path.GetDirectoryName(asset) ?? string.Empty;
                        var variant = GetVariantName(asset);
                        // 保证变体bundle名一致
                        if (!string.IsNullOrEmpty(variant))
                        {
                            dir = dir.Replace($"/{variant}", "");
                        }
                        var bundleName = MakeBundleName(dir, variant);
                        CollectRuleAsset(asset, bundleName, rule.collectorType, variant);
                        RecordBundleRule(bundleName, rule);
                    }

                    break;
                }
                case PackRule.PackByTopDirectory:
                {
                    var rs =  PathManager.GetRegularPath(rule.GetSearchPath());
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
                        var variant = GetVariantName(dir);
                        var bundleName = MakeBundleName(dir, variant);
                        CollectRuleAsset(asset, bundleName, rule.collectorType, variant);
                        RecordBundleRule(bundleName, rule);
                    }

                    break;
                }
                case PackRule.PackRawFile:
                    foreach (var asset in assets)
                    {
                        // 注意：原生文件只支持无依赖关系的资源
                        var depends = AssetDatabase.GetDependencies(asset, true);
                        if (depends.Length != 1)
                            throw new Exception("RawFile cannot depend on other assets");
                        
                        var bundleName = MakeBundleName(asset, null);
                        CollectRuleAsset(asset, bundleName, rule.collectorType, null, true);
                        RecordBundleRule(bundleName, rule);
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void CollectRuleAsset(string assetPath, string bundleName, 
            CollectorType type = CollectorType.MainAssetCollector, string variant = null, bool rawFile = false)
        {
            var ruleAsset = new RuleAsset
            {
                assetName = assetPath,
                bundle = bundleName,
                guid = AssetDatabase.AssetPathToGUID(assetPath),
                resourceVariant = variant ?? string.Empty,
                rawFile = rawFile,
                dependAssets = GetDependencies(assetPath),
                collectorType = type
            };
            _collectedRuleAsset.Add(assetPath, ruleAsset);
        }

        private List<string> GetDependencies(string mainAssetPath)
        {
            var dependencies = AssetDatabase.GetDependencies(mainAssetPath);
            return dependencies.Where(dep => IsValidAsset(dep) && dep != mainAssetPath).ToList();
        }

        private void Clear()
        {
            _bundlesRule.Clear();
            _shared.Clear();
            _conflicted.Clear();
            _collectedRuleAsset.Clear();
        }

        private void Save()
        {
            EditorUtility.ClearProgressBar();
            EditorUtility.SetDirty(this);
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
            if (string.IsNullOrEmpty(variantRootPath))
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