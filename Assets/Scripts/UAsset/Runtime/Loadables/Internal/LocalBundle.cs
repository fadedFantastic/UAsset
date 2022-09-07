using UnityEngine;

namespace UAsset
{
    internal class LocalBundle : Bundle
    {
        private AssetBundleCreateRequest _request;

        protected override void OnLoad()
        {
            _request = LoadAssetBundleAsync(pathOrURL);
        }

        public override void LoadImmediate()
        {
            if (isDone) return;

            // 这里直接访问assetBundle属性，Unity内部会自动异步转成同步加载，直到完成
            OnLoaded(_request.assetBundle);
            _request = null;
        }

        protected override void OnUpdate()
        {
            if (status != LoadableStatus.Loading) return;

            progress = _request.progress;
            if (_request.isDone) OnLoaded(_request.assetBundle);
        }
    }
}