using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Mime;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace UAsset
{
    public class RawAsset : Loadable
    {
        public static readonly Dictionary<string, RawAsset> Cache = new Dictionary<string, RawAsset>();
        private ManifestBundle _info;
        private UnityWebRequest _request;
        protected byte[] _binaryBytes;

        public delegate void LoadRawAssetComplete(RawAsset asset);
        public LoadRawAssetComplete completed;
        public byte[] binaryBytes => _binaryBytes;
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

            if (!_info.isRaw)
            {
                Finish("Cannot load asset bundle file using RawAsset");
                return;
            }

            if (Versions.IsDownloaded(_info))
            {
                LoadFinish();
                return;
            }

            var savePath = Downloader.GetDownloadDataPath(_info.nameWithAppendHash);
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

            _binaryBytes = null;
            Cache.Remove(pathOrURL);
        }

        protected override void OnComplete()
        {
            if (completed == null)
            {
                return;
            }
            
            var saved = completed;
            completed?.Invoke(this);
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

        public static RawAsset Load(string filename)
        {
            return LoadInternal(filename, true);
        }

        private static RawAsset LoadInternal(string filename, bool mustCompleteOnNextFrame = false, 
            LoadRawAssetComplete completed = null)
        {
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
                var filePath = PathManager.GetBundlePathOrURL(_info);
                if (Versions.IsStreamingAsset(_info.nameWithAppendHash))
                {
#if UNITY_ANDROID && !UNITY_EDITOR
                    _binaryBytes = new byte[_info.size];
                    AndroidNativeHelper.ReadAllBytes(filePath, ref _binaryBytes);
#else
                    _binaryBytes = File.ReadAllBytes(filePath);
#endif
                }
                else
                {
                    _binaryBytes = File.ReadAllBytes(filePath);
                }

                if (Versions.EncryptionEnabled)
                {
                    Utility.DeEncryptBinaryFile(_binaryBytes); 
                }
            }
            Finish(errorCode);
        }

        public string GetFileText()
        {
            if (_binaryBytes == null || _binaryBytes.Length == 0)
            {
                return string.Empty;
            }
            
            return Encoding.Default.GetString(_binaryBytes); 
        }
    }
}