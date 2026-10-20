using UnityEngine;

/// <summary>
/// 挂在 kwanzaa-kikombe 物体上
/// 当主角（鸡）站着碰到该物体时，弹出UI提示框
/// </summary>
public class KwanzaaKikombeTrigger : MonoBehaviour
{
    [Header("UI Reference")]
    public KwanzaaKikombeUI uiController;  // 引用UI控制器

    [Header("Detection Settings")]
    public string playerTag = "Player";    // 主角的Tag

    void Start()
    {
        // 检查是否有 Collider
        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            Debug.LogError("[KwanzaaKikombeTrigger] ❌ kwanzaa-kikombe 没有 Collider 组件！请添加一个 Collider（如 BoxCollider）");
        }
        else
        {
            Debug.Log($"[KwanzaaKikombeTrigger] ✅ 找到 Collider: {col.GetType().Name}, isTrigger={col.isTrigger}");
        }

        if (uiController == null)
        {
            Debug.LogError("[KwanzaaKikombeTrigger] ❌ uiController 未赋值！请将 con canva 上的 KwanzaaKikombeUI 组件拖到这里");
        }
    }

    // 支持 Trigger 碰撞
    private void OnTriggerEnter(Collider other)
    {
        TryShowPopup(other.gameObject);
    }

    // 也支持普通物理碰撞（如果没勾选 Is Trigger 也能触发）
    private void OnCollisionEnter(Collision collision)
    {
        TryShowPopup(collision.gameObject);
    }

    private void TryShowPopup(GameObject other)
    {
        Debug.Log($"[KwanzaaKikombeTrigger] 碰撞检测！物体: {other.name}, Tag: {other.tag}");

        // 检查碰撞的是否是主角
        if (other.CompareTag(playerTag))
        {
            Debug.Log("[KwanzaaKikombeTrigger] ✅ 碰撞到主角！");

            if (uiController != null)
            {
                uiController.ShowPopup();
                Debug.Log("[KwanzaaKikombeTrigger] ✅ 主角碰到 kwanzaa-kikombe，弹出提示框！");
            }
            else
            {
                Debug.LogWarning("[KwanzaaKikombeTrigger] ❌ uiController 未赋值！请将 con canva 上的 KwanzaaKikombeUI 组件拖到这里");
            }
        }
        else
        {
            Debug.Log($"[KwanzaaKikombeTrigger] 碰撞物体 Tag 不是 {playerTag}，忽略");
        }
    }
}


