using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 管理 kwanzaa-kikombe 的提示框UI
/// 挂在 Canvas 上，控制提示框的显示/隐藏
/// </summary>
public class KwanzaaKikombeUI : MonoBehaviour
{
    [Header("UI References")]
    public GameObject popupPanel;          // 提示框面板（包含背景图和关闭按钮）
    public Button closeButton;             // 关闭按钮

    [Header("Textures")]
    public Sprite popupBackgroundSprite;   // 提示框背景图（图片11）
    public Sprite buttonSprite;            // 按钮图片（图片8）

    private Image popupBackgroundImage;    // 提示框背景 Image 组件

    void Awake()
    {
        // 在 Awake 中尽早隐藏提示框
        if (popupPanel != null)
        {
            popupPanel.SetActive(false);
        }
    }

    void Start()
    {
        if (popupPanel == null)
        {
            Debug.LogError("[KwanzaaKikombeUI] popupPanel 未赋值！请在 Inspector 中将提示框物体（congras）拖到 Popup Panel 参数位置");
            return;
        }

        // 获取提示框背景的 Image 组件
        popupBackgroundImage = popupPanel.GetComponent<Image>();
        if (popupBackgroundImage == null)
        {
            Debug.LogError("[KwanzaaKikombeUI] popupPanel 上没有 Image 组件！");
            return;
        }

        // 设置提示框背景图
        if (popupBackgroundSprite != null)
        {
            popupBackgroundImage.sprite = popupBackgroundSprite;
        }

        // 设置按钮图片
        if (closeButton != null && buttonSprite != null)
        {
            Image buttonImage = closeButton.GetComponent<Image>();
            if (buttonImage != null)
            {
                buttonImage.sprite = buttonSprite;
            }
        }

        // 再次确保提示框隐藏
        popupPanel.SetActive(false);

        // 如果 Close Button 没有在 Inspector 中赋值，自动在 popupPanel 下查找
        if (closeButton == null)
        {
            closeButton = popupPanel.GetComponentInChildren<Button>(true);
            if (closeButton != null)
            {
                Debug.Log("[KwanzaaKikombeUI] 自动找到了关闭按钮: " + closeButton.name);
            }
        }

        // 绑定关闭按钮事件
        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(HidePopup);
            Debug.Log("[KwanzaaKikombeUI] 关闭按钮事件绑定成功");
        }
        else
        {
            Debug.LogError("[KwanzaaKikombeUI] ❌ 找不到关闭按钮！请确保 congras 下有一个 Button 组件，并将其拖到 Close Button 参数位置");
        }

        Debug.Log("[KwanzaaKikombeUI] 初始化完成，提示框已隐藏");
    }

    /// <summary>
    /// 显示提示框（显示提示框、显示鼠标）
    /// </summary>
    public void ShowPopup()
    {
        if (popupPanel != null)
        {
            popupPanel.SetActive(true);

            // 每次显示时重新查找并绑定按钮（防止隐藏时丢失引用）
            if (closeButton == null)
            {
                closeButton = popupPanel.GetComponentInChildren<Button>(true);
            }
            if (closeButton != null)
            {
                closeButton.onClick.RemoveAllListeners();
                closeButton.onClick.AddListener(HidePopup);
                Debug.Log("[KwanzaaKikombeUI] ShowPopup: 按钮事件已重新绑定");
            }
        }

        // 显示鼠标并解锁
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    /// <summary>
    /// 隐藏提示框（隐藏提示框、隐藏鼠标）
    /// </summary>
    public void HidePopup()
    {
        if (popupPanel != null)
        {
            popupPanel.SetActive(false);
        }

        // 隐藏鼠标并锁定回游戏
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void OnDestroy()
    {
        // 移除监听，防止内存泄漏
        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(HidePopup);
        }
    }
}
