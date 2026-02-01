using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.SceneManagement;

/// <summary>
/// 生成点组件
/// </summary>
public class SpawnPoint : MonoBehaviour
{
    [Header("=== 生成点设置 ===")]
    public string spawnPointID;
    public bool isDefaultSpawnPoint = false;

    [Header("=== 相机设置 ===")]
    [Tooltip("要移动的相机组 Parent")]
    public Transform cameraParent;

    [Header("=== 冻结设置 ===")]
    [Tooltip("冻结画面的颜色")]
    public Color freezeColor = Color.black;
    
    [Tooltip("冻结持续帧数")]
    public int freezeFrames = 3;

    private static bool hasSpawnedThisScene = false;
    private static string spawnedSceneName = "";

    private void Start()
    {
        string currentScene = SceneManager.GetActiveScene().name;
        
        Debug.Log($"[SpawnPoint:{spawnPointID}] Start() 被调用，场景: {currentScene}");
        Debug.Log($"[SpawnPoint:{spawnPointID}] hasSpawnedThisScene: {hasSpawnedThisScene}, spawnedSceneName: {spawnedSceneName}");
        
        if (spawnedSceneName != currentScene)
        {
            hasSpawnedThisScene = false;
            spawnedSceneName = currentScene;
            Debug.Log($"[SpawnPoint:{spawnPointID}] 新场景，重置标志");
        }

        bool shouldSpawn = ShouldSpawnHere();
        Debug.Log($"[SpawnPoint:{spawnPointID}] ShouldSpawnHere: {shouldSpawn}, hasSpawnedThisScene: {hasSpawnedThisScene}");
        
        if (!hasSpawnedThisScene && shouldSpawn)
        {
            hasSpawnedThisScene = true;
            Debug.Log($"[SpawnPoint:{spawnPointID}] ▶ 开始生成玩家！");
            StartCoroutine(SpawnWithFreeze());
        }
        else
        {
            string reason = hasSpawnedThisScene ? "已经生成过" : "不是目标生成点";
            Debug.Log($"[SpawnPoint:{spawnPointID}] ✗ 不生成，原因: {reason}");
        }
    }

    private bool ShouldSpawnHere()
    {
        if (GameManager.Instance == null)
        {
            Debug.Log($"[SpawnPoint:{spawnPointID}] GameManager.Instance 为空");
            return false;
        }

        string targetID = GameManager.Instance.targetSpawnID;
        Debug.Log($"[SpawnPoint:{spawnPointID}] 检查: targetID='{targetID}', 我的ID='{spawnPointID}', isDefault={isDefaultSpawnPoint}");
        
        if (!string.IsNullOrEmpty(targetID))
        {
            bool match = (targetID == spawnPointID);
            Debug.Log($"[SpawnPoint:{spawnPointID}] 目标ID匹配: {match}");
            return match;
        }

        Debug.Log($"[SpawnPoint:{spawnPointID}] 无目标ID，使用默认: {isDefaultSpawnPoint}");
        return isDefaultSpawnPoint;
    }

    private IEnumerator SpawnWithFreeze()
    {
        // 1. 创建冻结遮罩
        GameObject freezeCanvas = CreateFreezeCanvas();

        // 2. 移动相机
        if (cameraParent != null)
        {
            float z = cameraParent.position.z;
            cameraParent.position = new Vector3(transform.position.x, transform.position.y, z);
            Debug.Log("[SpawnPoint] 相机已移动");
        }

        // 3. 移动玩家
        GameObject[] allPlayers = GameObject.FindGameObjectsWithTag("Player");
        Debug.Log($"[SpawnPoint:{spawnPointID}] 找到 {allPlayers.Length} 个Player对象");
        foreach (var p in allPlayers)
        {
            Debug.Log($"[SpawnPoint:{spawnPointID}] Player: {p.name}, 位置: {p.transform.position}, 场景: {p.scene.name}");
        }
        
        GameObject player = allPlayers.Length > 0 ? allPlayers[0] : null;
        if (player != null)
        {
            Vector3 oldPos = player.transform.position;
            float z = player.transform.position.z;
            player.transform.position = new Vector3(transform.position.x, transform.position.y, z);
            
            Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
            }
            Debug.Log($"[SpawnPoint] ★ 玩家生成在: {spawnPointID}");
            Debug.Log($"[SpawnPoint] 位置变化: {oldPos} → {player.transform.position}");
            Debug.Log($"[SpawnPoint] SpawnPoint位置: {transform.position}");
        }
        else
        {
            Debug.LogWarning($"[SpawnPoint:{spawnPointID}] 找不到Player!");
        }

        // 4. 等待几帧让一切稳定
        for (int i = 0; i < freezeFrames; i++)
        {
            yield return null;
        }

        // 5. 移除遮罩
        if (freezeCanvas != null)
        {
            Destroy(freezeCanvas);
        }
        
        // 6. 验证最终位置
        if (player != null)
        {
            Debug.Log($"[SpawnPoint:{spawnPointID}] 冻结结束后玩家位置: {player.transform.position}");
        }
    }

    private GameObject CreateFreezeCanvas()
    {
        GameObject canvasObj = new GameObject("FreezeCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 99999;

        GameObject panel = new GameObject("FreezePanel");
        panel.transform.SetParent(canvasObj.transform, false);

        Image img = panel.AddComponent<Image>();
        img.color = freezeColor;
        img.raycastTarget = false;

        RectTransform rt = panel.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        return canvasObj;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = isDefaultSpawnPoint ? Color.blue : Color.green;
        Gizmos.DrawWireSphere(transform.position, 0.5f);
    }

    /// <summary>
    /// 重置生成点静态状态
    /// </summary>
    public static void ResetSpawnPointState()
    {
        hasSpawnedThisScene = false;
        spawnedSceneName = "";
        Debug.Log("[SpawnPoint] 生成点状态已重置");
    }
}
