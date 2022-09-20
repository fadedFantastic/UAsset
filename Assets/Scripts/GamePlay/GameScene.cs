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


            // 同步加载贴图
            {
                var button = rootCanvas.transform.Find("load_sprite").GetComponent<Button>();
                var image = rootCanvas.transform.Find("load_sprite/image").GetComponent<Image>();
                Asset loadable = null;
                button.onClick.AddListener(() =>
                {
                    loadable = Asset.Load("Assets/Res/UISprite/daggers_2.png", typeof(Sprite));
                    image.sprite = loadable.Get<Sprite>();
                });
                
                var unloadButton = rootCanvas.transform.Find("load_sprite/unload").GetComponent<Button>();
                unloadButton.onClick.AddListener(() =>
                {
                    loadable?.Release();
                });
            }
        }
    }
}