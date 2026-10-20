using UnityEngine;

public class BouncePad : MonoBehaviour
{
    [Header("弹跳参数")]
    public float bounceHeight = 8f;
    public float coolDownTime = 0.5f;
    public float minTriggerVelocity = -0.5f;

    [Header("重力设置")]
    public float gravity = -9.81f;  // 改为与 ChickenController 一致

    private Collider bounceCollider;
    private float lastBounceTime;
    private bool isCoolingDown;

    void Start()
    {
        bounceCollider = GetComponent<Collider>();
        if (bounceCollider != null)
        {
            bounceCollider.isTrigger = false;
        }

        if (bounceCollider.material == null || bounceCollider.material.bounciness == 0)
        {
            PhysicMaterial defaultMat = new PhysicMaterial("BouncePadMat");
            defaultMat.dynamicFriction = 0.6f;
            defaultMat.staticFriction = 0.6f;
            defaultMat.bounciness = 0f;
            bounceCollider.material = defaultMat;
        }

        lastBounceTime = -coolDownTime;
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            ApplyBounce(collision.gameObject);
        }
    }

    void OnCollisionStay(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player") && !isCoolingDown)
        {
            ApplyBounce(collision.gameObject);
        }
    }

    void ApplyBounce(GameObject player)
    {
        if (Time.time - lastBounceTime < coolDownTime)
        {
            isCoolingDown = true;
            return;
        }

        // 使用 ChickenController
        ChickenController chickenController = player.GetComponent<ChickenController>();
        if (chickenController == null)
        {
            Debug.LogWarning("Player没有ChickenController组件！");
            return;
        }

        Vector3 playerVel = chickenController.GetVelocity();

        // 只在向下运动时触发
        if (playerVel.y <= minTriggerVelocity)
        {
            float bounceVelocity = Mathf.Sqrt(2 * Mathf.Abs(gravity) * bounceHeight);
            Vector3 newVelocity = new Vector3(playerVel.x, bounceVelocity, playerVel.z);
            chickenController.SetVelocity(newVelocity);

            lastBounceTime = Time.time;
            isCoolingDown = false;

            Debug.Log($"Bounce! Height: {bounceHeight}, Velocity: {bounceVelocity}");
        }
    }
}