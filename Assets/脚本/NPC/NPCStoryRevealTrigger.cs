using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// NPC 故事揭示触发器
/// 挂在 NPC 上，对话完成后触发 StoryRevealManager
/// </summary>
public class NPCStoryRevealTrigger : MonoBehaviour
{
    [Header("=== 触发设置 ===")]
    [Tooltip("此 NPC 的唯一 ID（用于记录是否已触发）")]
    public string npcId;
    
    [Tooltip("只在第一次对话后触发")]
    public bool triggerOncePerScene = true;

    [Header("=== 背景设置 ===")]
    [Tooltip("半透明背景图片（UI Image）")]
    public Image backgroundImage;
    
    [Tooltip("背景淡入时间")]
    public float backgroundFadeInDuration = 1f;

    [Header("=== 要显示的元素 ===")]
    [Tooltip("依次显示的 UI 元素（文字、图片等）")]
    public List<GameObject> revealElements = new List<GameObject>();
    
    [Tooltip("每个元素的淡入时间")]
    public float elementFadeInDuration = 0.5f;
    
    [Tooltip("元素之间的间隔时间")]
    public float delayBetweenElements = 1f;

    [Header("=== Debug ===")]
    public bool enableDebug = true;

    // 记录已触发的 NPC（场景内）
    private static HashSet<string> triggeredNPCs = new HashSet<string>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        triggeredNPCs.Clear();
    }

    private void Awake()
    {
        // 在 Awake 中隐藏，确保在 NPC 被禁用前执行
        HideAllElements();
    }

    private void HideAllElements()
    {
        // 初始隐藏背景
        if (backgroundImage != null)
        {
            backgroundImage.gameObject.SetActive(false);
        }

        // 初始隐藏所有元素
        foreach (var element in revealElements)
        {
            if (element != null)
            {
                element.SetActive(false);
            }
        }
    }

    /// <summary>
    /// 对话完成后调用此方法
    /// </summary>
    public void OnDialogueComplete()
    {
        if (enableDebug) Debug.Log($"[NPCStoryReveal:{npcId}] 对话完成，检查是否触发揭示");

        // 检查是否已触发过
        if (triggerOncePerScene && triggeredNPCs.Contains(npcId))
        {
            if (enableDebug) Debug.Log($"[NPCStoryReveal:{npcId}] 已触发过，跳过");
            return;
        }

        // 检查是否有元素可显示
        if (revealElements.Count == 0)
        {
            if (enableDebug) Debug.Log($"[NPCStoryReveal:{npcId}] 没有元素可显示，跳过");
            return;
        }

        // 检查 StoryRevealManager
        if (StoryRevealManager.Instance == null)
        {
            if (enableDebug) Debug.LogError($"[NPCStoryReveal:{npcId}] StoryRevealManager.Instance 为 null！");
            return;
        }

        // 标记已触发
        triggeredNPCs.Add(npcId);

        // 触发揭示（传入所有设置）
        if (enableDebug) Debug.Log($"[NPCStoryReveal:{npcId}] 触发故事揭示");
        StoryRevealManager.Instance.StartReveal(
            backgroundImage,
            backgroundFadeInDuration,
            revealElements,
            elementFadeInDuration,
            delayBetweenElements
        );
    }

    /// <summary>
    /// 重置此 NPC 的触发状态
    /// </summary>
    public void ResetTrigger()
    {
        triggeredNPCs.Remove(npcId);
        if (enableDebug) Debug.Log($"[NPCStoryReveal:{npcId}] 触发状态已重置");
    }

    /// <summary>
    /// 重置所有 NPC 的触发状态
    /// </summary>
    public static void ResetAllTriggers()
    {
        triggeredNPCs.Clear();
        Debug.Log("[NPCStoryReveal] 所有触发状态已重置");
    }
}
