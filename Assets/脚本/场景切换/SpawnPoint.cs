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
        if (spawnedSceneName != currentScene)
        {
            hasSpawnedThisScene = false;
            spawnedSceneName = currentScene;
        }

        if (!hasSpawnedThisScene && ShouldSpawnHere())
        {
            hasSpawnedThisScene = true;
            StartCoroutine(SpawnWithFreeze());
        }
    }

    private bool ShouldSpawnHere()
    {
        if (GameManager.Instance == null) return false;

        string targetID = GameManager.Instance.targetSpawnID;
        
        if (!string.IsNullOrEmpty(targetID))
        {
            return (targetID == spawnPointID);
        }

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
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            float z = player.transform.position.z;
            player.transform.position = new Vector3(transform.position.x, transform.position.y, z);
            
            Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
            }
            Debug.Log($"[SpawnPoint] ★ 玩家生成在: {spawnPointID}");
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
}
