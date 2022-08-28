using System;
using UnityEngine;

namespace xasset
{
    public class GameObjectAssetReferencer : MonoBehaviour
    {
        private Asset m_ReferenceAsset = null;
        
        public void RefAsset(Asset asset)
        {
            m_ReferenceAsset = asset;
        }

        private void OnDestroy()
        {
            if (m_ReferenceAsset != null)
            {
                m_ReferenceAsset.Release();
                m_ReferenceAsset = null;
            }
        }
    }
}