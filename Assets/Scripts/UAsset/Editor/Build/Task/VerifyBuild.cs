using System.Collections.Generic;
using System.IO;

namespace xasset.editor
{
    // 用于修复Unity增量打包时，bundle名变化，间接引用的bundle不更新，导致的引用关系错误
    // 具体思路为加载上个版本的清单文件，检查是否有bundle被删除，如果被删除，则把依赖于该bundle的所有bundle删除
    public class VerifyBuild : BuildTaskJob
    {
        private HashSet<string> _newBundles = new HashSet<string>();
        private HashSet<int> _removeBundles = new HashSet<int>();

        public VerifyBuild(BuildTask task) : base(task)
        {
        }
        
        protected override void DoTask()
        {
            var versions = BuildVersions.Load(GetBuildPath(Versions.Filename));
            // 第一次打包，跳过该步骤
            if (versions.internalResourceVersion == 0)
            {
                return;
            }
            
            // 记录当前构建生成bundle名
            var ruleBundles = _task.buildRules.ruleBundles;
            foreach (var bundle in ruleBundles)
            {
                _newBundles.Add(bundle.name.ToLower());
            }
            
            // 加载上次构建的清单文件
            var version = versions.Get(_task.name);
            var manifest = Manifest.LoadFromFile(GetBuildPath(version?.file));
            var bundles = manifest.bundles;
            // 记录被删除的bundle
            foreach (var bundle in bundles)
            {
                if (bundle.isRaw) continue;

                var name = bundle.name;
                if (bundle.IsVariant)
                {
                    name = name.Replace($".{bundle.variant}", "");
                }
                
                if (!_newBundles.Contains(name))
                {
                    _removeBundles.Add(bundle.id);
                }
            }

            // 添加依赖于被删除项的bundle到删除列表
            foreach (var bundle in bundles)
            {
                if (bundle.isRaw) continue;
                
                foreach (var dep in bundle.deps)
                {
                    if (_removeBundles.Contains(dep))
                    {
                        _removeBundles.Add(bundle.id);
                        break;
                    }
                }
            }
            
            // 移除bundle的清单
            foreach (var id in _removeBundles)
            {
                var bundle = manifest.GetBundle(id);
                var bundleManifestPath = $"{GetBuildPath(bundle.name)}.manifest";
                if (File.Exists(bundleManifestPath))
                {
                    File.Delete(bundleManifestPath);
                }
            }
        }
    }
}