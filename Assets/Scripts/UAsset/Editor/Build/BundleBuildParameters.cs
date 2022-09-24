using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace UAsset.Editor
{
    /// <summary>
    /// 资源包构建配置
    /// </summary>
    [CreateAssetMenu]
    public class BundleBuildParameters : ScriptableObject
    {
        /// <summary>
        /// 资源清单版本号
        /// </summary>
        public int manifestVersion = 1;

        /// <summary>
        /// 资源包构建设置
        /// </summary>
        public BuildAssetBundleOptions buildOptions = BuildAssetBundleOptions.ChunkBasedCompression;

        /// <summary>
        /// 资源包构建输出目录
        /// </summary>
        public string abPath = "Bundles";

        /// <summary>
        /// 内置变体名
        /// </summary>
        public string builtinVariant = "chinese";
        
        /// <summary>
        /// 是否进行冗余资源分析
        /// </summary>
        public bool runRedundancyAnalyze = true;

        /// <summary>
        /// 在资源包构建完成后是否将其复制到只读目录下
        /// </summary>
        public bool copyToStreamingAssets = true;

        /// <summary>
        /// 是否开启加密
        /// </summary>
        public bool encryptionEnable = true;
    }
}

