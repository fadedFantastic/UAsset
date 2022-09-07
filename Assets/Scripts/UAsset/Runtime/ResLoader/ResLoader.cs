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
        
        private static Stack<ResLoader> m_Loaders = new Stack<ResLoader>();
        
        private Dictionary<string, Asset> m_AssetCache = new Dictionary<string, Asset>();
        private Dictionary<string, Res> m_WaitingRes = new Dictionary<string, Res>();
        private Dictionary<string, List<Loadable.ResLoadCompleteCallBack>> m_CompleteCallbacks =
            new Dictionary<string, List<Loadable.ResLoadCompleteCallBack>>();

        private Action m_OnAllFinish;

        public static ResLoader Alloc()
        {
            return m_Loaders.Count > 0 ? m_Loaders.Pop() : new ResLoader();
        }

        /// <summary>
        /// loader添加一个等待加载的资源
        /// </summary>
        /// <param name="name">资源名</param>
        /// <param name="onComplete">完成后回调方法</param>
        public ResLoader Add2Load(string name, Loadable.ResLoadCompleteCallBack onComplete = null)
        {
            return Add2Load(name, typeof(UnityEngine.Object), onComplete);
        }

        /// <summary>
        /// loader添加一个等待加载的资源
        /// </summary>
        /// <param name="name">资源名</param>
        /// <param name="type">资源类型</param>
        /// <param name="onComplete">完成后回调方法</param>
        public ResLoader Add2Load(string name, Type type, Loadable.ResLoadCompleteCallBack onComplete = null)
        {
            return Add2Waiting(name, type, false, onComplete);
        }
        
        /// <summary>
        /// loader添加一个等待加载的资源(同步模式)
        /// </summary>
        /// <param name="name">资源名</param>
        /// <param name="onComplete">完成后回调方法</param>
        public ResLoader Add2LoadRapid(string name, Loadable.ResLoadCompleteCallBack onComplete = null)
        {
            return Add2LoadRapid(name, null, onComplete);
        }

        /// <summary>
        /// loader添加一个等待加载的资源
        /// </summary>
        /// <param name="name">资源名</param>
        /// <param name="type">资源类型</param>
        /// <param name="onComplete">完成后回调方法</param>
        public ResLoader Add2LoadRapid(string name, Type type, Loadable.ResLoadCompleteCallBack onComplete = null)
        {
            return Add2Waiting(name, type, true, onComplete);
        }

        private ResLoader Add2Waiting(string name, Type type, bool isRapid, Loadable.ResLoadCompleteCallBack listener = null)
        {
            if (!m_WaitingRes.ContainsKey(name))
            {
                var res = new Res(name, type, isRapid);
                m_WaitingRes[name] = res;
                if (listener != null)
                {
                    if (!m_CompleteCallbacks.ContainsKey(name))
                    {
                        m_CompleteCallbacks[name] = new List<Loadable.ResLoadCompleteCallBack>();
                    }
                    m_CompleteCallbacks[name].Add(listener);   
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
            m_OnAllFinish = onFinish;

            foreach (var res in m_WaitingRes)
            {
                var resInfo = res.Value;
                var path = resInfo.path;
                Asset asset;
                if (resInfo.rapid)
                {
                    asset = Asset.Load(path, resInfo.type, OnLoadOneComplete); 
                }
                else
                {
                    asset = Asset.LoadAsync(path, resInfo.type, OnLoadOneComplete);
                }
                m_AssetCache[path] = asset;
            }
            m_WaitingRes.Clear();
        }

        private void OnLoadOneComplete(bool success, string assetName, object asset)
        {
            if (m_CompleteCallbacks.ContainsKey(assetName))
            {
                foreach (var listener in m_CompleteCallbacks[assetName])
                {
                    listener?.Invoke(success, assetName, asset);
                }
                m_CompleteCallbacks.Remove(assetName);
            }

            bool allDone = true;
            foreach (var cache in m_AssetCache)
            {
                allDone = allDone && cache.Value.isDone;
            }

            if (allDone)
            {
                m_OnAllFinish?.Invoke();
                m_OnAllFinish = null;
            }
        }
        
        public T LoadAssetImmediate<T>(string assetName) where T : UnityEngine.Object
        {
            if (m_AssetCache.ContainsKey(assetName))
            {
                return m_AssetCache[assetName].Get<T>();
            }

            var asset = Asset.Load(assetName, typeof(T));
            m_AssetCache.Add(assetName, asset);
            return asset.Get<T>();
        }

        /// <summary>
        /// loader取消对某个资源的引用
        /// </summary>
        public void ReleaseRes(string name)
        {
            if (m_AssetCache.TryGetValue(name, out var asset))
            {
                asset.Release();
                m_AssetCache.Remove(name);
            }
            else if (m_WaitingRes.ContainsKey(name))
            {
                m_WaitingRes.Remove(name);
            }
        }

        /// <summary>
        /// loader取消对所有资源的引用
        /// </summary>
        public void ReleaseAllRes()
        {
            foreach (var asset in m_AssetCache)
            {
                asset.Value.Release();
            }
            m_AssetCache.Clear();
        }
        
        public void Dispose()
        {
            ReleaseAllRes();
            m_WaitingRes.Clear();
            m_CompleteCallbacks.Clear();
            m_OnAllFinish = null;
        }

        public void Recycle2Cache()
        {
            Recycle(this);
        }

        /// <summary>
        /// 让一个loader返回池中
        /// </summary>
        /// <param name="resLoader">要回池的loader</param>
        public static void Recycle(ResLoader resLoader)
        {
            resLoader.Dispose();
            m_Loaders.Push(resLoader);
        }
        
        public static void Clear()
        {
            while (m_Loaders.Count > 0)
            {
                m_Loaders.Pop().Dispose();
            }
        }
    }
}