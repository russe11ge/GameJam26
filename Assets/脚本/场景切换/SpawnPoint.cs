using UnityEngine;
using System.Collections;

/// <summary>
/// 生成点组件
/// 在场景加载时，将玩家传送到指定的生成点
/// 使用方法：添加到空物体上，设置唯一的spawnPointID
/// </summary>
public class SpawnPoint : MonoBehaviour
{
    [Header("=== 生成点设置 ===")]
    [Tooltip("此生成点的唯一ID")]
    public string spawnPointID;

    [Tooltip("是否是默认生成点（当没有指定目标时使用）")]
    public bool isDefaultSpawnPoint = false;

    [Header("=== 玩家设置 ===")]
    [Tooltip("玩家标签")]
    public string playerTag = "Player";
    
    [Tooltip("延迟生成时间（秒），0表示立即")]
    public float spawnDelay = 0.1f;

    private static bool hasSpawnedThisScene = false;

    private void Awake()
    {
        // 每次场景加载重置标志
        hasSpawnedThisScene = false;
    }

    private void Start()
    {
        // 检查是否应该在此生成点生成玩家
        if (ShouldSpawnHere())
        {
            if (spawnDelay > 0)
            {
                StartCoroutine(DelayedSpawn());
            }
            else
            {
                SpawnPlayerHere();
            }
        }
    }

    /// <summary>
    /// 检查是否应该在此生成点生成玩家
    /// </summary>
    private bool ShouldSpawnHere()
    {
        // 如果已经在这个场景生成过，跳过
        if (hasSpawnedThisScene) return false;

        // 如果有GameManager
        if (GameManager.Instance != null)
        {
            string targetID = GameManager.Instance.targetSpawnID;
            
            // 如果指定了目标生成点ID
            if (!string.IsNullOrEmpty(targetID))
            {
                // 只有ID匹配才生成
                return targetID == spawnPointID;
            }
        }

        // 如果没有GameManager或没有指定目标，使用默认生成点
        return isDefaultSpawnPoint;
    }

    /// <summary>
    /// 延迟生成
    /// </summary>
    private IEnumerator DelayedSpawn()
    {
        yield return new WaitForSeconds(spawnDelay);
        SpawnPlayerHere();
    }

    /// <summary>
    /// 将玩家传送到此生成点
    /// </summary>
    private void SpawnPlayerHere()
    {
        // 防止重复生成
        if (hasSpawnedThisScene) return;
        hasSpawnedThisScene = true;

        GameObject player = GameObject.FindGameObjectWithTag(playerTag);
        
        if (player == null)
        {
            Debug.LogWarning($"[SpawnPoint] 未找到标签为 '{playerTag}' 的玩家！");
            return;
        }

        // 暂时禁用玩家物理（防止生成时掉落）
        Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
        bool wasKinematic = false;
        
        if (rb != null)
        {
            wasKinematic = rb.isKinematic;
            rb.isKinematic = true;
            rb.linearVelocity = Vector2.zero;
        }

        // 设置玩家位置（只修改X和Y，保留原来的Z轴）
        float originalZ = player.transform.position.z;
        player.transform.position = new Vector3(transform.position.x, transform.position.y, originalZ);

        // 恢复物理
        if (rb != null)
        {
            rb.isKinematic = wasKinematic;
        }

        Debug.Log($"[SpawnPoint] 玩家已生成在: {spawnPointID} ({transform.position})");

        // 清除GameManager中的目标生成点（防止下次场景加载时重复使用）
        if (GameManager.Instance != null)
        {
            // 可选：清除targetSpawnID，或者保留以便返回时使用
            // GameManager.Instance.targetSpawnID = "";
        }

        // 触发事件
        OnPlayerSpawned?.Invoke(spawnPointID, transform.position);
    }

    /// <summary>
    /// 手动将玩家传送到此生成点（可从其他脚本调用）
    /// </summary>
    public void TeleportPlayerHere()
    {
        hasSpawnedThisScene = false; // 允许手动传送
        SpawnPlayerHere();
    }

    /// <summary>
    /// 强制在此生成点生成玩家（忽略其他检查）
    /// </summary>
    public void ForceSpawnHere()
    {
        hasSpawnedThisScene = false;
        
        GameObject player = GameObject.FindGameObjectWithTag(playerTag);
        if (player != null)
        {
            // 只修改X和Y，保留原来的Z轴
            float originalZ = player.transform.position.z;
            player.transform.position = new Vector3(transform.position.x, transform.position.y, originalZ);
            
            Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
            }
            
            hasSpawnedThisScene = true;
            Debug.Log($"[SpawnPoint] 强制生成玩家在: {spawnPointID}");
        }
    }

    /// <summary>
    /// 玩家生成事件
    /// </summary>
    public static event System.Action<string, Vector3> OnPlayerSpawned;

    /// <summary>
    /// 重置生成标志（用于重新加载场景时）
    /// </summary>
    public static void ResetSpawnFlag()
    {
        hasSpawnedThisScene = false;
    }

    #region Editor
    private void OnDrawGizmos()
    {
        // 默认生成点用蓝色，其他用绿色
        Gizmos.color = isDefaultSpawnPoint ? Color.blue : Color.green;
        Gizmos.DrawWireSphere(transform.position, 0.3f);
        
        // 绘制十字标记
        Gizmos.DrawLine(transform.position + Vector3.left * 0.3f, transform.position + Vector3.right * 0.3f);
        Gizmos.DrawLine(transform.position + Vector3.down * 0.3f, transform.position + Vector3.up * 0.5f);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(transform.position + Vector3.up * 0.5f, new Vector3(0.8f, 1.6f, 0.1f));
        
        #if UNITY_EDITOR
        string label = isDefaultSpawnPoint ? $"[默认] {spawnPointID}" : spawnPointID;
        UnityEditor.Handles.Label(transform.position + Vector3.up * 1.5f, label);
        #endif
    }
    #endregion
}
