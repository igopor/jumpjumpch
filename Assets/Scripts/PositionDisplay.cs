using UnityEngine;
using TMPro;  // 如果使用 TextMeshPro
// using UnityEngine.UI;  // 如果使用 Legacy Text

public class PositionDisplay : MonoBehaviour
{
    [Header("引用")]
    public Transform player;           // 拖入主角的 Transform
    public TextMeshProUGUI displayText; // 拖入 UI 文本组件

    [Header("格式")]
    public string prefix = "Y: ";
    public string format = "F2";       // F2 = 保留2位小数

    void Update()
    {
        if (player != null && displayText != null)
        {
            float yPos = player.position.y;
            displayText.text = prefix + yPos.ToString(format);
        }
    }
}