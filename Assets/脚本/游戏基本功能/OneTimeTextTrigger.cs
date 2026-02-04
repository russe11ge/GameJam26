using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// 一次性文字提示触发器
/// 
/// 功能：
/// - 玩家靠近 Collider 时，显示文字 + 播放音效
/// - 文字显示几秒后自动消失
/// - 整个游戏过程中只触发一次
/// - 返回主菜单时重置（通过 MainMenuReset 调用 ResetAllTriggers）
/// 
/// 使用方法：
/// 1. 创建一个带有 Collider2D (Is Trigger) 的物体
/// 2. 添加此脚本
/// 3. 设置 triggerId（唯一标识）
/// 4. 拖入要显示的 TextMeshProUGUI
/// 5. 可选：设置音效
/// </summary>
public class OneTimeTextTrigger : MonoBehaviour
{
    [Header("=== 触发器设置 ===")]
    [Tooltip("触发器唯一ID（用于记录是否已触发）")]
    public string triggerId;
    
    [Tooltip("玩家标签")]
    public string playerTag = "Player";

    [Header("=== 文字设置 ===")]
    [Tooltip("要显示的文字组件（TextMeshProUGUI）")]
    public TextMeshProUGUI targetText;
    
    [Tooltip("文字显示时长（秒）")]
    public float displayDuration = 3f;
    
    [Tooltip("文字淡入时长")]
    public float fadeInDuration = 0.3f;
    
    [Tooltip("文字淡出时长")]
    public float fadeOutDuration = 0.5f;

    [Header("=== 音效设置 ===")]
    [Tooltip("触发时播放的音效")]
    public AudioClip triggerSound;
    
    [Tooltip("音效音量")]
    [Range(0f, 1f)]
    public float soundVolume = 1f;

    [Header("=== 触发后行为 ===")]
    [Tooltip("触发后隐藏此物体（包括 Collider）")]
    public bool hideAfterTrigger = true;
    
    [Tooltip("触发后禁用 Collider（如果不隐藏物体）")]
    public bool disableColliderAfterTrigger = true;

    [Header("=== Debug ===")]
    public bool enableDebug = false;

    // 记录已触发的触发器（跨场景保留）
    private static HashSet<string> triggeredIds = new HashSet<string>();

    private AudioSource audioSource;
    private Collider2D triggerCollider;
    private bool isTriggering = false;
    private Coroutine displayCoroutine;

    private void Awake()
    {
        // 获取或创建 AudioSource
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && triggerSound != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }

        triggerCollider = GetComponent<Collider2D>();
    }

    private void Start()
    {
        // 初始隐藏文字
        if (targetText != null)
        {
            SetTextAlpha(0f);
        }

        // 检查是否已触发过
        if (!string.IsNullOrEmpty(triggerId) && triggeredIds.Contains(triggerId))
        {
            if (enableDebug) Debug.Log($"[OneTimeTextTrigger:{triggerId}] 已触发过，禁用");
            
            if (hideAfterTrigger)
            {
                gameObject.SetActive(false);
            }
            else if (disableColliderAfterTrigger && triggerCollider != null)
            {
                triggerCollider.enabled = false;
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag)) return;
        if (isTriggering) return;

        // 检查是否已触发
        if (!string.IsNullOrEmpty(triggerId) && triggeredIds.Contains(triggerId))
        {
            return;
        }

        // 触发！
        TriggerDisplay();
    }

    /// <summary>
    /// 触发显示
    /// </summary>
    private void TriggerDisplay()
    {
        if (isTriggering) return;
        isTriggering = true;

        if (enableDebug) Debug.Log($"[OneTimeTextTrigger:{triggerId}] 触发！");

        // 标记为已触发
        if (!string.IsNullOrEmpty(triggerId))
        {
            triggeredIds.Add(triggerId);
        }

        // 播放音效
        if (triggerSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(triggerSound, soundVolume);
        }

        // 开始显示文字
        if (displayCoroutine != null)
        {
            StopCoroutine(displayCoroutine);
        }
        displayCoroutine = StartCoroutine(DisplayTextCoroutine());
    }

    /// <summary>
    /// 显示文字的协程
    /// </summary>
    private IEnumerator DisplayTextCoroutine()
    {
        if (targetText == null)
        {
            if (enableDebug) Debug.LogWarning($"[OneTimeTextTrigger:{triggerId}] targetText 为空！");
            yield break;
        }

        // 1. 淡入
        yield return StartCoroutine(FadeText(0f, 1f, fadeInDuration));

        // 2. 显示一段时间
        yield return new WaitForSeconds(displayDuration);

        // 3. 淡出
        yield return StartCoroutine(FadeText(1f, 0f, fadeOutDuration));

        // 4. 触发后处理
        if (hideAfterTrigger)
        {
            gameObject.SetActive(false);
        }
        else if (disableColliderAfterTrigger && triggerCollider != null)
        {
            triggerCollider.enabled = false;
        }

        if (enableDebug) Debug.Log($"[OneTimeTextTrigger:{triggerId}] 显示完成");
    }

    /// <summary>
    /// 淡入淡出文字
    /// </summary>
    private IEnumerator FadeText(float from, float to, float duration)
    {
        if (targetText == null) yield break;
        if (duration <= 0)
        {
            SetTextAlpha(to);
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(from, to, elapsed / duration);
            SetTextAlpha(alpha);
            yield return null;
        }
        SetTextAlpha(to);
    }

    /// <summary>
    /// 设置文字透明度
    /// </summary>
    private void SetTextAlpha(float alpha)
    {
        if (targetText == null) return;
        Color color = targetText.color;
        color.a = alpha;
        targetText.color = color;
    }

    /// <summary>
    /// 手动触发（可从其他脚本调用）
    /// </summary>
    public void ManualTrigger()
    {
        if (!string.IsNullOrEmpty(triggerId) && triggeredIds.Contains(triggerId))
        {
            if (enableDebug) Debug.Log($"[OneTimeTextTrigger:{triggerId}] 已触发过，无法再次触发");
            return;
        }
        TriggerDisplay();
    }

    /// <summary>
    /// 重置此触发器（允许再次触发）
    /// </summary>
    public void ResetTrigger()
    {
        if (!string.IsNullOrEmpty(triggerId))
        {
            triggeredIds.Remove(triggerId);
        }
        isTriggering = false;
        
        if (triggerCollider != null)
        {
            triggerCollider.enabled = true;
        }
        
        if (enableDebug) Debug.Log($"[OneTimeTextTrigger:{triggerId}] 已重置");
    }

    /// <summary>
    /// 重置所有触发器（返回主菜单时调用）
    /// </summary>
    public static void ResetAllTriggers()
    {
        triggeredIds.Clear();
        Debug.Log("[OneTimeTextTrigger] 所有触发器已重置");
    }

    /// <summary>
    /// 检查触发器是否已触发
    /// </summary>
    public static bool HasTriggered(string id)
    {
        return triggeredIds.Contains(id);
    }

    private void OnDrawGizmos()
    {
        // 已触发显示红色，未触发显示绿色
        bool triggered = !string.IsNullOrEmpty(triggerId) && triggeredIds.Contains(triggerId);
        Gizmos.color = triggered ? Color.red : Color.green;
        
        Collider2D col = GetComponent<Collider2D>();
        if (col is BoxCollider2D box)
        {
            Gizmos.DrawWireCube(transform.position + (Vector3)box.offset, box.size);
        }
        else if (col is CircleCollider2D circle)
        {
            Gizmos.DrawWireSphere(transform.position + (Vector3)new Vector2(circle.offset.x, circle.offset.y), circle.radius);
        }
        else
        {
            Gizmos.DrawWireSphere(transform.position, 0.5f);
        }
    }

    // 域重载时不清除（保持游戏会话内状态）
    // 如果需要在编辑器重新进入Play Mode时重置，取消下面的注释
    // [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    // private static void ResetStatics()
    // {
    //     triggeredIds.Clear();
    // }
}
