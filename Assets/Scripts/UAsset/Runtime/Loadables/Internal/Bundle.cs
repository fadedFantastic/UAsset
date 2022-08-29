using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace UAsset
{
    public class Bundle : Loadable
    {
        public static readonly Dictionary<string, Bundle> Cache = new Dictionary<string, Bundle>();

        protected ManifestBundle info;

        private Stream _stream;
        
        public static Func<string, ManifestBundle, Bundle> customLoader { get; set; } = null;

        public AssetBundle assetBundle { get; protected set; }

        protected AssetBundleCreateRequest LoadAssetBundleAsync(string url)
        {
            Logger.I("LoadAssetBundleAsync", info.nameWithAppendHash);

            if (Versions.EncryptionEnabled)
            {
                TryCreateFileStream(url);
                return AssetBundle.LoadFromStreamAsync(_stream);
            }
            
            return AssetBundle.LoadFromFileAsync(url);  
        }

        protected AssetBundle LoadAssetBundle(string url)
        {
            Logger.I("LoadAssetBundle", info.nameWithAppendHash);
            
            if (Versions.EncryptionEnabled)
            {
                TryCreateFileStream(url);
                return AssetBundle.LoadFromStream(_stream);
            }
            
            return AssetBundle.LoadFromFile(url);
        }
        
        private void TryCreateFileStream(string filepath)
        {
            if (_stream == null)
            {
                Stream fileStream;
#if UNITY_ANDROID && !UNITY_EDITOR
                if (Versions.IsStreamingAsset(info.nameWithAppendHash))
                {
                    fileStream = new AndroidFileStream(filepath);
                }
                else
                {
                    fileStream = File.OpenRead(filepath);
                }
#else
                fileStream = File.OpenRead(filepath);
#endif

                var uniqueSalt = Encoding.UTF8.GetBytes(info.name);
                _stream = new SeekableAesStream(fileStream, Utility.EncryptKey, uniqueSalt);   
            }
        }

        protected void OnLoaded(AssetBundle bundle)
        {
            assetBundle = bundle;
            Finish(assetBundle == null ? "assetBundle == null" : null);
        }

        internal static Bundle LoadInternal(ManifestBundle bundle)
        {
            if (bundle == null) throw new NullReferenceException();

            if (!Cache.TryGetValue(bundle.nameWithAppendHash, out var item))
            {
                var url = PathManager.GetBundlePathOrURL(bundle);
                if (customLoader != null) item = customLoader(url, bundle);

                if (item == null)
                {
                    if (url.StartsWith("http://") || url.StartsWith("https://") || url.StartsWith("ftp://"))
                        item = new DownloadBundle {pathOrURL = url, info = bundle};
                    else
                        item = new LocalBundle {pathOrURL = url, info = bundle};
                }

                Cache.Add(bundle.nameWithAppendHash, item);
            }

            item.Load();
            return item;
        }

        protected override void OnUnload()
        {
            Cache.Remove(info.nameWithAppendHash);
            if (assetBundle == null) return;

            assetBundle.Unload(true);
            assetBundle = null;
            
            _stream?.Dispose();
            _stream = null;
        }
    }
}