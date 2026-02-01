using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 检查点组件 - 简化版
/// </summary>
public class Checkpoint : MonoBehaviour
{
    [Header("=== 检查点设置 ===")]
    public string checkpointID;
    public string triggerTag = "Player";
    public bool triggerOnce = false;

    private bool hasTriggered = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(triggerTag)) return;
        if (triggerOnce && hasTriggered) return;

        hasTriggered = true;
        
        string sceneName = SceneManager.GetActiveScene().name;
        GameManager.Instance.SetCheckpoint(sceneName, checkpointID);
        
        Debug.Log($"[Checkpoint] 已保存: {checkpointID}");
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = hasTriggered ? Color.green : Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 0.5f);
    }
}
