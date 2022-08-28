using System.Collections.Generic;
using UnityEngine;

namespace xasset
{
    public class Dependencies : Loadable
    {
        public static readonly Dictionary<string, Dependencies> Cache = new Dictionary<string, Dependencies>();

        private readonly List<Bundle> _bundles = new List<Bundle>();
        private Bundle _mainBundle;
        public AssetBundle assetBundle => _mainBundle?.assetBundle;

        public static Dependencies Load(string path)
        {
            if (!Cache.TryGetValue(path, out var item))
            {
                item = new Dependencies {pathOrURL = path};
                Cache.Add(path, item);
            }

            item.Load();
            return item;
        }

        protected override void OnLoad()
        {
            if (!Versions.GetDependencies(pathOrURL, out var info, out var infos))
            {
                Finish("Dependencies not found");
                return;
            }

            if (info == null)
            {
                Finish("info == null");
                return;
            }

            _mainBundle = Bundle.LoadInternal(info);
            _bundles.Add(_mainBundle);
            if (infos == null || infos.Length <= 0) return;

            foreach (var item in infos)
            {
                _bundles.Add(Bundle.LoadInternal(item));
            }
        }

        protected override void OnUnused()
        {
        }

        public override void LoadImmediate()
        {
            if (isDone) return;

            foreach (var request in _bundles)
            {
                request.LoadImmediate();
            }
        }

        protected override void OnUnload()
        {
            if (_bundles.Count > 0)
            {
                foreach (var item in _bundles)
                {
                    if (string.IsNullOrEmpty(item.error))
                    {
                        item.Release();
                    }
                }

                _bundles.Clear();
            }

            _mainBundle = null;
            Cache.Remove(pathOrURL);
        }

        protected override void OnUpdate()
        {
            if (status != LoadableStatus.Loading) return;

            var totalProgress = 0f;
            var allDone = true;
            foreach (var child in _bundles)
            {
                totalProgress += child.progress;
                if (!string.IsNullOrEmpty(child.error))
                {
                    status = LoadableStatus.FailedToLoad;
                    error = child.error;
                    progress = 1;
                    return;
                }

                if (child.isDone) continue;

                allDone = false;
                break;
            }

            progress = totalProgress / _bundles.Count * 0.5f;
            if (!allDone) return;

            if (assetBundle == null)
            {
                Finish("assetBundle == null");
                return;
            }

            Finish();
        }
    }
}