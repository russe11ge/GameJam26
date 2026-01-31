using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 检查点组件
/// 当玩家进入触发器时，记录当前位置作为检查点
/// 使用方法：添加到带有Collider2D（勾选IsTrigger）的物体上
/// </summary>
public class Checkpoint : MonoBehaviour
{
    [Header("=== 检查点设置 ===")]
    [Tooltip("此检查点的唯一ID（用于识别不同检查点）")]
    public string checkpointID;

    [Tooltip("可触发的标签（默认Player）")]
    public string triggerTag = "Player";

    [Header("=== 可选功能 ===")]
    [Tooltip("触发时是否显示提示")]
    public bool showNotification = true;
    
    [Tooltip("是否只能触发一次")]
    public bool triggerOnce = false;
    
    [Tooltip("触发时播放的音效（可选）")]
    public AudioClip activateSound;

    [Header("=== 视觉效果 ===")]
    [Tooltip("激活后改变颜色（可选）")]
    public SpriteRenderer spriteRenderer;
    
    [Tooltip("激活后的颜色")]
    public Color activatedColor = Color.green;

    private bool hasTriggered = false;
    private AudioSource audioSource;

    private void Awake()
    {
        // 自动生成ID（如果未设置）
        if (string.IsNullOrEmpty(checkpointID))
        {
            checkpointID = $"Checkpoint_{gameObject.name}_{transform.position.x}_{transform.position.y}";
        }

        // 获取或添加AudioSource
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && activateSound != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 检查标签
        if (!string.IsNullOrEmpty(triggerTag) && !other.CompareTag(triggerTag))
        {
            return;
        }

        // 检查是否只触发一次
        if (triggerOnce && hasTriggered)
        {
            return;
        }

        // 检查GameManager是否存在
        if (GameManager.Instance == null)
        {
            Debug.LogError("[Checkpoint] GameManager实例未找到！请确保场景中有GameManager。");
            return;
        }

        // 激活检查点
        ActivateCheckpoint();
    }

    /// <summary>
    /// 激活检查点
    /// </summary>
    private void ActivateCheckpoint()
    {
        hasTriggered = true;

        // 记录检查点到GameManager
        string currentScene = SceneManager.GetActiveScene().name;
        GameManager.Instance.SetCheckpoint(currentScene, checkpointID);

        // 播放音效
        if (activateSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(activateSound);
        }

        // 改变颜色
        if (spriteRenderer != null)
        {
            spriteRenderer.color = activatedColor;
        }

        // 显示提示
        if (showNotification)
        {
            Debug.Log($"[Checkpoint] 检查点已激活: {checkpointID}");
            // 这里可以添加UI提示
        }

        // 触发事件
        OnCheckpointActivated?.Invoke(checkpointID);
    }

    /// <summary>
    /// 手动激活检查点（可从其他脚本调用）
    /// </summary>
    public void ManualActivate()
    {
        if (triggerOnce && hasTriggered) return;
        ActivateCheckpoint();
    }

    /// <summary>
    /// 重置检查点状态
    /// </summary>
    public void ResetCheckpoint()
    {
        hasTriggered = false;
        
        // 恢复原来的颜色
        if (spriteRenderer != null)
        {
            // 注意：这里需要你存储原始颜色或设置一个默认颜色
        }
    }

    /// <summary>
    /// 检查点激活事件
    /// </summary>
    public static event System.Action<string> OnCheckpointActivated;

    #region Editor
    // 在编辑器中绘制图标
    private void OnDrawGizmos()
    {
        Gizmos.color = hasTriggered ? Color.green : Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 0.5f);
        
        // 绘制标签
        #if UNITY_EDITOR
        UnityEditor.Handles.Label(transform.position + Vector3.up * 0.7f, 
            string.IsNullOrEmpty(checkpointID) ? "Checkpoint" : checkpointID);
        #endif
    }
    #endregion
}
