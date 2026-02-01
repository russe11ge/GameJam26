using UnityEngine;
using UnityEngine.SceneManagement;

public class AutoLoadSceneAfterDelay : MonoBehaviour
{
    [Header("Delay")]
    [SerializeField] private float delaySeconds = 2f;

    [Header("Target (choose one)")]
    [SerializeField] private string targetSceneName = "";   // 例如 "SchoolGate"
    [SerializeField] private int targetBuildIndex = -1;      // 例如 1 (>=0才生效)

    private void Start()
    {
        Invoke(nameof(LoadTargetScene), delaySeconds);
    }

    private void LoadTargetScene()
    {
        // 优先用 Build Index（如果填了>=0）
        if (targetBuildIndex >= 0)
        {
            SceneManager.LoadScene(targetBuildIndex);
            return;
        }

        // 否则用 Scene 名字
        if (!string.IsNullOrEmpty(targetSceneName))
        {
            SceneManager.LoadScene(targetSceneName);
            return;
        }

        Debug.LogError("AutoLoadSceneAfterDelay: 没有设置 targetSceneName 或 targetBuildIndex。");
    }
}
