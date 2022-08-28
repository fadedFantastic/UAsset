using System.Collections;
using UnityEngine;

/// <summary>
/// 更新完成
/// </summary>
public class PatchDone : IPatchStep
{
    public string Name { get; } = nameof(PatchDone);
    
    public void OnEnter()
    {
        PatchUpdater.PatchPage.ShowStateChangeTips(PatchStates.PatchDone);
        GameStart.StartCoroutineWrap(Finish());
    }

    public void OnExit()
    {
    }

    private IEnumerator Finish()
    {
        yield return new WaitForSecondsRealtime(0.5f);
        
        var updaterGo = PatchUpdater.PatchPage.gameObject;
        if (updaterGo != null)
        {
            Object.Destroy(updaterGo);
            PatchUpdater.PatchPage = null;
        }
        
        GameStart.OnUpdateComplete();
    }
}