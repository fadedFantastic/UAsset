using System.Collections;
using UnityEngine;
using xasset;

/// <summary>
/// 下载bundle文件
/// </summary>
public class DownloadBundles : IPatchStep
{
    public string Name { get; } = nameof(DownloadBundles);
    
    public void OnEnter()
    {
        PatchUpdater.PatchPage.ShowStateChangeTips(PatchStates.DownloadBundles);
        GameStart.StartCoroutineWrap(CheckUpdatePackage());
    }
    
    public void OnExit()
    {
    }

    private IEnumerator CheckUpdatePackage()
    {
        yield return new WaitForSecondsRealtime(0.5f);
        
        var downloader = PatchUpdater.DownloadBundleOp.DownloadAsync();
        downloader.updated = PatchUpdater.PatchPage.ShowProgress;
        yield return downloader;
            
        if (downloader.status == OperationStatus.Success)
        {
            PatchUpdater.PatchComplete();
        }
        else
        {
            Download.ClearAllDownloads();
            PatchUpdater.PatchPage.ShowMessageBox(PatchMessageBoxType.DownloadBundleFailed, isOk =>
            {
                if (isOk)
                    PatchStepManager.Transition(nameof(CheckingResource));
                else
                    PatchUpdater.Quit();
            });
        }
    }
}