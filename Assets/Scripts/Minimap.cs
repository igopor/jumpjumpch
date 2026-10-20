using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 小地图控制器 - 左上角显示，贴合Wall边界
/// </summary>
public class Minimap : MonoBehaviour
{
    [Header("UI引用")]
    public RawImage mapImage;      // 小地图背景
    public RawImage markerImage;   // 玩家标记（小鸡）

    [Header("场景引用")]
    public Transform player;
    public Transform wallParent;   // Wall的父对象，包含所有墙壁块

    [Header("显示设置")]
    public Vector2 mapSize = new Vector2(150, 150);  // 小地图像素尺寸
    public Vector2 screenOffset = new Vector2(20, 20); // 距离屏幕左上角的偏移
    public float markerSize = 15f;  // 标记大小
    public bool showDebugLogs = true; // 是否显示调试日志

    private RectTransform mapRect;
    private RectTransform markerRect;
    private Bounds wallBounds;     // 使用Unity的Bounds类计算边界
    private bool ready = false;
    private int logCounter = 0;

    void Start()
    {
        // 获取组件
        mapRect = mapImage.GetComponent<RectTransform>();
        markerRect = markerImage.GetComponent<RectTransform>();

        if (mapRect == null || markerRect == null || player == null)
        {
            Debug.LogError("[Minimap] 缺少必要引用！");
            return;
        }

        // 设置小地图
        SetupMap();
        
        // 计算Wall边界
        CalculateWallBounds();
        
        // 设置标记
        SetupMarker();
        
        ready = true;
        Debug.Log("[Minimap] ===== 初始化完成 =====");
        PrintDebugInfo();
    }

    void SetupMap()
    {
        // 小地图锚点：左上角
        mapRect.anchorMin = new Vector2(0, 1);
        mapRect.anchorMax = new Vector2(0, 1);
        mapRect.pivot = new Vector2(0, 1);
        mapRect.sizeDelta = mapSize;
        mapRect.anchoredPosition = new Vector2(screenOffset.x, -screenOffset.y);
        
        Debug.Log($"[Minimap] 小地图位置: 左上角偏移 ({screenOffset.x}, {-screenOffset.y})");
        Debug.Log($"[Minimap] 小地图尺寸: {mapSize.x} x {mapSize.y}");
    }

    void SetupMarker()
    {
        // 标记锚点：左上角，Pivot：中心
        markerRect.anchorMin = new Vector2(0, 1);
        markerRect.anchorMax = new Vector2(0, 1);
        markerRect.pivot = new Vector2(0.5f, 0.5f);
        markerRect.sizeDelta = new Vector2(markerSize, markerSize);
        
        // 确保标记颜色正确
        if (markerImage.texture != null)
        {
            markerImage.color = Color.white;
        }
        
        Debug.Log($"[Minimap] 标记大小: {markerSize} x {markerSize}");
        Debug.Log($"[Minimap] 标记Pivot: (0.5, 0.5)");
    }

    void CalculateWallBounds()
    {
        if (wallParent == null)
        {
            Debug.LogError("[Minimap] wallParent未赋值！");
            return;
        }

        // 使用Unity内置的Bounds计算
        wallBounds = new Bounds(wallParent.position, Vector3.zero);
        bool hasChild = false;

        Debug.Log("[Minimap] ----- 开始计算Wall边界 -----");
        
        foreach (Transform child in wallParent)
        {
            hasChild = true;
            Renderer renderer = child.GetComponent<Renderer>();
            if (renderer != null)
            {
                wallBounds.Encapsulate(renderer.bounds);
                Debug.Log($"[Minimap]   - 墙壁块: {child.name}, 位置({child.position.x:F2}, {child.position.z:F2})");
            }
            else
            {
                Vector3 pos = child.position;
                wallBounds.Encapsulate(pos);
                wallBounds.Encapsulate(pos + Vector3.one * 0.5f);
                wallBounds.Encapsulate(pos - Vector3.one * 0.5f);
                Debug.LogWarning($"[Minimap]   - {child.name} 缺少Renderer组件，使用位置估算");
            }
        }

        if (!hasChild)
        {
            float size = 25f;
            wallBounds = new Bounds(wallParent.position, new Vector3(size * 2, 1, size * 2));
            Debug.LogWarning("[Minimap] Wall没有子物体，使用默认边界");
        }

        Debug.Log($"[Minimap] Wall边界 - 最小: X={wallBounds.min.x:F2}, Z={wallBounds.min.z:F2}");
        Debug.Log($"[Minimap] Wall边界 - 最大: X={wallBounds.max.x:F2}, Z={wallBounds.max.z:F2}");
        Debug.Log($"[Minimap] Wall边界 - 范围: X={wallBounds.size.x:F2}, Z={wallBounds.size.z:F2}");
    }

    void PrintDebugInfo()
    {
        if (!showDebugLogs) return;
        
        Vector2 playerPos = new Vector2(player.position.x, player.position.z);
        Debug.Log("[Minimap] ===== 当前状态 =====");
        Debug.Log($"[Minimap] 玩家位置: ({playerPos.x:F2}, {playerPos.y:F2})");
        Debug.Log($"[Minimap] Wall边界: X({wallBounds.min.x:F2} ~ {wallBounds.max.x:F2}), Z({wallBounds.min.z:F2} ~ {wallBounds.max.z:F2})");
        
        // 计算期望的小地图位置
        float normX = (playerPos.x - wallBounds.min.x) / (wallBounds.max.x - wallBounds.min.x);
        float normZ = (playerPos.y - wallBounds.min.z) / (wallBounds.max.z - wallBounds.min.z);
        float expectedX = normX * mapSize.x;
        float expectedY = (1f - normZ) * mapSize.y;
        Debug.Log($"[Minimap] 期望标记位置: ({expectedX:F2}, {expectedY:F2})");
        Debug.Log("[Minimap] ====================");
    }

    void LateUpdate()
    {
        if (!ready) return;
        UpdateMarker();
    }

    void UpdateMarker()
    {
        if (player == null || markerRect == null) return;

        // 获取玩家XZ坐标
        Vector2 playerPos = new Vector2(player.position.x, player.position.z);
        
        // 计算归一化坐标
        float rangeX = wallBounds.max.x - wallBounds.min.x;
        float rangeZ = wallBounds.max.z - wallBounds.min.z;
        
        if (rangeX <= 0.001f || rangeZ <= 0.001f)
        {
            Debug.LogError($"[Minimap] Wall边界范围异常: X={rangeX}, Z={rangeZ}");
            return;
        }
        
        float normX = (playerPos.x - wallBounds.min.x) / rangeX;
        float normZ = (playerPos.y - wallBounds.min.z) / rangeZ;
        
        // 限制范围
        float clampedX = Mathf.Clamp01(normX);
        float clampedZ = Mathf.Clamp01(normZ);
        
        // 映射到小地图坐标（Z轴反转，因为UI的Y轴向下）
        float mapX = clampedX * mapSize.x;
        float mapY = (1f - clampedZ) * mapSize.y;
        
        // 更新标记位置
        markerRect.anchoredPosition = new Vector2(mapX, mapY);
        
        // 每30帧打印一次调试信息
        logCounter++;
        if (showDebugLogs && logCounter % 30 == 0)
        {
            Debug.Log($"[Minimap] --- 帧{logCounter} ---");
            Debug.Log($"[Minimap] 玩家世界: ({playerPos.x:F2}, {playerPos.y:F2})");
            Debug.Log($"[Minimap] 归一化: X={normX:F4}, Z={normZ:F4} (限制后: X={clampedX:F4}, Z={clampedZ:F4})");
            Debug.Log($"[Minimap] 小地图坐标: ({mapX:F2}, {mapY:F2})");
            Debug.Log($"[Minimap] 当前标记位置: {markerRect.anchoredPosition}");
            
            // 检查是否在有效范围内
            bool inRange = mapX >= 0 && mapX <= mapSize.x && mapY >= 0 && mapY <= mapSize.y;
            Debug.Log($"[Minimap] 标记在小地图范围内: {inRange}");
        }
    }

    // 编辑器辅助：在Scene视图中显示Wall边界
    void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying && wallParent != null)
        {
            Gizmos.color = Color.yellow;
            Vector3 center = new Vector3(
                (wallBounds.min.x + wallBounds.max.x) / 2f,
                0,
                (wallBounds.min.z + wallBounds.max.z) / 2f
            );
            Vector3 size = new Vector3(
                wallBounds.size.x,
                0.5f,
                wallBounds.size.z
            );
            Gizmos.DrawWireCube(center, size);
        }
    }
}