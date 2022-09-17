using System;
using System.Collections.Generic;

namespace UAsset
{
    public class ResLoader
    {
        private struct Res
        {
            public readonly string path;
            public readonly Type type;
            public readonly bool rapid;

            public Res(string path, Type type, bool rapid)
            {
                this.path = path;
                this.type = type;
                this.rapid = rapid;
            }
        }
        
        private static Stack<ResLoader> _loaders = new Stack<ResLoader>();
        
        private Dictionary<string, Asset> _assetCache = new Dictionary<string, Asset>();
        private Dictionary<string, Res> _waitingRes = new Dictionary<string, Res>();
        private Dictionary<string, List<Action<Asset>>> _completeCallbacks =
            new Dictionary<string, List<Action<Asset>>>();

        private Action onAllFinish;

        public static ResLoader Alloc()
        {
            return _loaders.Count > 0 ? _loaders.Pop() : new ResLoader();
        }

        /// <summary>
        /// loader添加一个等待加载的资源
        /// </summary>
        /// <param name="name">资源名</param>
        /// <param name="type">资源类型</param>
        /// <param name="onComplete">完成后回调方法</param>
        public ResLoader Add2Load(string name, Type type, Action<Asset> onComplete = null)
        {
            return Add2Waiting(name, type, false, onComplete);
        }

        /// <summary>
        /// loader添加一个等待加载的资源(同步模式)
        /// </summary>
        /// <param name="name">资源名</param>
        /// <param name="type">资源类型</param>
        /// <param name="onComplete">完成后回调方法</param>
        public ResLoader Add2LoadRapid(string name, Type type, Action<Asset> onComplete = null)
        {
            return Add2Waiting(name, type, true, onComplete);
        }

        private ResLoader Add2Waiting(string name, Type type, bool isRapid, Action<Asset> listener = null)
        {
            if (!_waitingRes.ContainsKey(name))
            {
                var res = new Res(name, type, isRapid);
                _waitingRes[name] = res;
                if (listener != null)
                {
                    if (!_completeCallbacks.ContainsKey(name))
                    {
                        _completeCallbacks[name] = new List<Action<Asset>>();
                    }
                    _completeCallbacks[name].Add(listener);   
                }
            }
            return this;
        }
        
        /// <summary>
        /// 加载
        /// </summary>
        /// <param name="onFinish">全部资源加载结束回调</param>
        public void Load(Action onFinish = null)
        {
            onAllFinish = onFinish;

            foreach (var res in _waitingRes)
            {
                var resInfo = res.Value;
                var path = resInfo.path;
                Asset asset;
                if (resInfo.rapid)
                {
                    asset = Asset.Load(path, resInfo.type);
                    OnLoadOneComplete(asset);
                }
                else
                {
                    asset = Asset.LoadAsync(path, resInfo.type, OnLoadOneComplete);
                }
                _assetCache[path] = asset;
            }
            _waitingRes.Clear();
        }

        private void OnLoadOneComplete(Asset asset)
        {
            var assetName = asset.pathOrURL;
            if (_completeCallbacks.ContainsKey(assetName))
            {
                foreach (var listener in _completeCallbacks[assetName])
                {
                    listener?.Invoke(asset);
                }
                _completeCallbacks.Remove(assetName);
            }

            var allDone = true;
            foreach (var cache in _assetCache)
            {
                allDone = allDone && cache.Value.isDone;
            }

            if (allDone)
            {
                onAllFinish?.Invoke();
                onAllFinish = null;
            }
        }
        
        public T LoadAssetImmediate<T>(string assetName) where T : UnityEngine.Object
        {
            if (_assetCache.ContainsKey(assetName))
            {
                return _assetCache[assetName].Get<T>();
            }

            var asset = Asset.Load(assetName, typeof(T));
            _assetCache.Add(assetName, asset);
            return asset.Get<T>();
        }

        /// <summary>
        /// loader取消对某个资源的引用
        /// </summary>
        public void ReleaseRes(string name)
        {
            if (_assetCache.TryGetValue(name, out var asset))
            {
                asset.Release();
                _assetCache.Remove(name);
            }
            else if (_waitingRes.ContainsKey(name))
            {
                _waitingRes.Remove(name);
            }
        }

        /// <summary>
        /// loader取消对所有资源的引用
        /// </summary>
        private void ReleaseAllRes()
        {
            foreach (var asset in _assetCache)
            {
                asset.Value.Release();
            }
            _assetCache.Clear();
        }
        
        private void Dispose()
        {
            ReleaseAllRes();
            _waitingRes.Clear();
            _completeCallbacks.Clear();
            onAllFinish = null;
        }

        public void Recycle2Cache()
        {
            Dispose();
            _loaders.Push(this);
        }

        public static void Clear()
        {
            while (_loaders.Count > 0)
            {
                _loaders.Pop().Dispose();
            }
        }
    }
}