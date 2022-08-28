using System;
using System.Collections.Generic;

namespace xasset
{
    [Serializable]
    public class ManifestBundle
    {
        public int id;
        public string name;
        public long size;
        public string hash;
        public int[] deps; // 依赖项
        public bool isRaw; // 是否是二进制文件
        public bool copyToPackage; // 打安装包时，是否拷贝到StreamingAssets
        public string nameWithAppendHash; // 带hash值的bundle名
        public string tag; // 资源标签(非重要资源，打了标签热更时会忽略 且 打安装包时不会拷贝到StreamingAssets)
        public string variant; // 变体名称
        
        public List<string> assets { get; set; }
        public bool IsWithTag => !string.IsNullOrEmpty(tag);
        public bool IsVariant => !string.IsNullOrEmpty(variant);
    }
}