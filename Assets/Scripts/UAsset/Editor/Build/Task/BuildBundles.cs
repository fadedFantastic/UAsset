using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;

namespace xasset.editor
{
    public class BuildBundles : BuildTaskJob
    {
        private readonly BuildAssetBundleOptions _options;
        public readonly List<ManifestBundle> bundles = new List<ManifestBundle>();
        public readonly List<RuleBundle> rawBundles = new List<RuleBundle>();

        public BuildBundles(BuildTask task, BuildAssetBundleOptions options) : base(task)
        {
            _options = options;
        }

        private ABuildPipeline BuildPipeline => customBuildPipeline != null
            ? customBuildPipeline.Invoke(this)
            : new BuiltinBuildPipeline();

        public static Func<BuildBundles, ABuildPipeline> customBuildPipeline { get; set; } = null;

        protected override void DoTask()
        {
            CreateBundles();
            if (bundles.Count > 0)
            {
                if (!BuildAssetBundles())
                {
                    return;
                }
            }
            BuildRawAssetsToBundles();
            _task.bundles.AddRange(bundles);
        }

        /// <summary>
        /// 处理二进制资源
        /// </summary>
        private void BuildRawAssetsToBundles()
        {
            if (rawBundles.Count == 0)
            {
                return;
            }

            var bundleStartId = bundles.Count;
            for (var index = 0; index < rawBundles.Count; index++)
            {
                var bundle = rawBundles[index];
                foreach (var filePath in bundle.assets)
                {
                    if (string.IsNullOrEmpty(filePath))
                    {
                        Logger.E("RawAsset not found:{0}", filePath);
                        continue;
                    }
                    
                    var file = new FileInfo(filePath);
                    var fileExtension = file.Extension;
                    var hash = Utility.ComputeHash(filePath);
                    var bundleName = bundle.name.ToLower();
                    var manifestBundle = new ManifestBundle
                    {
                        id = bundleStartId + index,
                        hash = hash,
                        name = bundleName,
                        nameWithAppendHash = $"{bundleName}_{hash}{fileExtension}",
                        isRaw = true,
                        assets = bundle.assets.ToList(),
                        size = file.Length,
                        copyToPackage = bundle.packed,
                        tag = bundle.tag
                    };
                    
                    var path = GetBuildPath(manifestBundle.nameWithAppendHash);
                    Utility.CreateFileDirectory(path);

                    if (!File.Exists(path))
                    {
                        file.CopyTo(path);
                    }
                    bundles.Add(manifestBundle);   
                }
                
                UnityEditor.EditorUtility.DisplayProgressBar("Create RawAsset Bundle", bundle.name,
                    (index + 1) / (float)rawBundles.Count);
            }
            UnityEditor.EditorUtility.ClearProgressBar();
        }

        protected AssetBundleBuild[] GetBuilds()
        {
            return bundles.ConvertAll(bundle =>
                new AssetBundleBuild
                {
                    assetNames = bundle.assets.ToArray(),
                    assetBundleName = bundle.name,
                    assetBundleVariant = bundle.variant
                }).ToArray();
        }

        private bool BuildAssetBundles()
        {
            var builds = GetBuilds();
            var manifest = BuildPipeline.BuildAssetBundles(_task.outputPath, builds, _options,
                EditorUserBuildSettings.activeBuildTarget);
            if (manifest == null)
            {
                TreatError($"Failed to build AssetBundles with {_task.name}.");
                return false;
            }

            var nameWithBundles = GetBundles();
            return BuildWithoutEncryption(nameWithBundles, manifest);
        }

        private bool BuildWithoutEncryption(IReadOnlyDictionary<string, ManifestBundle> nameWithBundles,
            IAssetBundleManifest manifest)
        {
            var assetBundles = manifest.GetAllAssetBundles();
            for(var index = 0; index < assetBundles.Length; index++)
            {
                var assetBundle = assetBundles[index];
                if (nameWithBundles.TryGetValue(assetBundle, out var bundle))
                {
                    var path = GetBuildPath(assetBundle);
                    string hash = Utility.ComputeHash(path);
                    var nameWithAppendHash = $"{assetBundle}_{hash}{Settings.BundleExtension}";
                    bundle.hash = hash;
                    bundle.deps = Array.ConvertAll(manifest.GetAllDependencies(assetBundle),
                        input => nameWithBundles[input].id);
                    bundle.nameWithAppendHash = nameWithAppendHash;
                    var dir = Path.GetDirectoryName(path);
                    var newPath = $"{dir}/{Path.GetFileName(nameWithAppendHash)}";
                    var info = new FileInfo(path);
                    if (info.Exists)
                    {
                        bundle.size = info.Length;
                    }
                    else
                    {
                        TreatError($"File not found: {info}");
                        return false;
                    }

                    if (!File.Exists(newPath)) info.CopyTo(newPath, true);
                    
                    UnityEditor.EditorUtility.DisplayProgressBar("Create Bundle With Hash", bundle.name,
                        (index + 1) / (float)assetBundles.Length);
                }
                else
                {
                    TreatError($"Bundle not found: {assetBundle}");
                    UnityEditor.EditorUtility.ClearProgressBar();
                    return false;
                }
            }
            
            UnityEditor.EditorUtility.ClearProgressBar();
            return true;
        }

        private Dictionary<string, ManifestBundle> GetBundles()
        {
            var nameWithBundles = new Dictionary<string, ManifestBundle>();

            for (var i = 0; i < bundles.Count; i++)
            {
                var bundle = bundles[i];
                var bundleName = bundle.IsVariant ? $"{bundle.name}.{bundle.variant}" : bundle.name;
                bundle.id = i;
                bundle.name = bundleName;
                nameWithBundles[bundleName] = bundle;
            }

            return nameWithBundles;
        }

        private void CreateBundles()
        {
            var rules = _task.buildRules;
            var ruleBundles = rules.ruleBundles;

            foreach (var bundle in ruleBundles)
            {
                if (bundle.IsLoadFromBinary)
                {
                    rawBundles.Add(bundle);  
                }
                else
                {
                    var manifestBundle = new ManifestBundle
                    {
                        name = bundle.name.ToLower(),
                        assets = bundle.assets.ToList(),
                        variant = bundle.variant,
                        copyToPackage = bundle.packed,
                        tag = bundle.tag
                    };
                    bundles.Add(manifestBundle);
                }
            }
        }
    }
}