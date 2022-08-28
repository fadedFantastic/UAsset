using System;
using UnityEngine;
using UnityEngine.UI;
using xasset;

public enum PatchMessageBoxType
{
    UpdateVersionFailed,
    UpdateManifestFailed,
    NeedForceUpdate,
    LowDiskSpace,
    DownloadBundleFailed,
}

/// <summary>
/// 热更界面
/// </summary>
public class PatchPage : MonoBehaviour
{
    private class MessageBox
    {
        private GameObject m_Panel;
        private Text m_Content;
        
        public bool Visible => m_Panel.activeSelf;
        private Action<bool> completed { get; set; }

        public MessageBox(GameObject panel)
        {
            m_Panel = panel;
            var panelTransform = m_Panel.transform;
            
            m_Content = panelTransform.Find("Text").GetComponent<Text>();
            var confirmBtn = panelTransform.Find("Confirm").GetComponent<Button>();
            var cancelBtn = panelTransform.Find("Cancel").GetComponent<Button>();
            
            confirmBtn.onClick.AddListener(() => HandleClickEvent(true));
            cancelBtn.onClick.AddListener(() => HandleClickEvent(false));
        }
        
        public void Show(string content, Action<bool> onCompleted = null)
        {
            m_Content.text = content;
            completed = onCompleted;
            m_Panel.SetActive(true);
        }
        
        private void HandleClickEvent(bool clickOk)
        {
            completed?.Invoke(clickOk);
            Hide();
        }
    
        private void Hide()
        {
            m_Panel.SetActive(false);
            completed = null;
        }
    }

    private Slider m_Progress;
    private Text m_ProgressTips;
    private GameObject m_MsgBoxObj;
    private MessageBox m_MsgBox;

    private void Awake()
    {
        m_MsgBoxObj = transform.Find("ConfirmPanel").gameObject;
        m_Progress = transform.Find("Slider").GetComponent<Slider>();
        m_ProgressTips = transform.Find("Text").GetComponent<Text>();
        
        // 暂时好像没有开多个的需要
        m_MsgBox = new MessageBox(m_MsgBoxObj);
    }
    
    // TODO: 本地化处理
    public void ShowStateChangeTips(PatchStates state)
    {
        if (state == PatchStates.UpdateVersion)
            m_ProgressTips.text = "正在检查版本信息";
        else if (state == PatchStates.UpdateManifest)
            m_ProgressTips.text = "检查更新资源清单文件";
        else if (state == PatchStates.CheckingResource)
            m_ProgressTips.text = "正在检查资源";
        else if (state == PatchStates.DownloadBundles)
            m_ProgressTips.text = "开始下载文件";
        else if (state == PatchStates.PatchDone)
            m_ProgressTips.text = "正在启动游戏";
    }

    // TODO: 本地化处理
    public void ShowMessageBox(PatchMessageBoxType type, Action<bool> onClick)
    {
        string content = string.Empty;
        if (type == PatchMessageBoxType.UpdateVersionFailed)
            content = "更新版本文件失败，请检查网络连接后重试";
        else if (type == PatchMessageBoxType.UpdateManifestFailed)
            content = "下载清单文件失败，请检查网络连接后重试";
        else if (type == PatchMessageBoxType.NeedForceUpdate)
            content = "当前版本过低，需要前往下载最新安装包";
        else if (type == PatchMessageBoxType.LowDiskSpace)
            content = "当前磁盘空间不足，请清理空间后重试";
        else if (type == PatchMessageBoxType.DownloadBundleFailed)
            content = "下载资源失败，请检查网络连接后重试";

        ShowMessageBox(content, onClick);
    }

    // TODO: 本地化处理
    public void ShowProgress(long downloaded, long totalSize, long speed)
    {
        m_ProgressTips.text = $"下载中...{FormatBytes(downloaded)}/{FormatBytes(totalSize)}  速度: {FormatBytes(speed)}/s";
        m_Progress.value = downloaded * 1f / totalSize;
    }
    
    public void ShowMessageBox(string content, Action<bool> onClick)
    {
        m_MsgBox.Show(content, onClick);
    }
    
    private string FormatBytes(long bytes)
    {
        return Utility.FormatBytes(bytes);
    }
}