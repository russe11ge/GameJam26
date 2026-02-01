using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 游戏管理器 - 简化版
/// </summary>
public class GameManager : MonoBehaviour
{
    #region 单例（自动创建）
    private static GameManager _instance;
    public static GameManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<GameManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("GameManager");
                    _instance = go.AddComponent<GameManager>();
                }
            }
            return _instance;
        }
    }
    #endregion

    [Header("=== 生成点 ===")]
    public string initialSpawnPointID = "Default";
    public string targetSpawnID;

    [Header("=== 检查点 ===")]
    public string lastCheckpointScene;
    public string lastCheckpointID;

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
            
            if (string.IsNullOrEmpty(targetSpawnID))
            {
                targetSpawnID = initialSpawnPointID;
            }
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }

    public void SetCheckpoint(string scene, string id)
    {
        lastCheckpointScene = scene;
        lastCheckpointID = id;
    }

    public void LoadScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }

    public void ResetGame()
    {
        targetSpawnID = initialSpawnPointID;
        lastCheckpointScene = "";
        lastCheckpointID = "";
        PlayerPrefs.DeleteAll();
    }
}
