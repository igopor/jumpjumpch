using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 按 Esc 键弹出/关闭图片12
/// 挂在 Canvas 上
/// </summary>
public class EscapeMenuUI : MonoBehaviour
{
    [Header("UI References")]
    public GameObject escapePanel;         // 图片12的面板
    public Button exitButton;              // 退出游戏按钮

    [Header("Texture")]
    public Sprite escapeSprite;            // 图片12的Sprite

    private Image escapeImage;             // 图片12的 Image 组件
    private bool isShowing = false;        // 当前是否显示

    void Start()
    {
        if (escapePanel == null)
        {
            Debug.LogError("[EscapeMenuUI] escapePanel 未赋值！");
            return;
        }

        // 获取 Image 组件并设置图片
        escapeImage = escapePanel.GetComponent<Image>();
        if (escapeImage == null)
        {
            Debug.LogError("[EscapeMenuUI] escapePanel 上没有 Image 组件！");
            return;
        }

        if (escapeSprite != null)
        {
            escapeImage.sprite = escapeSprite;
        }

        // 查找退出按钮（如果没有在Inspector中赋值）
        if (exitButton == null)
        {
            exitButton = escapePanel.GetComponentInChildren<Button>(true);
            if (exitButton != null)
            {
                Debug.Log("[EscapeMenuUI] 自动找到退出按钮: " + exitButton.name);
            }
        }

        // 绑定退出按钮事件
        if (exitButton != null)
        {
            exitButton.onClick.RemoveAllListeners();
            exitButton.onClick.AddListener(QuitGame);
            Debug.Log("[EscapeMenuUI] 退出按钮事件绑定成功");
        }
        else
        {
            Debug.LogWarning("[EscapeMenuUI] 未找到退出按钮");
        }

        // 初始隐藏
        escapePanel.SetActive(false);
    }

    void Update()
    {
        // 按 Esc 键切换显示/隐藏
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            isShowing = !isShowing;
            escapePanel.SetActive(isShowing);

            // 显示/隐藏鼠标
            if (isShowing)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }
    }

    /// <summary>
    /// 退出游戏功能
    /// </summary>
    public void QuitGame()
    {
        Debug.Log("[EscapeMenuUI] 退出游戏");

        #if UNITY_EDITOR
        // 在编辑器模式下停止播放
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        // 在发布版本中退出应用
        Application.Quit();
        #endif
    }

    void OnDestroy()
    {
        // 移除按钮监听，防止内存泄漏
        if (exitButton != null)
        {
            exitButton.onClick.RemoveListener(QuitGame);
        }
    }
}
