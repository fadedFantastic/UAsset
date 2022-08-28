using System;
using System.Collections;
using UnityEngine;

public class GameStart : MonoBehaviour
{
    private static GameStart s_Instance;
        
    private void Awake()
    {
        s_Instance = this;
    }

    public static void StartCoroutineWrap(IEnumerator co)
    {
        s_Instance.StartCoroutine(co);
    }

    // 热更完成后的进游戏的一些初始化
    public static void OnUpdateComplete()
    {
        
    }
}