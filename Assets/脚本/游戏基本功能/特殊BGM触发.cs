using UnityEngine;

/// <summary>
/// 特殊BGM触发器
/// 用于在特定情况下触发特殊背景音乐
/// 使用方法：
/// 1. 添加到带有Collider2D（勾选IsTrigger）的物体上
/// 2. 设置触发的特殊BGM名称
/// 3. 玩家进入/离开触发区域时自动切换音乐
/// </summary>
public class SpecialBGMTrigger : MonoBehaviour
{
    [Header("=== 触发设置 ===")]
    [Tooltip("要触发的特殊BGM名称（需要在MusicManager中配置）")]
    public string specialBGMName;
    
    [Tooltip("是否淡入淡出")]
    public bool withFade = true;
    
    [Tooltip("触发方式")]
    public TriggerType triggerType = TriggerType.OnEnter;
    
    [Tooltip("可触发的标签（空则所有物体都可触发）")]
    public string triggerTag = "Player";

    [Header("=== 离开设置 ===")]
    [Tooltip("离开触发区域时是否停止特殊BGM")]
    public bool stopOnExit = true;

    [Header("=== 一次性触发 ===")]
    [Tooltip("是否只触发一次")]
    public bool triggerOnce = false;
    
    private bool hasTriggered = false;

    public enum TriggerType
    {
        [Tooltip("进入时触发")]
        OnEnter,
        [Tooltip("离开时触发")]
        OnExit,
        [Tooltip("停留时触发")]
        OnStay
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (triggerType != TriggerType.OnEnter) return;
        if (!CheckTag(other)) return;
        if (triggerOnce && hasTriggered) return;

        TriggerBGM();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!CheckTag(other)) return;

        if (triggerType == TriggerType.OnExit)
        {
            if (triggerOnce && hasTriggered) return;
            TriggerBGM();
        }
        else if (stopOnExit && triggerType == TriggerType.OnEnter)
        {
            StopBGM();
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (triggerType != TriggerType.OnStay) return;
        if (!CheckTag(other)) return;
        if (triggerOnce && hasTriggered) return;

        TriggerBGM();
    }

    private bool CheckTag(Collider2D other)
    {
        if (string.IsNullOrEmpty(triggerTag))
        {
            return true;
        }
        return other.CompareTag(triggerTag);
    }

    private void TriggerBGM()
    {
        if (MusicManager.Instance == null)
        {
            Debug.LogWarning("MusicManager实例未找到！");
            return;
        }

        if (string.IsNullOrEmpty(specialBGMName))
        {
            Debug.LogWarning("特殊BGM名称为空！");
            return;
        }

        MusicManager.Instance.TriggerSpecialBGM(specialBGMName, withFade);
        hasTriggered = true;
    }

    private void StopBGM()
    {
        if (MusicManager.Instance == null) return;

        MusicManager.Instance.StopSpecialBGM(withFade);
    }

    /// <summary>
    /// 手动触发特殊BGM（可在其他脚本中调用）
    /// </summary>
    public void ManualTrigger()
    {
        if (triggerOnce && hasTriggered) return;
        TriggerBGM();
    }

    /// <summary>
    /// 手动停止特殊BGM
    /// </summary>
    public void ManualStop()
    {
        StopBGM();
    }

    /// <summary>
    /// 重置触发状态
    /// </summary>
    public void ResetTrigger()
    {
        hasTriggered = false;
    }
}
