using System.Collections;
using UnityEngine;
using UAsset;

/// <summary>
/// 检查资源差异
/// </summary>
public class CheckingResource : IPatchStep
{
    public string Name { get; } = nameof(CheckingResource);
    
    public void OnEnter()
    {
        PatchUpdater.PatchPage.ShowStateChangeTips(PatchStates.CheckingResource);
        GameStart.StartCoroutineWrap(CheckPatchList());
    }

    public void OnExit()
    {
    }

    private IEnumerator CheckPatchList()
    {
        yield return new WaitForSecondsRealtime(0.5f);
        
        var getDownloadSize = Versions.GetImportantAssetsDownloadSize();
        PatchUpdater.DownloadBundleOp = getDownloadSize;
        yield return getDownloadSize;

        var downloadSize = getDownloadSize.downloadSize;
        Debug.Log("手机磁盘剩余空间：" + Utility.FormatBytes(DiskUtility.GetAvailableSpace()));
        if (downloadSize > 0)
        {
            // 磁盘空间不足
            if (downloadSize > DiskUtility.GetAvailableSpace())
            {
                PatchUpdater.PatchPage.ShowMessageBox(PatchMessageBoxType.LowDiskSpace, isOk =>
                {
                    PatchUpdater.Quit();
                });
                
                yield break;
            }
            PatchStepManager.Transition(nameof(DownloadBundles)); 
        }
        else
        {
            PatchUpdater.PatchComplete();
        }
    }
}