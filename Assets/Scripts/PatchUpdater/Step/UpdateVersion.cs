using System.Collections;
using UnityEngine;
using UAsset;

/// <summary>
/// 更新版本文件
/// </summary>
public class UpdateVersion : IPatchStep
{
    public string Name { get; } = nameof(UpdateVersion);
    
    public void OnEnter()
    {
        PatchUpdater.PatchPage.ShowStateChangeTips(PatchStates.UpdateVersion);
        GameStart.StartCoroutineWrap(CheckVersion());
    }

    public void OnExit()
    {
    }

    /// <summary>
    /// 检查更新版本文件
    /// </summary>
    /// <returns></returns>
    private IEnumerator CheckVersion()
    {
        yield return new WaitForSecondsRealtime(0.5f);
        
        var checking = Versions.CheckForUpdatesAsync();
        yield return checking;

        PatchUpdater.CheckForUpdate = checking;
        if (checking.status == OperationStatus.Success)
        {
            OnUpdateVersionSuccess();
        }
        else
        {
            PatchUpdater.PatchPage.ShowMessageBox(PatchMessageBoxType.UpdateVersionFailed, isOk =>
            {
                if (isOk)
                    PatchStepManager.Transition(nameof(UpdateVersion));
                else
                    PatchUpdater.Quit();
            });
        }
    }

    /// <summary>
    /// 版本文件更新成功后，检查是否需要强更，热更等
    /// </summary>
    private void OnUpdateVersionSuccess()
    {
        var remoteVersions = PatchUpdater.CheckForUpdate.remoteVersions;
        
        // 比较游戏版本号，检查是否需要强更
        if (CheckNeedForceUpdate(remoteVersions))
        {
            PatchUpdater.PatchPage.ShowMessageBox(PatchMessageBoxType.NeedForceUpdate, isOk =>
            {
                if (isOk)
                {
                    // TODO: 跳转到对应渠道的下载地址
                }
                PatchUpdater.Quit();
            });
            return;
        }
        
        // 设置资源最新下载地址
        if (!string.IsNullOrEmpty(remoteVersions.UpdatePrefixUri))
        {
            Downloader.DownloadURL = remoteVersions.UpdatePrefixUri;
        }
        
        // 比较资源版本号，检查是否需要热更
        if (!CheckNeedHotFix(remoteVersions))
        {
            PatchStepManager.Transition(nameof(PatchDone));
            return;
        }

        PatchStepManager.Transition(nameof(UpdateManifest));
    }

    /// <summary>
    /// 检查是否需要强更
    /// </summary>
    /// <param name="remoteVersions">远端version</param>
    /// <returns></returns>
    private bool CheckNeedForceUpdate(BuildVersions remoteVersions)
    {
        var remoteEngineVersion = int.Parse(remoteVersions.gameVersion.Split('.')[1]);
        var localEngineVersion = int.Parse(Versions.GameVersion.Split('.')[1]);
        
        return remoteEngineVersion > localEngineVersion;
    }

    /// <summary>
    /// 检查是否需要热更资源
    /// </summary>
    /// <param name="remoteVersions">远端version</param>
    /// <returns></returns>
    private bool CheckNeedHotFix(BuildVersions remoteVersions)
    {
        var remoteResVersion = remoteVersions.internalResourceVersion;
        var localResVersion = Versions.InternalResourceVersion;
        var variantVersion = Versions.GetVariantVersion();

        return remoteResVersion > localResVersion || remoteResVersion > variantVersion;
    }

    /// <summary>
    /// 获取平台类型名称
    /// </summary>
    /// <returns></returns>
    private string GetPlatformName()
    {
        var platform = Application.platform;
        
#if UNITY_ANDROID || UNITY_EDITOR_WIN //内网测试的时候只能打android的internal包测试 打包机是Mac在外网
        platform = RuntimePlatform.Android;
#elif UNITY_IOS
        platform = RuntimePlatform.IPhonePlayer;
#endif
        
        return Utility.GetPlatformName(platform);
    }
}