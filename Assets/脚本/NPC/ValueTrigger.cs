using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 值触发器 - 玩家碰到后设置指定的玩家值
/// </summary>
public class ValueTrigger : MonoBehaviour
{
    [Header("=== 触发设置 ===")]
    [Tooltip("触发器唯一ID（用于记录触发状态）")]
    public string triggerID;
    
    [Tooltip("要设置的值名称")]
    public string valueKey = "value1";
    
    [Tooltip("触发后设置的值")]
    public int valueToSet = 1;
    
    [Tooltip("只触发一次")]
    public bool triggerOnce = true;
    
    [Tooltip("触发后隐藏此物体")]
    public bool hideAfterTrigger = false;

    [Header("=== Debug ===")]
    public bool enableDebug = true;

    // 记录已触发的触发器（跨场景保留）
    private static HashSet<string> triggeredIDs = new HashSet<string>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        // 域重载时不清除，保持游戏会话内的状态
    }

    private void Start()
    {
        // 检查是否已触发过（用于场景重新加载后恢复状态）
        if (triggerOnce && !string.IsNullOrEmpty(triggerID) && triggeredIDs.Contains(triggerID))
        {
            if (hideAfterTrigger)
            {
                gameObject.SetActive(false);
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        
        // 检查是否已触发
        if (triggerOnce && !string.IsNullOrEmpty(triggerID) && triggeredIDs.Contains(triggerID))
        {
            return;
        }

        // 标记已触发
        if (triggerOnce && !string.IsNullOrEmpty(triggerID))
        {
            triggeredIDs.Add(triggerID);
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetPlayerValue(valueKey, valueToSet);
            
            if (enableDebug)
            {
                Debug.Log($"[ValueTrigger:{gameObject.name}] 玩家触发！设置 '{valueKey}' = {valueToSet}");
            }
        }
        else
        {
            Debug.LogError($"[ValueTrigger:{gameObject.name}] GameManager.Instance 为 null！");
        }

        if (hideAfterTrigger)
        {
            gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 重置所有触发器状态
    /// </summary>
    public static void ResetAllTriggers()
    {
        triggeredIDs.Clear();
        Debug.Log("[ValueTrigger] 所有触发器状态已重置");
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        BoxCollider2D box = GetComponent<BoxCollider2D>();
        if (box != null)
        {
            Gizmos.DrawWireCube(transform.position + (Vector3)box.offset, box.size);
        }
        else
        {
            Gizmos.DrawWireSphere(transform.position, 0.5f);
        }
    }
}
