using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 游戏管理器 - 单例模式，跨场景保留
/// 管理游戏状态、检查点记录、场景切换
/// </summary>
public class GameManager : MonoBehaviour
{
    #region 单例
    public static GameManager Instance { get; private set; }
    #endregion

    #region Inspector设置
    [Header("=== 初始设置 ===")]
    [Tooltip("游戏开始时的默认出生点ID")]
    public string initialSpawnPointID = "StartPoint";

    [Header("=== 游戏状态 ===")]
    [Tooltip("游戏是否暂停")]
    public bool isPaused = false;
    #endregion

    #region 检查点数据
    [Header("=== 检查点数据（运行时）===")]
    [Tooltip("最后一个检查点的场景名")]
    public string lastCheckpointScene;
    
    [Tooltip("最后一个检查点的ID")]
    public string lastCheckpointID;
    
    [Tooltip("当前目标生成点ID")]
    public string targetSpawnID;
    #endregion

    #region 游戏进度数据（可自定义扩展）
    [Header("=== 游戏进度 ===")]
    [Tooltip("已收集的物品数量")]
    public int collectedItems = 0;
    
    [Tooltip("已完成的对话ID列表")]
    public System.Collections.Generic.List<string> completedDialogues = new System.Collections.Generic.List<string>();
    #endregion

    #region Unity生命周期
    private void Awake()
    {
        // 单例模式
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            // 初始化目标生成点
            if (string.IsNullOrEmpty(targetSpawnID))
            {
                targetSpawnID = initialSpawnPointID;
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        Time.timeScale = 1f;
    }
    #endregion

    #region ========== 检查点系统 ==========
    
    /// <summary>
    /// 设置检查点（当玩家到达检查点时调用）
    /// </summary>
    /// <param name="sceneName">场景名</param>
    /// <param name="checkpointID">检查点ID</param>
    public void SetCheckpoint(string sceneName, string checkpointID)
    {
        lastCheckpointScene = sceneName;
        lastCheckpointID = checkpointID;
        
        Debug.Log($"[GameManager] 检查点已保存: 场景={sceneName}, ID={checkpointID}");
        
        // 可选：保存到PlayerPrefs持久化
        SaveCheckpointToPrefs();
    }

    /// <summary>
    /// 获取最后的检查点信息
    /// </summary>
    public (string scene, string id) GetLastCheckpoint()
    {
        return (lastCheckpointScene, lastCheckpointID);
    }

    /// <summary>
    /// 是否有保存的检查点
    /// </summary>
    public bool HasCheckpoint()
    {
        return !string.IsNullOrEmpty(lastCheckpointScene) && !string.IsNullOrEmpty(lastCheckpointID);
    }

    /// <summary>
    /// 清除检查点记录
    /// </summary>
    public void ClearCheckpoint()
    {
        lastCheckpointScene = "";
        lastCheckpointID = "";
        targetSpawnID = initialSpawnPointID;
        
        PlayerPrefs.DeleteKey("CheckpointScene");
        PlayerPrefs.DeleteKey("CheckpointID");
        PlayerPrefs.Save();
        
        Debug.Log("[GameManager] 检查点已清除");
    }
    #endregion

    #region ========== 场景切换 ==========
    
    /// <summary>
    /// 加载场景（通过名称）
    /// </summary>
    public void LoadScene(string sceneName)
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(sceneName);
    }

    /// <summary>
    /// 加载场景（通过索引）
    /// </summary>
    public void LoadSceneByIndex(int sceneIndex)
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(sceneIndex);
    }

    /// <summary>
    /// 加载场景并指定生成点
    /// </summary>
    public void LoadSceneWithSpawnPoint(string sceneName, string spawnPointID)
    {
        targetSpawnID = spawnPointID;
        LoadScene(sceneName);
    }

    /// <summary>
    /// 重新加载当前场景
    /// </summary>
    public void ReloadCurrentScene()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    /// <summary>
    /// 返回上一个检查点场景
    /// </summary>
    public void ReturnToLastCheckpoint()
    {
        if (HasCheckpoint())
        {
            targetSpawnID = lastCheckpointID;
            LoadScene(lastCheckpointScene);
        }
        else
        {
            Debug.LogWarning("[GameManager] 没有保存的检查点！");
            ReloadCurrentScene();
        }
    }
    #endregion

    #region ========== 游戏控制 ==========
    
    /// <summary>
    /// 暂停游戏
    /// </summary>
    public void PauseGame()
    {
        isPaused = true;
        Time.timeScale = 0f;
    }

    /// <summary>
    /// 恢复游戏
    /// </summary>
    public void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = 1f;
    }

    /// <summary>
    /// 重置游戏（清除所有进度）
    /// </summary>
    public void ResetGame()
    {
        // 清除检查点
        ClearCheckpoint();
        
        // 清除进度
        collectedItems = 0;
        completedDialogues.Clear();
        
        // 清除PlayerPrefs
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
        
        Debug.Log("[GameManager] 游戏已重置");
    }

    /// <summary>
    /// 退出游戏
    /// </summary>
    public void QuitGame()
    {
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }
    #endregion

    #region ========== 进度记录 ==========
    
    /// <summary>
    /// 记录已完成的对话
    /// </summary>
    public void MarkDialogueCompleted(string dialogueID)
    {
        if (!completedDialogues.Contains(dialogueID))
        {
            completedDialogues.Add(dialogueID);
        }
    }

    /// <summary>
    /// 检查对话是否已完成
    /// </summary>
    public bool IsDialogueCompleted(string dialogueID)
    {
        return completedDialogues.Contains(dialogueID);
    }

    /// <summary>
    /// 增加收集物品数量
    /// </summary>
    public void AddCollectedItem(int amount = 1)
    {
        collectedItems += amount;
    }
    #endregion

    #region ========== 数据持久化 ==========
    
    /// <summary>
    /// 保存检查点到PlayerPrefs
    /// </summary>
    private void SaveCheckpointToPrefs()
    {
        PlayerPrefs.SetString("CheckpointScene", lastCheckpointScene);
        PlayerPrefs.SetString("CheckpointID", lastCheckpointID);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// 从PlayerPrefs加载检查点
    /// </summary>
    public void LoadCheckpointFromPrefs()
    {
        lastCheckpointScene = PlayerPrefs.GetString("CheckpointScene", "");
        lastCheckpointID = PlayerPrefs.GetString("CheckpointID", "");
        
        if (!string.IsNullOrEmpty(lastCheckpointID))
        {
            targetSpawnID = lastCheckpointID;
        }
    }
    #endregion
}
