using System;
using UAsset;

public class PatchUpdater
{
    public static CheckForUpdates CheckForUpdate { get; set; }
    public static PatchPage PatchPage { get; set; }

    public static GetDownloadSize DownloadBundleOp { get; set; }

    public static void Run()
    {
        PatchStepManager.AddStep(new PatchInit());
        PatchStepManager.AddStep(new UpdateVersion());
        PatchStepManager.AddStep(new UpdateManifest());
        PatchStepManager.AddStep(new CheckingResource());
        PatchStepManager.AddStep(new DownloadBundles());
        PatchStepManager.AddStep(new PatchDone());
        PatchStepManager.Run(nameof(PatchInit));
    }
    
    public static void PatchComplete()
    {
        CheckForUpdate.SaveAndUpdateVersion();
        PatchStepManager.Transition(nameof(PatchDone));
    }

    public static void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.ExitPlaymode();
#else
        UnityEngine.Application.Quit();
#endif
    }
}