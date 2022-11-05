using System;
using UnityEngine;

namespace UAsset
{
    /// <summary>
    /// 自动管理引用组件，销毁时自动减少引用计数
    /// 如果挂载该组件的prefab被克隆，由于_referencedAsset为非序列化数据，并不会被一起克隆，所以引用计数还是安全的
    /// </summary>
    public class AssetAutoReferencer : MonoBehaviour
    {
        private Asset _referencedAsset;

        public void RefAsset(Asset asset)
        {
            _referencedAsset = asset;
        }

        private void OnDestroy()
        {
            _referencedAsset?.Release();
            _referencedAsset = null;
        }
    }
}