using System.IO;
using UnityEngine;

namespace UAsset.Editor
{
    /// <summary>
    ///     拷贝资源到StreamingAssets目录(编辑器下默认只拷贝版本和清单文件，出包时由jenkins参数决定)
    /// </summary>
    public class CopyResToPackage : BuildTaskJob
    {
        public CopyResToPackage(BuildTask task) : base(task)
        {
        }

        protected override void DoTask()
        {
            BuildScript.CopyToStreamingAssets(_task.packageResourceType, _task.buildVariant);
        }
    }
}