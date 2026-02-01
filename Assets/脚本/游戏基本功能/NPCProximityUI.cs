using System.Collections;
using UnityEngine;
using TMPro;

/// <summary>
/// NPC 接近 UI 系统
/// 管理 NPC 相关的接近提示、对话中提示
/// </summary>
public class NPCProximityUI : MonoBehaviour
{
    [Header("=== NPC 设置 ===")]
    [Tooltip("关联的 NPC 物体（用于检测 NPC 是否存在）")]
    public GameObject npcObject;

    [Header("=== 靠近检测 ===")]
    [Tooltip("靠近检测用的 Collider（NPC 消失时会禁用）")]
    public Collider2D proximityCollider;

    [Header("=== 对话提示框 ===")]
    [Tooltip("靠近时显示的提示文字（如：按 E 对话）")]
    public TextMeshProUGUI promptText;
    
    [Tooltip("提示文字淡入速度")]
    public float promptFadeInSpeed = 3f;
    
    [Tooltip("提示文字淡出速度")]
    public float promptFadeOutSpeed = 3f;

    [Header("=== 对话中提示框 ===")]
    [Tooltip("对话中显示的文字（闪烁效果）")]
    public TextMeshProUGUI talkingText;
    
    [Tooltip("闪烁速度")]
    public float blinkSpeed = 2f;

    [Header("=== 对话结束后设置 ===")]
    [Tooltip("对话结束后重新显示提示框（取消勾选：NPC 对话后消失的情况）")]
    public bool showPromptAfterDialogue = true;

    [Header("=== 玩家设置 ===")]
    public string playerTag = "Player";

    // 状态
    private bool playerInRange = false;
    private bool isNPCTalking = false;
    private bool npcExists = true;
    private float promptAlpha = 0f;
    private Transform playerTransform;
    private Coroutine blinkCoroutine;

    private void Start()
    {
        // 初始化文字为透明
        if (promptText != null)
        {
            SetTextAlpha(promptText, 0f);
        }
        if (talkingText != null)
        {
            SetTextAlpha(talkingText, 0f);
        }

        // 查找玩家
        GameObject player = GameObject.FindGameObjectWithTag(playerTag);
        if (player != null)
        {
            playerTransform = player.transform;
        }

        // 检查 NPC 是否存在
        CheckNPCExists();
    }

    private void Update()
    {
        // 检查 NPC 是否存在
        CheckNPCExists();

        if (!npcExists)
        {
            // NPC 不存在，隐藏所有 UI
            HideAllUI();
            return;
        }

        // 查找玩家（如果还没找到）
        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag(playerTag);
            if (player != null)
            {
                playerTransform = player.transform;
            }
            else
            {
                return;
            }
        }

        // 检测玩家是否在范围内
        playerInRange = IsPlayerInRange();

        // 检测 NPC 是否正在对话
        bool wasTalking = isNPCTalking;
        isNPCTalking = IsNPCTalking();

        // 状态变化处理
        if (isNPCTalking && !wasTalking)
        {
            // 开始对话
            OnDialogueStart();
        }
        else if (!isNPCTalking && wasTalking)
        {
            // 结束对话
            OnDialogueEnd();
        }

        // 更新 UI
        UpdatePromptUI();
    }

    private void CheckNPCExists()
    {
        if (npcObject != null)
        {
            npcExists = npcObject.activeInHierarchy;
        }
        else
        {
            npcExists = false;
        }

        // 禁用/启用 Collider
        if (proximityCollider != null)
        {
            proximityCollider.enabled = npcExists;
        }
    }

    private bool IsPlayerInRange()
    {
        if (proximityCollider == null || playerTransform == null) return false;
        return proximityCollider.OverlapPoint(playerTransform.position);
    }

    private bool IsNPCTalking()
    {
        if (DialogueManager.Instance == null) return false;
        return DialogueManager.Instance.IsTalking();
    }

    private void OnDialogueStart()
    {
        // 隐藏提示框
        promptAlpha = 0f;
        if (promptText != null)
        {
            SetTextAlpha(promptText, 0f);
        }

        // 开始闪烁对话中文字
        if (talkingText != null)
        {
            if (blinkCoroutine != null)
            {
                StopCoroutine(blinkCoroutine);
            }
            blinkCoroutine = StartCoroutine(BlinkText());
        }
    }

    private void OnDialogueEnd()
    {
        // 停止闪烁
        if (blinkCoroutine != null)
        {
            StopCoroutine(blinkCoroutine);
            blinkCoroutine = null;
        }

        // 隐藏对话中文字
        if (talkingText != null)
        {
            SetTextAlpha(talkingText, 0f);
        }

        // 检查 NPC 是否还存在（可能对话后消失了）
        CheckNPCExists();
    }

    private void UpdatePromptUI()
    {
        if (promptText == null) return;

        // 对话中不显示提示
        if (isNPCTalking)
        {
            promptAlpha = 0f;
            SetTextAlpha(promptText, 0f);
            return;
        }

        // NPC 不存在不显示
        if (!npcExists)
        {
            promptAlpha = 0f;
            SetTextAlpha(promptText, 0f);
            return;
        }

        // 根据玩家是否在范围内淡入/淡出
        if (playerInRange)
        {
            // 检查是否应该显示（对话结束后）
            if (showPromptAfterDialogue || !WasDialogueCompleted())
            {
                promptAlpha = Mathf.MoveTowards(promptAlpha, 1f, promptFadeInSpeed * Time.deltaTime);
            }
        }
        else
        {
            promptAlpha = Mathf.MoveTowards(promptAlpha, 0f, promptFadeOutSpeed * Time.deltaTime);
        }

        SetTextAlpha(promptText, promptAlpha);
    }

    private bool WasDialogueCompleted()
    {
        // 检查 NPC 是否已经完成过对话
        var npcDialogue = npcObject?.GetComponent<NPCConditionalDialogue>();
        if (npcDialogue != null)
        {
            // 使用反射检查 hasCompletedFirstDialogue（因为是私有的）
            var field = typeof(NPCConditionalDialogue).GetField("hasCompletedFirstDialogue", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                return (bool)field.GetValue(npcDialogue);
            }
        }
        return false;
    }

    private IEnumerator BlinkText()
    {
        float alpha = 0f;
        bool increasing = true;

        while (true)
        {
            if (increasing)
            {
                alpha += blinkSpeed * Time.deltaTime;
                if (alpha >= 1f)
                {
                    alpha = 1f;
                    increasing = false;
                }
            }
            else
            {
                alpha -= blinkSpeed * Time.deltaTime;
                if (alpha <= 0f)
                {
                    alpha = 0f;
                    increasing = true;
                }
            }

            if (talkingText != null)
            {
                SetTextAlpha(talkingText, alpha);
            }

            yield return null;
        }
    }

    private void HideAllUI()
    {
        // 停止闪烁
        if (blinkCoroutine != null)
        {
            StopCoroutine(blinkCoroutine);
            blinkCoroutine = null;
        }

        promptAlpha = 0f;

        if (promptText != null)
        {
            SetTextAlpha(promptText, 0f);
        }
        if (talkingText != null)
        {
            SetTextAlpha(talkingText, 0f);
        }
    }

    private void SetTextAlpha(TextMeshProUGUI text, float alpha)
    {
        if (text == null) return;
        Color color = text.color;
        color.a = alpha;
        text.color = color;
    }

    private void OnDisable()
    {
        HideAllUI();
    }
}
