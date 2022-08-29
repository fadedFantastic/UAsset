using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine.Networking;

namespace UAsset
{
    public class RawAsset : Loadable
    {
        public static readonly Dictionary<string, RawAsset> Cache = new Dictionary<string, RawAsset>();
        private ManifestBundle _info;
        private UnityWebRequest _request;

        public delegate void LoadRawAssetComplete(string binaryAssetName, byte[] binaryBytes);
        public LoadRawAssetComplete completed;
        public string savePath { get; private set; }
        public byte[] binaryBytes { get; protected set; }
        public static Func<string, RawAsset> Creator { get; set; } = path => new RawAsset { pathOrURL = path };

        public override void LoadImmediate()
        {
            if (isDone)
            {
                LoadFinish();
                return;
            }
            while (!_request.isDone)
            {
            }

            LoadFinish(_request.error);
        }

        private static RawAsset CreateInstance(string path)
        {
            if (string.IsNullOrEmpty(path)) throw new ArgumentException(nameof(path));

            return Creator(path);
        }

        protected override void OnLoad()
        {
            _info = Versions.GetBundle(pathOrURL);
            if (_info == null)
            {
                Finish("File not found.");
                return;
            }

            savePath = Downloader.GetDownloadDataPath(_info.nameWithAppendHash);
            var file = new FileInfo(savePath);
            if (file.Exists)
            {
                if (file.Length == _info.size)
                {
                    LoadFinish();
                    return;
                }
                File.Delete(savePath);
            }

            var url = PathManager.GetBundlePathOrURL(_info);
            _request = UnityWebRequest.Get(url);
            _request.downloadHandler = new DownloadHandlerFile(savePath);
            _request.SendWebRequest();
            status = LoadableStatus.Loading;
        }

        protected override void OnUnload()
        {
            if (_request != null)
            {
                _request.Dispose();
                _request = null;
            }

            binaryBytes = null;
            Cache.Remove(pathOrURL);
        }

        protected override void OnComplete()
        {
            if (completed == null)
            {
                return;
            }
            
            var saved = completed;
            completed?.Invoke(pathOrURL, binaryBytes);
            completed -= saved;
        }

        protected override void OnUpdate()
        {
            if (status != LoadableStatus.Loading)
            {
                return;
            }
            UpdateLoading();
        }

        protected override void OnUnused()
        {
            completed = null;
        }

        private void UpdateLoading()
        {
            if (_request == null)
            {
                Finish("request == null");
                return;
            }

            if (!_request.isDone)
            {
                return;
            }

            LoadFinish(_request.error);
        }

        public static RawAsset LoadAsync(string filename, LoadRawAssetComplete completed = null)
        {
            return LoadInternal(filename, false, completed);
        }

        public static RawAsset Load(string filename, LoadRawAssetComplete completed = null)
        {
            return LoadInternal(filename, true, completed);
        }

        private static RawAsset LoadInternal(string filename, bool mustCompleteOnNextFrame = false, 
            LoadRawAssetComplete completed = null)
        {
            // TODO: 现在业务层传过来的是短路径，暂时先由外面直接传全路径
            // PathManager.GetActualPath(ref filename);
            // if (!Versions.Contains(filename))
            // {
            //     throw new FileLoadException(filename);
            // }
            if (!Cache.TryGetValue(filename, out var asset))
            {
                asset = CreateInstance(filename);
                Cache.Add(filename, asset);
            }
            
            if (completed != null) asset.completed += completed;
            
            asset.Load();
            if (mustCompleteOnNextFrame)
            {
                asset.LoadImmediate();
            }
            return asset;
        }
        
        private void LoadFinish(string errorCode = null)
        {
            if (string.IsNullOrEmpty(errorCode))
            {
                var filePath = Downloader.GetDownloadDataPath(_info.nameWithAppendHash);
                binaryBytes = File.ReadAllBytes(filePath);
                
                if (Versions.EncryptionEnabled)
                {
                    Utility.DeEncryptBinaryFile(binaryBytes); 
                }
            }
            Finish(errorCode);
        }
        
        public static string GetBinaryPath(string filename)
        {
            if (Versions.SimulationMode)
            {
                return $"{UnityEngine.Application.dataPath}/../{filename}";
            }
            
            var bundle = Versions.GetBundle(filename);
            return bundle == null ? null : PathManager.GetBundlePathOrURL(bundle);   
        }
    }
}