using UnityEngine;

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

    [Tooltip("是否是默认生成点（场景中只能有一个）")]
    public bool isDefaultSpawnPoint = false;

    [Header("=== 玩家设置 ===")]
    [Tooltip("玩家标签")]
    public string playerTag = "Player";

    private void Start()
    {
        TrySpawnPlayer();
    }

    /// <summary>
    /// 尝试在此生成点生成玩家
    /// </summary>
    private void TrySpawnPlayer()
    {
        // 检查GameManager
        if (GameManager.Instance == null)
        {
            // 如果没有GameManager，检查是否是默认生成点
            if (isDefaultSpawnPoint)
            {
                SpawnPlayerHere();
            }
            return;
        }

        // 检查是否是目标生成点
        bool shouldSpawn = false;

        // 优先检查targetSpawnID
        if (!string.IsNullOrEmpty(GameManager.Instance.targetSpawnID))
        {
            if (GameManager.Instance.targetSpawnID == spawnPointID)
            {
                shouldSpawn = true;
            }
        }
        // 如果没有指定targetSpawnID，使用默认生成点
        else if (isDefaultSpawnPoint)
        {
            shouldSpawn = true;
        }

        if (shouldSpawn)
        {
            SpawnPlayerHere();
        }
    }

    /// <summary>
    /// 将玩家传送到此生成点
    /// </summary>
    private void SpawnPlayerHere()
    {
        GameObject player = GameObject.FindGameObjectWithTag(playerTag);
        
        if (player != null)
        {
            // 设置玩家位置
            player.transform.position = transform.position;
            
            // 重置玩家速度（如果有Rigidbody2D）
            Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
            }
            
            Debug.Log($"[SpawnPoint] 玩家已生成在: {spawnPointID} ({transform.position})");
            
            // 触发事件
            OnPlayerSpawned?.Invoke(spawnPointID, transform.position);
        }
        else
        {
            Debug.LogWarning($"[SpawnPoint] 未找到标签为 '{playerTag}' 的玩家！");
        }
    }

    /// <summary>
    /// 手动将玩家传送到此生成点
    /// </summary>
    public void TeleportPlayerHere()
    {
        SpawnPlayerHere();
    }

    /// <summary>
    /// 玩家生成事件
    /// </summary>
    public static event System.Action<string, Vector3> OnPlayerSpawned;

    #region Editor
    // 在编辑器中绘制图标
    private void OnDrawGizmos()
    {
        // 默认生成点用蓝色，其他用绿色
        Gizmos.color = isDefaultSpawnPoint ? Color.blue : Color.green;
        Gizmos.DrawWireSphere(transform.position, 0.5f);
        
        // 绘制方向箭头（表示玩家朝向）
        Gizmos.DrawLine(transform.position, transform.position + Vector3.up * 0.8f);
        
        // 绘制标签
        #if UNITY_EDITOR
        string label = isDefaultSpawnPoint ? $"[默认] {spawnPointID}" : spawnPointID;
        UnityEditor.Handles.Label(transform.position + Vector3.up * 1f, label);
        #endif
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(transform.position, new Vector3(1f, 2f, 0.1f));
    }
    #endregion
}
