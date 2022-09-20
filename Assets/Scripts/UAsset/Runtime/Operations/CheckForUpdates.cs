using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace UAsset
{
    /// <summary>
    ///     检查版本更新，先下载 versions.json 文件，同步后根据改文件记录的版本信息更新和下载清单文件。
    /// </summary>
    public sealed class CheckForUpdates : Operation
    {
        private readonly List<BuildVersion> _changes = new List<BuildVersion>();
        private UnityWebRequest _download;
        private string _savePath;

        /// <summary>
        ///     需要更新的大小
        /// </summary>
        public long downloadSize { get; private set; }

        public string url { get; set; }
        
        public BuildVersions remoteVersions { get; private set; }

        /// <summary>
        ///     下载更新的内容。
        /// </summary>
        /// <returns></returns>
        public DownloadFiles DownloadAsync()
        {
            var infos = _changes.ConvertAll(input => Versions.GetDownloadInfo(input.file, input.hash, input.size));
            var downloadFiles = Versions.DownloadAsync(infos.ToArray());
            downloadFiles.completed = operation => { ApplyChanges(); };
            return downloadFiles;
        }

        /// <summary>
        ///     加载下载后的清单文件。
        /// </summary>
        private void ApplyChanges()
        {
            foreach (var version in _changes)
            {
                // 加载没有加载且已经下载到本地的版本文件。
                if (Versions.Changed(version) && Versions.Exist(version))
                {
                    Versions.LoadManifest(version);
                }
            }
            _changes.Clear();
        }

        /// <summary>
        ///     保存版本文件到下载目录并更新
        /// </summary>
        public void SaveAndUpdateVersion()
        {
            // ------------------->更新变体版本号<-------------------
            var severVersion = BuildVersions.Load(_savePath);
            var localVersion = BuildVersions.Load(Downloader.GetDownloadDataPath(Versions.Filename));
            // 首次安装时，下载目录暂不存在本地版本文件
            var variantVersion = localVersion.variantVersion ?? severVersion.variantVersion;
            var variantTypes = severVersion.variantTypes;
            for (var i = 0; i < variantTypes.Length; ++i)
            {
                if (variantTypes[i] == Versions.CurrentVariant)
                {
                    variantVersion[i] = severVersion.internalResourceVersion;
                    break;
                }
            }
            severVersion.variantVersion = variantVersion;
            File.WriteAllText(_savePath, JsonUtility.ToJson(severVersion, true));

            // ------------------->拷贝服务器版本文件到下载目录<-------------------
            File.Copy(_savePath, Downloader.GetDownloadDataPath(Versions.Filename), true);
            Versions.LoadRemoteVersions(remoteVersions);
        }

        public override void Start()
        {
            base.Start();
            if (Versions.OfflineMode)
            {
                Finish();
                return;
            }

            if (string.IsNullOrEmpty(url))
            {
                url = Downloader.GetDownloadURL(Versions.Filename);
            }

            _savePath = PathManager.GetTemporaryPath(Versions.Filename);
            if (File.Exists(_savePath))
            {
                File.Delete(_savePath);
            }
            
            _download = UnityWebRequest.Get(url);
            _download.downloadHandler = new DownloadHandlerFile(_savePath);
            _download.SendWebRequest();
        }

        protected override void Update()
        {
            if (status != OperationStatus.Processing) return;

            if (!_download.isDone) return;

            if (string.IsNullOrEmpty(_download.error))
            {
                downloadSize = 0;
                remoteVersions = BuildVersions.Load(_savePath);
                foreach (var item in remoteVersions.data)
                {
                    if (Versions.Exist(item))
                    {
                        // 检查下是否已经加载最新的清单文件
                        if (Versions.Changed(item))
                        {
                            Versions.LoadManifest(item);   
                        }
                        continue;
                    }

                    downloadSize += item.size;
                    _changes.Add(item);
                }

                Finish();
            }
            else
            {
                Finish(_download.error);
            }
        }
    }
}