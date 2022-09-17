using System;
using System.Collections.Generic;
using UAsset;
using UnityEngine;
using UnityEngine.UI;

namespace GamePlay
{
    public class GameScene : MonoBehaviour
    {
        public GameObject rootCanvas;
        
        private List<Loadable> _cacheLoadables;

        private void Start()
        {
            Init();
        }

        private void Init()
        {
            // 同步加载原生文件
            {
                var button = rootCanvas.transform.Find("load_rawFile").GetComponent<Button>();
                var hint = rootCanvas.transform.Find("load_rawFile/icon/hint").GetComponent<Text>();
                button.onClick.AddListener(() =>
                {
                    var loadable = RawAsset.Load("Assets/Res/RawText/rawText1.txt");
                    hint.text = loadable.GetFileText();
                });
            }
        }
    }
}