using System.Collections.Generic;
using UnityEngine;

public class PlayerMaskManager : MonoBehaviour
{
    #region 单例（自动创建）
    private static PlayerMaskManager _instance;
    public static PlayerMaskManager Instance
    {
        get
        {
            if (_instance == null)
            {
                // 先尝试在场景中查找
                _instance = FindAnyObjectByType<PlayerMaskManager>();
                
                // 如果找不到，自动创建一个
                if (_instance == null)
                {
                    GameObject go = new GameObject("PlayerMaskManager");
                    _instance = go.AddComponent<PlayerMaskManager>();
                    Debug.Log("[PlayerMaskManager] 自动创建实例");
                }
            }
            return _instance;
        }
    }
    #endregion

    private HashSet<string> ownedMasks = new HashSet<string>();

    [Header("=== 当前面具 ===")]
    [Tooltip("当前佩戴的面具ID，UI会读取这个值显示图标")]
    public string currentMaskId = "Mask1";

    [Header("=== 默认设置 ===")]
    [Tooltip("默认/初始面具ID（重启游戏或进入主菜单时重置为此值）")]
    public string defaultMaskId = "Mask1";

    [Header("=== Debug ===")]
    public bool enableDebug = true;

    void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            
            // 确保是根对象才能 DontDestroyOnLoad
            if (transform.parent != null)
            {
                if (enableDebug) Debug.Log("[PlayerMaskManager] 检测到父对象，解除父子关系");
                transform.SetParent(null);
            }
            
            DontDestroyOnLoad(gameObject);
            if (enableDebug) Debug.Log("[PlayerMaskManager] 初始化成功，当前面具: " + currentMaskId);
        }
        else if (_instance != this)
        {
            if (enableDebug) Debug.Log("[PlayerMaskManager] 已存在实例，销毁重复的");
            Destroy(gameObject);
        }
    }

    void Start()
    {
        if (enableDebug) PrintStatus();
    }

    /// <summary>
    /// 解锁面具并自动设为当前面具
    /// </summary>
    public void UnlockMask(string maskId)
    {
        if (string.IsNullOrEmpty(maskId))
        {
            if (enableDebug) Debug.LogWarning("[PlayerMaskManager] UnlockMask 失败：maskId 为空");
            return;
        }
        
        bool isNew = !ownedMasks.Contains(maskId);
        ownedMasks.Add(maskId);
        string oldMask = currentMaskId;
        currentMaskId = maskId;
        
        if (enableDebug)
        {
            Debug.Log($"[PlayerMaskManager] UnlockMask: {maskId} (新面具: {isNew})");
            Debug.Log($"[PlayerMaskManager] 当前面具: {oldMask} → {currentMaskId}");
            PrintStatus();
        }
    }

    /// <summary>
    /// 仅解锁面具，不改变当前面具
    /// </summary>
    public void UnlockMaskOnly(string maskId)
    {
        if (string.IsNullOrEmpty(maskId))
        {
            if (enableDebug) Debug.LogWarning("[PlayerMaskManager] UnlockMaskOnly 失败：maskId 为空");
            return;
        }
        
        bool isNew = !ownedMasks.Contains(maskId);
        ownedMasks.Add(maskId);
        
        if (enableDebug)
        {
            Debug.Log($"[PlayerMaskManager] UnlockMaskOnly: {maskId} (新面具: {isNew})");
            PrintStatus();
        }
    }

    /// <summary>
    /// 设置当前面具（必须已拥有）
    /// </summary>
    public void SetCurrentMask(string maskId)
    {
        if (string.IsNullOrEmpty(maskId))
        {
            if (enableDebug) Debug.LogWarning("[PlayerMaskManager] SetCurrentMask 失败：maskId 为空");
            return;
        }
        
        if (ownedMasks.Contains(maskId) || maskId == defaultMaskId)
        {
            string oldMask = currentMaskId;
            currentMaskId = maskId;
            if (enableDebug) Debug.Log($"[PlayerMaskManager] SetCurrentMask: {oldMask} → {maskId}");
        }
        else
        {
            if (enableDebug) Debug.LogWarning($"[PlayerMaskManager] SetCurrentMask 失败：未拥有面具 {maskId}");
        }
    }

    public bool HasMask(string maskId)
    {
        bool has = ownedMasks.Contains(maskId);
        if (enableDebug) Debug.Log($"[PlayerMaskManager] HasMask({maskId}): {has}");
        return has;
    }

    /// <summary>
    /// 获取当前面具ID
    /// </summary>
    public string GetCurrentMask()
    {
        if (enableDebug) Debug.Log($"[PlayerMaskManager] GetCurrentMask: {currentMaskId}");
        return currentMaskId;
    }

    /// <summary>
    /// 获取拥有的面具数量
    /// </summary>
    public int GetOwnedMaskCount()
    {
        return ownedMasks.Count;
    }

    /// <summary>
    /// 打印当前状态
    /// </summary>
    public void PrintStatus()
    {
        Debug.Log("========== [PlayerMaskManager] 状态 ==========");
        Debug.Log($"当前佩戴面具: {currentMaskId}");
        Debug.Log($"拥有面具数量: {ownedMasks.Count}");
        if (ownedMasks.Count > 0)
        {
            Debug.Log("拥有的面具列表:");
            foreach (var mask in ownedMasks)
            {
                Debug.Log($"  - {mask}");
            }
        }
        Debug.Log("==============================================");
    }

    /// <summary>
    /// 重置所有面具数据
    /// </summary>
    public void ResetAllMasks()
    {
        ownedMasks.Clear();
        currentMaskId = defaultMaskId;
        if (enableDebug)
        {
            Debug.Log("[PlayerMaskManager] 所有面具已重置");
            PrintStatus();
        }
    }

    // 确保在编辑器中重新进入Play Mode时重置静态变量
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        _instance = null;
    }
}