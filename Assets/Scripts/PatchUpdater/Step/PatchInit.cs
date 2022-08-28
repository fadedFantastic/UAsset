using System.Collections;
using UnityEngine;

public class PatchInit : IPatchStep
{
    public string Name { get; } = nameof(PatchInit);
    
    public void OnEnter()
    {
        var updaterGo = Object.Instantiate(Resources.Load<GameObject>("PatchPage"));
        PatchUpdater.PatchPage = updaterGo.GetComponent<PatchPage>();
        
        GameStart.StartCoroutineWrap(Begin());
    }

    public void OnExit()
    {
    }

    private IEnumerator Begin()
    {
        yield return new WaitForSecondsRealtime(0.5f);
        
        PatchStepManager.Transition(nameof(UpdateVersion));
    }
}