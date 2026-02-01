using System.Collections.Generic;
using UnityEngine;

public class PlayerMaskManager : MonoBehaviour
{
    public static PlayerMaskManager Instance;

    private HashSet<string> ownedMasks = new HashSet<string>();

    [Header("=== 当前面具 ===")]
    [Tooltip("当前佩戴的面具ID，UI会读取这个值显示图标")]
    public string currentMaskId = "mask_blank";

    [Header("=== 默认设置 ===")]
    [Tooltip("默认/初始面具ID")]
    public string defaultMaskId = "mask_blank";

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else Destroy(gameObject);
    }

    /// <summary>
    /// 解锁面具并自动设为当前面具
    /// </summary>
    public void UnlockMask(string maskId)
    {
        if (string.IsNullOrEmpty(maskId)) return;
        
        ownedMasks.Add(maskId);
        currentMaskId = maskId;  // 自动将当前面具设为新获得的
        Debug.Log("[Mask] Unlocked & set current: " + maskId);
    }

    /// <summary>
    /// 仅解锁面具，不改变当前面具
    /// </summary>
    public void UnlockMaskOnly(string maskId)
    {
        if (string.IsNullOrEmpty(maskId)) return;
        
        ownedMasks.Add(maskId);
        Debug.Log("[Mask] Unlocked (no change to current): " + maskId);
    }

    /// <summary>
    /// 设置当前面具（必须已拥有）
    /// </summary>
    public void SetCurrentMask(string maskId)
    {
        if (string.IsNullOrEmpty(maskId)) return;
        
        if (ownedMasks.Contains(maskId) || maskId == defaultMaskId)
        {
            currentMaskId = maskId;
            Debug.Log("[Mask] Current mask set to: " + maskId);
        }
        else
        {
            Debug.LogWarning("[Mask] Cannot set current mask - not owned: " + maskId);
        }
    }

    public bool HasMask(string maskId)
    {
        return ownedMasks.Contains(maskId);
    }

    /// <summary>
    /// 获取当前面具ID
    /// </summary>
    public string GetCurrentMask()
    {
        return currentMaskId;
    }

    /// <summary>
    /// 重置所有面具数据
    /// </summary>
    public void ResetAllMasks()
    {
        ownedMasks.Clear();
        currentMaskId = defaultMaskId;
        Debug.Log("[Mask] 所有面具已重置，当前面具: " + currentMaskId);
    }

    // 确保在编辑器中重新进入Play Mode时重置静态变量
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        Instance = null;
    }
}