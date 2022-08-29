using System;
using System.Collections;
using System.Collections.Generic;
using Object = UnityEngine.Object;

namespace UAsset
{
    public class Asset : Loadable, IEnumerator
    {
        public static readonly Dictionary<string, Asset> Cache = new Dictionary<string, Asset>();

        public ResLoadCompleteCallBack completed;

        public static Func<string, Type, Asset> Creator { get; set; } = BundledAsset.Create;

        public Object asset { get; protected set; }

        public Object[] subAssets { get; protected set; }

        protected Type type { get; set; }

        protected bool isSubAssets { get; set; }

        public bool MoveNext()
        {
            return !isDone;
        }

        public void Reset()
        {
        }

        public object Current => null;

        private static Asset CreateInstance(string path, Type type)
        {
            if (string.IsNullOrEmpty(path)) throw new ArgumentException(nameof(path));

            return Creator(path, type);
        }

        protected void OnLoaded(Object target)
        {
            asset = target;
            Finish(asset == null ? "asset == null" : null);
        }

        public T Get<T>() where T : Object
        {
            return asset as T;
        }

        protected override void OnComplete()
        {
            if (completed == null) return;

            var saved = completed;
            var isSuccessLoaded = status == LoadableStatus.SuccessToLoad;
            completed?.Invoke(isSuccessLoaded, pathOrURL, asset);

            completed -= saved;
        }

        protected override void OnUnused()
        {
            completed = null;
        }

        protected override void OnUnload()
        {
            Cache.Remove(pathOrURL);
        }

        public static Asset LoadAsync(string path, Type type, ResLoadCompleteCallBack completed = null)
        {
            return LoadInternal(path, type, completed);
        }

        public static Asset Load(string path, Type type, ResLoadCompleteCallBack completed = null)
        {
            var asset = LoadInternal(path, type, completed);
            asset.LoadImmediate();
            return asset;
        }

        public static Asset LoadWithSubAssets(string path, Type type)
        {
            var asset = LoadInternal(path, type);
            if (asset == null)
                return null;
            asset.isSubAssets = true;
            asset.LoadImmediate();
            return asset;
        }

        public static Asset LoadWithSubAssetsAsync(string path, Type type)
        {
            var asset = LoadInternal(path, type);
            if (asset == null)
                return null;
            asset.isSubAssets = true;
            return asset;
        }

        private static Asset LoadInternal(string path, Type type,
            ResLoadCompleteCallBack completed = null)
        {
            // TODO: 现在业务层传过来的是短路径，暂时先由外面直接传全路径
            // PathManager.GetActualPath(ref path);
            // if (!Versions.Contains(path))
            // {
            //     Logger.E("FileNotFoundException {0}", path);
            //     return null;
            // }
            
            path = PathManager.GetAssetPath(path);
            if (!Cache.TryGetValue(path, out var item))
            {
                item = CreateInstance(path, type);
                Cache.Add(path, item);
            }

            if (completed != null) item.completed += completed;

            item.Load();
            return item;
        }
    }
}