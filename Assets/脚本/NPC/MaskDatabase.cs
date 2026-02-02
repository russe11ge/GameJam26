using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 面具数据库 - 存储所有面具的ID和图标
/// 使用 DontDestroyOnLoad 跨场景保持
/// </summary>
public class MaskDatabase : MonoBehaviour
{
    #region 单例（自动查找）
    private static MaskDatabase _instance;
    public static MaskDatabase Instance
    {
        get
        {
            if (_instance == null)
            {
                // 尝试在场景中查找（MaskDatabase需要配置数据，不能自动创建空的）
                _instance = FindAnyObjectByType<MaskDatabase>();
                
                if (_instance == null)
                {
                    Debug.LogWarning("[MaskDatabase] 场景中没有找到 MaskDatabase！请确保在开始界面场景中放置了 MaskDatabase 对象。");
                }
            }
            return _instance;
        }
    }
    #endregion

    [Serializable]
    public class MaskEntry
    {
        [Tooltip("面具唯一ID")]
        public string maskId;
        [Tooltip("面具图标")]
        public Sprite icon;
    }

    [Header("=== 面具列表 ===")]
    [Tooltip("所有面具的ID和图标配置")]
    public List<MaskEntry> masks = new List<MaskEntry>();

    private Dictionary<string, Sprite> lookup = new Dictionary<string, Sprite>();

    void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            
            // 确保是根对象才能 DontDestroyOnLoad
            if (transform.parent != null)
            {
                Debug.Log("[MaskDatabase] 检测到父对象，解除父子关系");
                transform.SetParent(null);
            }
            
            DontDestroyOnLoad(gameObject);
            BuildLookup();
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 构建查找字典
    /// </summary>
    private void BuildLookup()
    {
        lookup.Clear();
        foreach (var m in masks)
        {
            if (m == null || string.IsNullOrEmpty(m.maskId)) continue;
            lookup[m.maskId] = m.icon;
        }
        Debug.Log("[MaskDatabase] 已加载 " + lookup.Count + " 个面具");
    }

    /// <summary>
    /// 根据面具ID获取图标
    /// </summary>
    public Sprite GetIcon(string maskId)
    {
        if (string.IsNullOrEmpty(maskId)) return null;
        return lookup.TryGetValue(maskId, out var s) ? s : null;
    }

    /// <summary>
    /// 检查面具ID是否存在于数据库
    /// </summary>
    public bool HasMaskInDatabase(string maskId)
    {
        return !string.IsNullOrEmpty(maskId) && lookup.ContainsKey(maskId);
    }

    /// <summary>
    /// 重新构建查找字典（如果在运行时修改了masks列表）
    /// </summary>
    public void RefreshDatabase()
    {
        BuildLookup();
    }

    // 确保在编辑器中重新进入Play Mode时重置静态变量
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        _instance = null;
    }
}
