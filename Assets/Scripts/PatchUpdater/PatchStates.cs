/// <summary>
/// 热更状态
/// </summary>
public enum PatchStates
{
    /// <summary>
    /// 更新资源版本
    /// </summary>
    UpdateVersion,

    /// <summary>
    /// 更新清单
    /// </summary>
    UpdateManifest,

    /// <summary>
    /// 检查资源差异
    /// </summary>
    CheckingResource,
    
    /// <summary>
    /// 下载远端文件
    /// </summary>
    DownloadBundles,

    /// <summary>
    /// 补丁流程完毕
    /// </summary>
    PatchDone,
}