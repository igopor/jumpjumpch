using UnityEngine;

public class BouncySurface : MonoBehaviour
{
    public float bounceForce = 15f;  // 弹起的力量

    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        // 1. 检查碰到的是不是主角
        if (hit.gameObject.CompareTag("Player"))
        {
            // 2. 获取主角身上的 CharacterController
            CharacterController controller = hit.gameObject.GetComponent<CharacterController>();
            if (controller != null)
            {
                // 3. 这里是关键：直接让主角“瞬移”向上移动，模拟弹跳
                //    CharacterController.Move 方法会立即改变位置，同时保持碰撞检测[citation:2][citation:8]
                Vector3 jumpVelocity = new Vector3(0, bounceForce, 0);
                controller.Move(jumpVelocity * Time.deltaTime);

                // 可选：如果主角有自己的移动脚本，也可以尝试调用它的Jump方法
                // hit.gameObject.GetComponent<PlayerMovement>()?.Jump();
            }
        }
    }
}