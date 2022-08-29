namespace UAsset.Editor
{
    public interface IAssetBundleManifest
    {
        string[] GetAllAssetBundles();
        string[] GetAllDependencies(string assetBundle);
    }
}