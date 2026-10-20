using UnityEngine;

public class ChickenController : MonoBehaviour
{
    [Header("Movement")]
    public float walkSpeed = 5f;
    public float jumpForce = 5f;
    public float rotationSmoothTime = 0.1f;

    [Header("Camera")]
    public Transform cameraTarget;
    public float mouseSensitivity = 2f;
    public float minVerticalAngle = -30f;
    public float maxVerticalAngle = 60f;
    public float cameraDistance = 3.5f;

    [Header("Camera Bob")]
    public bool enableCameraBob = true;
    public float bobFrequency = 8f;
    public float bobVerticalAmplitude = 0.03f;
    public float bobHorizontalAmplitude = 0.015f;

    [Header("Physics")]
    public float gravity = -9.81f;

    [Header("Audio")]
    public AudioClip[] footstepSounds;     // 脚步声数组（可放多个不同音效）
    public AudioSource audioSource;        // 音源组件
    public float footstepInterval = 0.3f;  // 脚步声间隔（秒）
    public float minSpeedForFootstep = 1f; // 触发脚步声的最小速度

    private Animator animator;
    private CharacterController characterController;
    private Vector3 moveDirection;
    [SerializeField] private Vector3 velocity;
    private float currentAngleX = 0f;
    private float currentAngleY = 0f;
    private float turnSmoothVelocity;
    private float bobTimer = 0f;
    private Vector3 cameraBaseOffset;

    // 新增：记录当前站立的地面物体
    private GameObject currentGroundObject;

    // 脚步声相关
    private float footstepTimer = 0f;

    void Start()
    {
        animator = GetComponent<Animator>();
        characterController = GetComponent<CharacterController>();

        if (characterController == null)
        {
            characterController = gameObject.AddComponent<CharacterController>();
            Debug.Log("[ChickenController] 自动添加了 CharacterController");
        }

        // ===== 音频系统初始化 =====
        // 设置音频组件
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                Debug.Log("[ChickenController] 自动添加了 AudioSource");
            }
        }

        // 配置 AudioSource 参数（脚步声专用）
        audioSource.spatialBlend = 0f;       // 0=2D音效（全局可听），不随距离衰减
        audioSource.volume = 1.0f;            // 音量
        audioSource.playOnAwake = false;      // 脚步声由代码控制，不自播放
        audioSource.loop = false;             // 脚步声不循环

        Debug.Log($"[ChickenController] 音频初始化完成: footstepSounds数组长度={(footstepSounds != null ? footstepSounds.Length.ToString() : "null")}, audioSource={audioSource != null}");
        if (footstepSounds != null && footstepSounds.Length > 0)
        {
            for (int i = 0; i < footstepSounds.Length; i++)
            {
                Debug.Log($"[ChickenController] footstepSounds[{i}] = {(footstepSounds[i] != null ? footstepSounds[i].name : "null")}");
            }
        }
        else
        {
            Debug.LogWarning("[ChickenController] ⚠️ footstepSounds 数组为空！请在 Inspector 中为 footstepSounds 添加 AudioClip！");
        }

        // Calculate local bounds of all renderers
        var renders = GetComponentsInChildren<Renderer>();
        if (renders.Length > 0)
        {
            Vector3 localMin = Vector3.positiveInfinity;
            Vector3 localMax = Vector3.negativeInfinity;

            foreach (var renderer in renders)
            {
                Matrix4x4 worldToLocal = transform.worldToLocalMatrix;

                Bounds rendererBounds = renderer.bounds;
                Vector3[] corners = new Vector3[8];
                Vector3 center = rendererBounds.center;
                Vector3 ext = rendererBounds.extents;
                corners[0] = center + new Vector3(-ext.x, -ext.y, -ext.z);
                corners[1] = center + new Vector3(ext.x, -ext.y, -ext.z);
                corners[2] = center + new Vector3(-ext.x, ext.y, -ext.z);
                corners[3] = center + new Vector3(ext.x, ext.y, -ext.z);
                corners[4] = center + new Vector3(-ext.x, -ext.y, ext.z);
                corners[5] = center + new Vector3(ext.x, -ext.y, ext.z);
                corners[6] = center + new Vector3(-ext.x, ext.y, ext.z);
                corners[7] = center + new Vector3(ext.x, ext.y, ext.z);

                foreach (var corner in corners)
                {
                    Vector3 localCorner = worldToLocal.MultiplyPoint3x4(corner);
                    localMin = Vector3.Min(localMin, localCorner);
                    localMax = Vector3.Max(localMax, localCorner);
                }
            }

            Vector3 localSize = localMax - localMin;
            Vector3 localCenter = (localMin + localMax) * 0.5f;

            // Move the chicken so feet (localMin.y) are at Y=0
            transform.position = new Vector3(transform.position.x, -localMin.y, transform.position.z);

            // Set CharacterController to match the mesh bounds
            characterController.height = localSize.y;
            characterController.center = new Vector3(localCenter.x, localCenter.y, localCenter.z);
            characterController.radius = Mathf.Max(localSize.x, localSize.z) * 0.35f;
            characterController.skinWidth = 0.01f;
        }

        Cursor.lockState = CursorLockMode.Locked;

        // Camera pivot is NOT a child - it follows position independently
        if (cameraTarget == null)
        {
            GameObject pivot = new GameObject("CameraPivot");
            cameraTarget = pivot.transform;
        }
        cameraBaseOffset = Vector3.up * 0.6f;
        cameraTarget.position = transform.position + cameraBaseOffset;
    }

    void Update()
    {
        // === Camera follows chicken position (no rotation inheritance) ===
        if (cameraTarget != null)
            cameraTarget.position = transform.position + cameraBaseOffset;

        // === Mouse Camera Orbit ===
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        currentAngleX += mouseX;
        currentAngleY -= mouseY;
        currentAngleY = Mathf.Clamp(currentAngleY, minVerticalAngle, maxVerticalAngle);

        if (cameraTarget != null)
            cameraTarget.rotation = Quaternion.Euler(currentAngleY, currentAngleX, 0);

        // === WASD Movement ===
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        Vector3 inputDir = new Vector3(horizontal, 0, vertical);

        if (inputDir.magnitude > 0.1f)
        {
            inputDir.Normalize();

            // Movement direction relative to camera facing (not chicken facing!)
            Vector3 camForward = cameraTarget.forward;
            camForward.y = 0;
            camForward.Normalize();
            Vector3 camRight = cameraTarget.right;
            camRight.y = 0;
            camRight.Normalize();

            moveDirection = camForward * inputDir.z + camRight * inputDir.x;
            moveDirection.Normalize();

            // Chicken faces the movement direction
            float targetAngle = Mathf.Atan2(moveDirection.x, moveDirection.z) * Mathf.Rad2Deg - 90f;
            float smoothAngle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnSmoothVelocity, rotationSmoothTime);
            transform.rotation = Quaternion.Euler(0, smoothAngle, 0);
        }
        else
        {
            moveDirection = Vector3.zero;
        }

        // Animator
        float speed = moveDirection.magnitude;
        if (animator != null)
            animator.SetFloat("Speed", speed, 0.1f, Time.deltaTime);

        // === Ground & Jump Logic (Modified for Bounce) ===
        bool isGrounded = characterController.isGrounded;

        // 更新当前站立的地面物体
        UpdateCurrentGroundObject();

        // 重要：如果是刚刚被弹起来（向上移动），不要立即重置为 -2f
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }
        // 如果刚落地时速度还向上，说明还在弹跳中，保持现状，什么都不做

        // Regular jump input
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
            velocity.y = Mathf.Sqrt(jumpForce * -2f * gravity);

        // Apply gravity
        velocity.y += gravity * Time.deltaTime;

        // Apply movement
        Vector3 movement = moveDirection * walkSpeed;
        movement.y = velocity.y;
        characterController.Move(movement * Time.deltaTime);

        // Update bob timer
        bool isMoving = moveDirection.magnitude > 0.1f && isGrounded;
        if (isMoving)
        {
            bobTimer += Time.deltaTime * bobFrequency * speed;
        }
        else
        {
            bobTimer = 0f;
        }

        // === 脚步声系统 ===
        HandleFootsteps(isMoving, speed);
    }

    void LateUpdate()
    {
        // Camera follows the pivot with distance
        if (Camera.main != null && cameraTarget != null)
        {
            // Calculate bob offset
            Vector3 bobOffset = Vector3.zero;
            if (enableCameraBob && bobTimer > 0f)
            {
                float verticalBob = Mathf.Sin(bobTimer) * bobVerticalAmplitude;
                float horizontalBob = Mathf.Cos(bobTimer * 0.5f) * bobHorizontalAmplitude;
                bobOffset = cameraTarget.up * verticalBob + cameraTarget.right * horizontalBob;
            }

            // Camera behind the pivot based on pivot's rotation, with bob offset
            Vector3 camPos = cameraTarget.position + cameraTarget.rotation * new Vector3(0, 0, -cameraDistance);
            camPos += bobOffset;
            Camera.main.transform.position = Vector3.Lerp(Camera.main.transform.position, camPos, Time.deltaTime * 10f);
            Camera.main.transform.LookAt(cameraTarget.position + bobOffset * 0.3f);
        }
    }

    // ========== 脚步声系统 ==========

    private void HandleFootsteps(bool isMoving, float speed)
    {
        // ===== 调试输出：每帧显示脚步声系统状态 =====
        if (isMoving)
        {
            Debug.Log($"[脚步声调试] isMoving={isMoving}, speed={speed:F2}, minSpeedForFootstep={minSpeedForFootstep}, " +
                      $"isGrounded={characterController.isGrounded}, footstepTimer={footstepTimer:F3}, " +
                      $"footstepSounds长度={(footstepSounds != null ? footstepSounds.Length.ToString() : "null")}, " +
                      $"audioSource={(audioSource != null ? "存在" : "null")}");
        }

        if (isMoving && speed > minSpeedForFootstep && characterController.isGrounded)
        {
            footstepTimer -= Time.deltaTime;

            if (footstepTimer <= 0f)
            {
                PlayFootstepSound();
                // 根据速度动态调整脚步间隔（跑得快，脚步更快）
                footstepTimer = footstepInterval / Mathf.Max(1f, speed);
                Debug.Log($"[脚步声调试] 播放了一次脚步声，下次间隔={footstepTimer:F3}秒");
            }
        }
        else
        {
            if (footstepTimer > 0f)
            {
                // 只有从"正在走"切换到"停止"时才重置计时器
                footstepTimer = 0f;
                Debug.Log("[脚步声调试] 停止移动，重置脚步计时器");
            }
        }
    }

    private void PlayFootstepSound()
    {
        Debug.Log("[脚步声调试] PlayFootstepSound() 被调用! Time=" + Time.time);

        if (footstepSounds == null || footstepSounds.Length == 0)
        {
            Debug.LogWarning("[脚步声调试] ⚠️ footstepSounds 数组为空！请在 Inspector 中为 footstepSounds 数组添加 AudioClip。");
            return;
        }

        if (audioSource == null)
        {
            Debug.LogError("[脚步声调试] ⚠️ audioSource 为 null！无法播放脚步声！");
            return;
        }

        // 随机选择一个脚步声效
        AudioClip clipToPlay = footstepSounds[Random.Range(0, footstepSounds.Length)];
        Debug.Log($"[脚步声调试] 准备播放: clip={clipToPlay.name}, 长度={clipToPlay.length:F2}秒, " +
                  $"audioSource.isPlaying={audioSource.isPlaying}, audioSource.volume={audioSource.volume}");

        // 随机微调音调，让脚步声更自然
        audioSource.pitch = 0.9f + Random.Range(-0.1f, 0.1f);

        // 播放音效
        audioSource.PlayOneShot(clipToPlay);
        Debug.Log($"[脚步声调试] PlayOneShot 执行完毕, audioSource.isPlaying={audioSource.isPlaying}");
    }

    // ========== 蹦床所需的三个公共接口 ==========

    /// <summary>
    /// 获取当前速度（供 BouncePad 调用）
    /// </summary>
    public Vector3 GetVelocity()
    {
        return velocity;
    }

    /// <summary>
    /// 设置速度（供 BouncePad 调用）
    /// </summary>
    public void SetVelocity(Vector3 newVelocity)
    {
        velocity = newVelocity;
        // 可选：设置速度后稍微推离地面，避免卡碰撞
        if (newVelocity.y > 0)
        {
            characterController.Move(Vector3.up * 0.02f);
        }
    }

    /// <summary>
    /// 获取当前站立的地面物体（供 BouncePad 检测是否站在蹦床上）
    /// </summary>
    public GameObject GetCurrentGroundObject()
    {
        return currentGroundObject;
    }

    // 辅助方法：更新当前站立的地面物体
    private void UpdateCurrentGroundObject()
    {
        if (characterController.isGrounded)
        {
            // 使用射线检测获取脚下的物体
            RaycastHit hit;
            float checkDistance = characterController.height * 0.5f + characterController.skinWidth + 0.1f;
            if (Physics.Raycast(transform.position, Vector3.down, out hit, checkDistance))
            {
                currentGroundObject = hit.collider.gameObject;
            }
            else
            {
                currentGroundObject = null;
            }
        }
        else
        {
            currentGroundObject = null;
        }
    }

    // ========== 原有的 Bounce 方法（保留兼容） ==========

    /// <summary>
    /// 让角色弹起，由蹦床物体调用
    /// </summary>
    /// <param name="bouncePower">弹起的速度（正值，如 8f）</param>
    public void Bounce(float bouncePower)
    {
        // 1. 覆盖当前的垂直速度
        velocity.y = bouncePower;

        // 2. 将角色强制“向上推”一点点，确保 CharacterController 立刻离开地面，
        //    这样重力就会马上生效，避免卡在地面上再次弹跳。
        characterController.Move(Vector3.up * 0.01f);

        // 3. (可选) 重置 bobTimer 让镜头晃动更自然
        bobTimer = 0f;

        // 4. (可选) 播放跳跃动画
        if (animator != null)
        {
            animator.SetTrigger("Jump"); // 需要你在 Animator 中定义 Jump 参数
        }

        // 5. (可选) 播放弹跳音效
        // AudioSource.PlayClipAtPoint(bounceSound, transform.position);
    }
}