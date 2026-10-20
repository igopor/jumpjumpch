using UnityEngine;

/// <summary>
/// 背景音乐管理器 - 挂在 "bgm" 空对象上
/// 确保游戏运行期间一直循环播放背景音乐
/// </summary>
public class BGMManager : MonoBehaviour
{
    private AudioSource audioSource;

    void Awake()
    {
        // 获取 AudioSource 组件
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            Debug.LogWarning("[BGMManager] 自动添加了 AudioSource");
        }

        // 配置为背景音乐
        audioSource.spatialBlend = 0f;    // 2D 音效，全局可听
        audioSource.loop = true;          // 循环播放
        audioSource.playOnAwake = false;  // 由代码控制播放时机
        audioSource.volume = 0.5f;        // 背景音乐音量

        Debug.Log($"[BGMManager] Awake: clip={(audioSource.clip != null ? audioSource.clip.name : "null")}, playOnAwake={audioSource.playOnAwake}, loop={audioSource.loop}");
    }

    void Start()
    {
        // 确保开始播放
        if (audioSource != null && audioSource.clip != null)
        {
            if (!audioSource.isPlaying)
            {
                audioSource.Play();
                Debug.Log($"[BGMManager] Start() 开始播放背景音乐: {audioSource.clip.name}");
            }
            else
            {
                Debug.Log($"[BGMManager] Start() 背景音乐已在播放: {audioSource.clip.name}");
            }
        }
        else
        {
            Debug.LogError("[BGMManager] ⚠️ 没有设置背景音乐 AudioClip！请在 Inspector 中为 AudioSource 的 Clip 字段赋值！");
        }
    }

    void Update()
    {
        // 确保背景音乐持续播放（防止意外停止）
        if (audioSource != null && audioSource.clip != null && !audioSource.isPlaying)
        {
            audioSource.Play();
            Debug.Log("[BGMManager] 检测到背景音乐停止，重新播放");
        }
    }
}
