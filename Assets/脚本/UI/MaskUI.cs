using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 面具UI显示组件 - 实时显示当前佩戴的面具图标
/// 自动从 PlayerMaskManager 和 MaskDatabase 获取信息
/// 每个场景都可以放置，会自动处理跨场景的引用
/// </summary>
public class MaskUI : MonoBehaviour
{
    [Header("=== UI 引用 ===")]
    [Tooltip("显示面具图标的 Image 组件")]
    public Image iconImage;

    [Header("=== 设置 ===")]
    [Tooltip("当没有图标时是否隐藏 Image")]
    public bool hideWhenNoIcon = true;

    [Tooltip("启用调试日志")]
    public bool enableDebug = false;

    private string lastMaskId = "";

    void Awake()
    {
        if (iconImage == null)
        {
            iconImage = GetComponent<Image>();
        }
    }

    void Start()
    {
        // 初始化时立即更新一次
        ForceUpdate();
    }

    void Update()
    {
        // 检查单例是否存在
        if (PlayerMaskManager.Instance == null || MaskDatabase.Instance == null)
        {
            return;
        }

        string current = PlayerMaskManager.Instance.currentMaskId;
        
        // 只有当面具ID改变时才更新
        if (current == lastMaskId) return;

        UpdateMaskIcon(current);
    }

    /// <summary>
    /// 更新面具图标显示
    /// </summary>
    private void UpdateMaskIcon(string maskId)
    {
        lastMaskId = maskId;

        if (MaskDatabase.Instance == null)
        {
            if (enableDebug) Debug.Log("[MaskUI] MaskDatabase.Instance 为空");
            return;
        }

        Sprite sprite = MaskDatabase.Instance.GetIcon(maskId);
        
        if (iconImage != null)
        {
            iconImage.sprite = sprite;
            
            if (hideWhenNoIcon)
            {
                iconImage.enabled = (sprite != null);
            }
        }

        if (enableDebug)
        {
            Debug.Log("[MaskUI] 更新面具图标: " + maskId + " -> " + (sprite != null ? "有图标" : "无图标"));
        }
    }

    /// <summary>
    /// 强制更新面具图标（场景加载后调用）
    /// </summary>
    public void ForceUpdate()
    {
        lastMaskId = ""; // 重置以强制更新
        
        if (PlayerMaskManager.Instance != null)
        {
            UpdateMaskIcon(PlayerMaskManager.Instance.currentMaskId);
        }
    }

    /// <summary>
    /// 手动设置图标（用于特殊情况）
    /// </summary>
    public void SetIcon(Sprite sprite)
    {
        if (iconImage != null)
        {
            iconImage.sprite = sprite;
            iconImage.enabled = (sprite != null) || !hideWhenNoIcon;
        }
    }
}
