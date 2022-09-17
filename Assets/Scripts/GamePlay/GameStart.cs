using System;
using System.Collections;
using UAsset;
using UnityEngine;

public class GameStart : MonoBehaviour
{
    private static GameStart s_Instance;
        
    private void Awake()
    {
        s_Instance = this;
        StartCoroutine(Init());
    }

    private IEnumerator Init()
    {
        yield return UAssets.Initialize();

        if (!UAssets.OfflineMode)
            PatchUpdater.Run();
        else
            OnUpdateComplete();
    }

    public static void StartCoroutineWrap(IEnumerator co)
    {
        s_Instance.StartCoroutine(co);
    }

    // 热更完成后的进游戏的一些初始化
    public static void OnUpdateComplete()
    {
        RawAsset.Load("Assets/Res/RawText/rawText1.txt");
    }
}