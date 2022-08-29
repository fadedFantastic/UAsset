using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace UAsset
{
    [DisallowMultipleComponent]
    public class DownloadWhilePlay : MonoBehaviour
    {
        /// <summary>
        ///     完整包标识文件名
        /// </summary>
        private const string kFullPackageFlagFileName = "FullPackage";
        
        /// <summary>
        ///     批量下载文件对象
        /// </summary>
        private DownloadFiles _downloadFiles;

        /// <summary>
        ///     完成后的回调
        /// </summary>
        public Action onComplete;
        
        /// <summary>
        ///     开始下载
        /// </summary>
        /// <param name="operation">获取下载信息操作</param>
        public void BeginDownload(GetDownloadSize operation)
        {
            if (operation == null)
            {
                throw new ArgumentNullException(nameof(operation), "operation cannot be null");
            }

            if (_downloadFiles != null)
            {
                _downloadFiles.Cancel();
                _downloadFiles = null;   
            }

            if (operation.downloadSize > 0)
            {
                StartCoroutine(BeginDownloadRoutine(operation));
            }
            else
            {
                OnDownloadComplete();
            }
        }
        
        /// <summary>
        ///     开始边玩边下
        /// </summary>
        public IEnumerator BeginDownloadRoutine(GetDownloadSize operation)
        {
            _downloadFiles = operation.DownloadAsync();
            yield return _downloadFiles;

            if (_downloadFiles.status == OperationStatus.Success)
            {
                OnDownloadComplete();
            }
        }
        
        /// <summary>
        ///     暂停下载
        /// </summary>
        public void Pause()
        {
            _downloadFiles?.PauseAll();
        }

        /// <summary>
        ///     恢复下载
        /// </summary>
        public void UnPause()
        {
            _downloadFiles?.UnPauseAll();
        }

        /// <summary>
        ///     是否正在下载
        /// </summary>
        /// <returns></returns>
        public bool IsDownloading()
        {
            return _downloadFiles.IsDownloading();
        }

        /// <summary>
        ///     取消下载
        /// </summary>
        public void Cancel()
        {
            _downloadFiles?.Cancel();
        }

        /// <summary>
        ///     注册下载更新回调
        /// </summary>
        /// <param name="onUpdate"></param>
        public void RegisterDownloadUpdateCallback(Action<long, long, long> onUpdate)
        {
            if (_downloadFiles != null)
            {
                _downloadFiles.updated += onUpdate;
            }
        }

        /// <summary>
        ///     移除下载更新回调
        /// </summary>
        /// <param name="onUpdate"></param>
        public void RemoveDownloadUpdateCallback(Action<long, long, long> onUpdate)
        {
            if (_downloadFiles != null)
            {
                _downloadFiles.updated -= onUpdate;
            }
        }
        
        /// <summary>
        /// 获取下载大小信息
        /// </summary>
        /// <param name="downloadedSize">已下载</param>
        /// <param name="totalSize">总大小</param>
        public void GetDownloadSize(out long downloadedSize, out long totalSize)
        {
            downloadedSize = _downloadFiles.downloadedBytes;
            totalSize = _downloadFiles.totalSize;
        }

        /// <summary>
        ///     当前是否为完整客户端（边玩边下已完成）
        /// </summary>
        public static bool IsFullPackageClient()
        {
            if (Versions.OfflineMode) return true;
            
            return File.Exists(Downloader.GetDownloadDataPath(kFullPackageFlagFileName));
        }

        /// <summary>
        ///     创建完整包标识文件
        /// </summary>
        private void CreateFullPackageFlagFile()
        {
            var filePath = Downloader.GetDownloadDataPath(kFullPackageFlagFileName);
            if (!File.Exists(filePath))
            {
                File.Create(filePath);
            }
        }
        
        /// <summary>
        ///     下载完成
        /// </summary>
        private void OnDownloadComplete()
        {
            CreateFullPackageFlagFile();
            onComplete?.Invoke();
            onComplete = null;
        }
    }
}