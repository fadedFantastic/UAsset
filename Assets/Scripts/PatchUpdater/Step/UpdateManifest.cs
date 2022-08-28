using System.Collections;
using UnityEngine;
using xasset;

/// <summary>
/// 更新清单
/// </summary>
public class UpdateManifest : IPatchStep
{
    public string Name { get; } = nameof(UpdateManifest);

    public void OnEnter()
    {
        PatchUpdater.PatchPage.ShowStateChangeTips(PatchStates.UpdateManifest);
        GameStart.StartCoroutineWrap(CheckManifest());
    }
    
    public void OnExit()
    {
    }

    private IEnumerator CheckManifest()
    {
        yield return new WaitForSecondsRealtime(0.5f);
        
        var checking = PatchUpdater.CheckForUpdate;
        if (checking.downloadSize > 0)
        {
            var downloadManifest = checking.DownloadAsync();
            yield return downloadManifest;

            if (downloadManifest.status == OperationStatus.Success)
            {
                PatchStepManager.Transition(nameof(CheckingResource));
            }
            else
            {
                PatchUpdater.PatchPage.ShowMessageBox(PatchMessageBoxType.UpdateManifestFailed, isOk =>
                {
                    if (isOk)
                        PatchStepManager.Transition(nameof(UpdateManifest));
                    else
                        PatchUpdater.Quit();
                });
            }
            yield break;
        }
        
        PatchStepManager.Transition(nameof(CheckingResource));
    }
}