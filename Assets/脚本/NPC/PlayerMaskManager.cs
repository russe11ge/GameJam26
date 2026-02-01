using System.Collections.Generic;
using UnityEngine;

public class PlayerMaskManager : MonoBehaviour
{
    public static PlayerMaskManager Instance;

    private HashSet<string> ownedMasks = new HashSet<string>();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else Destroy(gameObject);
    }

    public void UnlockMask(string maskId)
    {
        ownedMasks.Add(maskId);
        Debug.Log("[Mask] Unlocked: " + maskId);
    }

    public bool HasMask(string maskId)
    {
        return ownedMasks.Contains(maskId);
    }

    /// <summary>
    /// 重置所有面具数据
    /// </summary>
    public void ResetAllMasks()
    {
        ownedMasks.Clear();
        Debug.Log("[Mask] 所有面具已重置");
    }
}