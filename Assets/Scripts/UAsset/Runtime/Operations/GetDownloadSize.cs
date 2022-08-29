using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace UAsset
{
    /// <summary>
    ///     异步获取下载大小。
    /// </summary>
    public sealed class GetDownloadSize : Operation
    {
        public readonly List<ManifestBundle> bundles = new List<ManifestBundle>();
        public readonly ConcurrentBag<DownloadInfo> changes = new ConcurrentBag<DownloadInfo>();
        private long _downloadSize;
        
        /// <summary>
        ///     需要下载的大小
        /// </summary>
        public long downloadSize => _downloadSize;

        public override void Start()
        {
            base.Start();

            if (Versions.OfflineMode)
            {
                Finish();
                return;
            }
            
            if (bundles.Count == 0)
            {
                Finish();
                return;
            }

            ParallelChecksum();
        }

        public DownloadFiles DownloadAsync()
        {
            var downloadFiles = Versions.DownloadAsync(changes.ToArray());
            return downloadFiles;
        }

        private void ParallelChecksum()
        {
            try
            {
                Parallel.For(0, bundles.Count, i =>
                {
                    var bundle = bundles[i];
                    // 过滤掉非当前设置的变体的bundle
                    if (bundle.IsVariant && bundle.variant != Versions.CurrentVariant) return;
                    
                    var info = Versions.GetDownloadInfo(bundle.nameWithAppendHash, bundle.hash, bundle.size);
                    if (!Versions.IsDownloaded(bundle) && !changes.Contains(info))
                    {
                        Interlocked.Add(ref _downloadSize, info.downloadSize);
                        changes.Add(info);
                    }
                });
                
                Finish();
            }
            catch (AggregateException e)
            {
                foreach (var ex in e.InnerExceptions)
                {
                    UnityEngine.Debug.LogException(ex);
                }
                throw;
            }
        }
    }
}