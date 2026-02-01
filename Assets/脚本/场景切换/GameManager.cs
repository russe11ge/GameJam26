using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

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
                _instance = FindAnyObjectByType<GameManager>();
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

    [Header("=== 玩家值 ===")]
    [Tooltip("存储多个玩家值（key: 值名称, value: 数值）")]
    private Dictionary<string, int> playerValues = new Dictionary<string, int>();

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

    #region 玩家值系统
    
    /// <summary>
    /// 获取玩家值（不存在则返回0）
    /// </summary>
    public int GetPlayerValue(string key)
    {
        if (playerValues.TryGetValue(key, out int value))
        {
            return value;
        }
        return 0;
    }

    /// <summary>
    /// 设置玩家值
    /// </summary>
    public void SetPlayerValue(string key, int value)
    {
        playerValues[key] = value;
        Debug.Log($"[GameManager] 玩家值 '{key}' 设为 {value}");
    }

    /// <summary>
    /// 检查玩家值是否等于指定值
    /// </summary>
    public bool CheckPlayerValue(string key, int expectedValue)
    {
        return GetPlayerValue(key) == expectedValue;
    }

    /// <summary>
    /// 重置所有玩家值
    /// </summary>
    public void ResetAllPlayerValues()
    {
        playerValues.Clear();
        Debug.Log("[GameManager] 所有玩家值已重置");
    }

    #endregion

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
        ResetAllPlayerValues();
        PlayerPrefs.DeleteAll();
    }
}
