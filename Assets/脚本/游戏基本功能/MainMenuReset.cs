using UnityEngine;

/// <summary>
/// 主菜单重置器
/// 放在主菜单场景中，进入时自动重置所有游戏数据
/// </summary>
public class MainMenuReset : MonoBehaviour
{
    [Header("=== 重置选项 ===")]
    [Tooltip("重置 GameManager 数据（玩家值、检查点等）")]
    public bool resetGameManager = true;
    
    [Tooltip("重置 PlayerMaskManager（面具数据）")]
    public bool resetMasks = true;
    
    [Tooltip("重置 NPC 状态（永久消失的 NPC、对话状态）")]
    public bool resetNPCStates = true;
    
    [Tooltip("重置故事揭示触发状态")]
    public bool resetStoryRevealTriggers = true;
    
    [Tooltip("重置生成点状态")]
    public bool resetSpawnPoints = true;
    
    [Tooltip("重置值触发器状态（ValueTrigger）")]
    public bool resetValueTriggers = true;
    
    [Tooltip("重置一次性文字触发器（OneTimeTextTrigger）")]
    public bool resetOneTimeTextTriggers = true;
    
    [Tooltip("清除 PlayerPrefs 存档")]
    public bool clearPlayerPrefs = true;

    [Header("=== Debug ===")]
    public bool enableDebug = true;

    private void Start()
    {
        ResetAllData();
    }

    /// <summary>
    /// 重置所有游戏数据
    /// </summary>
    public void ResetAllData()
    {
        if (enableDebug) Debug.Log("[MainMenuReset] ========== 开始重置所有数据 ==========");

        // 1. 重置 GameManager
        if (resetGameManager)
        {
            ResetGameManagerData();
        }

        // 2. 重置面具
        if (resetMasks)
        {
            ResetMaskData();
        }

        // 3. 重置 NPC 状态
        if (resetNPCStates)
        {
            ResetNPCData();
        }

        // 4. 重置故事揭示触发
        if (resetStoryRevealTriggers)
        {
            ResetStoryRevealData();
        }

        // 5. 重置生成点状态
        if (resetSpawnPoints)
        {
            ResetSpawnPointData();
        }

        // 6. 重置值触发器状态
        if (resetValueTriggers)
        {
            ResetValueTriggerData();
        }

        // 7. 重置一次性文字触发器
        if (resetOneTimeTextTriggers)
        {
            ResetOneTimeTextTriggerData();
        }

        // 8. 清除 PlayerPrefs
        if (clearPlayerPrefs)
        {
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
            if (enableDebug) Debug.Log("[MainMenuReset] PlayerPrefs 已清除");
        }

        if (enableDebug) Debug.Log("[MainMenuReset] ========== 所有数据已重置 ==========");
    }

    private void ResetGameManagerData()
    {
        if (GameManager.Instance != null)
        {
            // 重置生成点
            GameManager.Instance.targetSpawnID = GameManager.Instance.initialSpawnPointID;
            
            // 重置检查点
            GameManager.Instance.lastCheckpointScene = "";
            GameManager.Instance.lastCheckpointID = "";
            
            // 重置所有玩家值
            GameManager.Instance.ResetAllPlayerValues();
            
            if (enableDebug) Debug.Log("[MainMenuReset] GameManager 数据已重置");
        }
        else
        {
            if (enableDebug) Debug.Log("[MainMenuReset] GameManager.Instance 为 null，跳过");
        }
    }

    private void ResetMaskData()
    {
        if (PlayerMaskManager.Instance != null)
        {
            PlayerMaskManager.Instance.ResetAllMasks();
            if (enableDebug) Debug.Log("[MainMenuReset] 面具数据已重置");
        }
        else
        {
            if (enableDebug) Debug.Log("[MainMenuReset] PlayerMaskManager.Instance 为 null，跳过");
        }
    }

    private void ResetNPCData()
    {
        // 重置永久消失的 NPC
        NPCConditionalDialogue.ResetAllDisappearedNPCs();
        if (enableDebug) Debug.Log("[MainMenuReset] NPC 永久消失状态已重置");
    }

    private void ResetStoryRevealData()
    {
        // 重置故事揭示触发状态
        NPCStoryRevealTrigger.ResetAllTriggers();
        if (enableDebug) Debug.Log("[MainMenuReset] 故事揭示触发状态已重置");
    }

    private void ResetSpawnPointData()
    {
        // 重置生成点状态
        SpawnPoint.ResetSpawnPointState();
        if (enableDebug) Debug.Log("[MainMenuReset] 生成点状态已重置");
    }

    private void ResetValueTriggerData()
    {
        // 重置值触发器状态
        ValueTrigger.ResetAllTriggers();
        if (enableDebug) Debug.Log("[MainMenuReset] 值触发器状态已重置");
    }

    private void ResetOneTimeTextTriggerData()
    {
        // 重置一次性文字触发器状态
        OneTimeTextTrigger.ResetAllTriggers();
        if (enableDebug) Debug.Log("[MainMenuReset] 一次性文字触发器状态已重置");
    }
}
