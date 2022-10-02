using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEngine;

namespace UAsset
{
    public class Loadable
    {
        private static readonly Dictionary<string, int> _loads = new Dictionary<string, int>();
        private static readonly Dictionary<string, int> _unloads = new Dictionary<string, int>();

        public static readonly List<Loadable> Loading = new List<Loadable>();
        public static readonly List<Loadable> Unused = new List<Loadable>();
        
        protected static bool _updateUnloadUnusedAssets;

        private readonly Reference _reference = new Reference();

        public int loadTimes => GetTimes(pathOrURL, _loads);
        public int unloadTimes => GetTimes(pathOrURL, _unloads);
        public int referenceCount => _reference.count;
        
        public LoadableStatus status { get; protected set; } = LoadableStatus.Wait;
        public string pathOrURL { get; protected set; }
        public string error { get; internal set; }

        public bool isDone => status == LoadableStatus.SuccessToLoad || status == LoadableStatus.Unloaded ||
                              status == LoadableStatus.FailedToLoad;

        public float progress { get; protected set; }

        private static int GetTimes(string path, IReadOnlyDictionary<string, int> times)
        {
            return times.TryGetValue(path, out var value) ? value : 0;
        }

        [Conditional("DEBUG")]
        private static void AddTimes(Loadable obj, IDictionary<string, int> dict)
        {
            if (obj is Dependencies) return;

            if (!dict.TryGetValue(obj.pathOrURL, out var times))
            {
                dict[obj.pathOrURL] = 1;
            }
            else
            {
                dict[obj.pathOrURL] = times + 1;
            }
        }

        protected void Finish(string errorCode = null)
        {
            error = errorCode;
            status = string.IsNullOrEmpty(errorCode) ? LoadableStatus.SuccessToLoad : LoadableStatus.FailedToLoad;
            progress = 1;
        }

        public static void UpdateAll()
        {
            for (var index = 0; index < Loading.Count; index++)
            {
                var item = Loading[index];
                if (Updater.busy) return;

                item.Update();
                if (!item.isDone) continue;

                Loading.RemoveAt(index);
                index--;
                item.Complete();
            }

            if (Scene.IsLoadingOrUnloading()) return;

            for (int index = 0, max = Unused.Count; index < max; index++)
            {
                var item = Unused[index];
                if (Updater.busy) break;
                
                if (!item.isDone) continue;

                Unused.RemoveAt(index);
                index--;
                max--;
                if (!item._reference.unused) continue;

                item.Unload();
            }
            
            if (Unused.Count > 0)
            {
                return;
            }

            if (_updateUnloadUnusedAssets)
            {
                Resources.UnloadUnusedAssets();
                _updateUnloadUnusedAssets = false;
            }
        }

        private void Update()
        {
            OnUpdate();
        }

        private void Complete()
        {
            OnComplete();
            
            if (status == LoadableStatus.FailedToLoad)
            {
                Logger.E("Unable to load {0} {1} with error: {2}", GetType().Name, pathOrURL, error);
                Release();
            }
        }

        protected virtual void OnUpdate()
        {
        }

        protected virtual void OnLoad()
        {
        }

        protected virtual void OnUnload()
        {
        }

        protected virtual void OnComplete()
        {
        }

        public virtual void LoadImmediate()
        {
            throw new InvalidOperationException();
        }

        protected void Load()
        {
            if (status != LoadableStatus.Wait && _reference.unused) Unused.Remove(this);

            _reference.Retain();
            Loading.Add(this);
            
            if (status != LoadableStatus.Wait) return;
            AddTimes(this, _loads);
            Logger.I("Load {0} {1}.", GetType().Name, pathOrURL);
            status = LoadableStatus.Loading;
            progress = 0;
            OnLoad();
        }

        private void Unload()
        {
            if (status == LoadableStatus.Unloaded) return;
            AddTimes(this, _unloads);
            Logger.I("Unload {0} {1}.", GetType().Name, pathOrURL, error);
            OnUnload();
            status = LoadableStatus.Unloaded;
        }

        public void Release()
        {
            if (_reference.count <= 0)
            {
                Logger.W("Release {0} {1}.", GetType().Name, Path.GetFileName(pathOrURL));
                return;
            }

            _reference.Release();
            if (!_reference.unused) return;

            Unused.Add(this);
            OnUnused();
        }

        protected virtual void OnUnused()
        {
        }

        public static void ClearAll()
        {
            ResLoader.Clear();
            
            // FIXME: 依赖于AssetBundle.UnloadAllAssetBundles，场景bundle总是卸载不掉，先手动卸下
            foreach (var bundle in Bundle.Cache.Values.ToList())
            {
                bundle.Unload();
            }
            Bundle.Cache.Clear();
            
            Asset.Cache.Clear();
            Dependencies.Cache.Clear();
            RawAsset.Cache.Clear();

            AssetBundle.UnloadAllAssetBundles(true);
        }
    }

    public enum LoadableStatus
    {
        Wait,
        Loading,
        DependentLoading,
        SuccessToLoad,
        FailedToLoad,
        Unloading,
        Unloaded
    }
}