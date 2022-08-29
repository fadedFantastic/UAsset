using UnityEditor;

namespace UAsset.Editor
{
    public class BuiltinBuildPipeline : ABuildPipeline
    {
        public override IAssetBundleManifest BuildAssetBundles(string outputPath, AssetBundleBuild[] builds,
            BuildAssetBundleOptions options, BuildTarget target)
        {
            var manifest = BuildPipeline.BuildAssetBundles(outputPath, builds, options,
                EditorUserBuildSettings.activeBuildTarget);
            return manifest != null ? new BuiltinAssetBundleManifest(manifest) : null;
        }
    }
}