using UnityEditor;

namespace UAsset.Editor
{
    public abstract class ABuildPipeline
    {
        public abstract IAssetBundleManifest BuildAssetBundles(string outputPath, AssetBundleBuild[] builds,
            BuildAssetBundleOptions options, BuildTarget target);
    }
}