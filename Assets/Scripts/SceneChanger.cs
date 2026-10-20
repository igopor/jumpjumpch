using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneChanger : MonoBehaviour
{
    // 在 Inspector 中指定要跳转的场景名称
    public string sceneToLoad = "GameScene";

    // 这个方法会被按钮调用
    public void LoadScene()
    {
        SceneManager.LoadScene(sceneToLoad);
    }
}
